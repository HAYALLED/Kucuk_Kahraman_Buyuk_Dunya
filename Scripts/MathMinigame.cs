using Godot;
using System;

public partial class MathMinigame : CanvasLayer
{
    // Minigame türleri
    public enum MinigameType
    {
        Teacher,      // Puan ver
        Tailor,       // Kostüm iyileştir/yok et
        SpecialEvent  // Geçici kostüm ver
    }

    // UI Referansları
    private Label soruLabel;
    private LineEdit answerInput;
    private Button submitButton;
    private Label correctLabel;
    private Label wrongLabel;
    private ProgressBar fuseBar;

    // Oyun değişkenleri
    private Godot.Collections.Array<Godot.Collections.Dictionary> questions;
    private int currentQuestionIndex = 0;
    private int correctCount = 0;
    private int wrongCount = 0;

    // Ayarlar
    public int QuestionCount = 2;
    public float TimeLimit = 30f;
    public string Difficulty = "";
    public MinigameType GameType = MinigameType.Teacher;

    // Callback - Sonuç bildirir (correctCount, wrongCount, questionCount)
    public Action<int, int, int> OnMinigameComplete;

    // Special Event için
    public CostumeResource RewardCostume;
    public int CostumeSlotIndex = 0;  // Tailor için hangi slot

    private float timeRemaining;
    private bool gameActive = false;

    public override void _Ready()
    {
        GD.Print($"[MATH] MathMinigame başlatılıyor... Tür: {GameType}, Soru: {QuestionCount}");

        var control = GetNode<Control>("Control");

        soruLabel = control.GetNodeOrNull<Label>("soru");
        answerInput = control.GetNodeOrNull<LineEdit>("LineEdit");
        submitButton = control.GetNodeOrNull<Button>("Button");
        correctLabel = control.GetNodeOrNull<Label>("CorrectLabel");
        wrongLabel = control.GetNodeOrNull<Label>("WrongLabel");
        fuseBar = control.GetNodeOrNull<ProgressBar>("FuseBar");

        if (submitButton != null)
            submitButton.Pressed += OnSubmitPressed;

        if (answerInput != null)
            answerInput.TextSubmitted += OnTextSubmitted;

        StartGame();
    }

    public override void _Process(double delta)
    {
        if (!gameActive) return;

        timeRemaining -= (float)delta;

        if (fuseBar != null)
            fuseBar.Value = (timeRemaining / TimeLimit) * 100;

        if (timeRemaining <= 0)
        {
            // Kalan soruları yanlış say
            wrongCount += (questions.Count - currentQuestionIndex);
            EndGame(false);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            CallDeferred(nameof(CloseMinigame));
        }
    }

    private void StartGame()
    {
        string difficultyFilter = string.IsNullOrEmpty(Difficulty) ? null : Difficulty;
        questions = Database.GetMathQuestions(difficultyFilter, QuestionCount);

        if (questions.Count == 0)
        {
            Database.InsertSampleMathQuestions();
            questions = Database.GetMathQuestions(difficultyFilter, QuestionCount);

            if (questions.Count == 0)
            {
                if (soruLabel != null)
                    soruLabel.Text = "Soru bulunamadı!";
                return;
            }
        }

        currentQuestionIndex = 0;
        correctCount = 0;
        wrongCount = 0;
        timeRemaining = TimeLimit;
        gameActive = true;

        if (fuseBar != null)
        {
            fuseBar.MaxValue = 100;
            fuseBar.Value = 100;
        }

        UpdateScoreLabels();
        ShowCurrentQuestion();

        if (answerInput != null)
            answerInput.GrabFocus();
    }

    private void ShowCurrentQuestion()
    {
        if (currentQuestionIndex >= questions.Count)
        {
            EndGame(true);
            return;
        }

        var question = questions[currentQuestionIndex];

        if (soruLabel != null)
            soruLabel.Text = question["question"].ToString();

        if (answerInput != null)
        {
            answerInput.Text = "";
            answerInput.GrabFocus();
        }
    }

    private void OnSubmitPressed() => CheckAnswer();
    private void OnTextSubmitted(string text) => CheckAnswer();

    private void CheckAnswer()
    {
        if (!gameActive || currentQuestionIndex >= questions.Count) return;

        var question = questions[currentQuestionIndex];
        string correctAnswer = question["answer"].ToString().Trim().ToLower();
        string playerAnswer = answerInput != null ? answerInput.Text.Trim().ToLower() : "";

        if (playerAnswer == correctAnswer)
        {
            correctCount++;
            GD.Print("[MATH] ✓ Doğru!");
        }
        else
        {
            wrongCount++;
            GD.Print($"[MATH] ✗ Yanlış! Doğru cevap: {correctAnswer}");
        }

        UpdateScoreLabels();
        currentQuestionIndex++;
        ShowCurrentQuestion();
    }

    private void UpdateScoreLabels()
    {
        if (correctLabel != null)
            correctLabel.Text = $"DOĞRU: {correctCount}";
        if (wrongLabel != null)
            wrongLabel.Text = $"YANLIŞ: {wrongCount}";
    }

    private void EndGame(bool completed)
    {
        gameActive = false;

        // Sonuç metnini türe göre ayarla
        string resultText = GetResultText();

        if (soruLabel != null)
            soruLabel.Text = resultText;

        if (answerInput != null)
            answerInput.Editable = false;

        if (submitButton != null)
        {
            submitButton.Text = "Kapat";
            submitButton.Pressed -= OnSubmitPressed;
            submitButton.Pressed += CloseMinigame;
        }

        // Callback'i çağır
        OnMinigameComplete?.Invoke(correctCount, wrongCount, questions.Count);

        GD.Print($"[MATH] Oyun bitti - Doğru: {correctCount}, Yanlış: {wrongCount}");
    }

    private string GetResultText()
    {
        float successRate = questions.Count > 0 ? (float)correctCount / questions.Count : 0;

        switch (GameType)
        {
            case MinigameType.Teacher:
                int points = (correctCount * 10) - (wrongCount * 5);
                return $"Sonuç!\n{correctCount}/{questions.Count} Doğru\n{(points >= 0 ? "+" : "")}{points} Puan";

            case MinigameType.Tailor:
                if (wrongCount >= 2)
                    return "Başarısız!\nKostüm kayboldu...";
                else if (correctCount >= 2)
                    return "Mükemmel!\nKostüm yenilendi!";
                else
                    return "Eh işte...\nHiçbir şey olmadı.";

            case MinigameType.SpecialEvent:
                if (correctCount == 3)
                    return "MUHTEŞEM!\nKostüm level boyunca senin!";
                else if (correctCount == 2)
                    return "İyi!\n80 saniye kostüm!";
                else if (correctCount == 1)
                    return "Yetersiz...\nHiçbir şey olmadı.";
                else
                    return "FELAKET!\nHasar aldın!";

            default:
                return $"{correctCount}/{questions.Count} Doğru";
        }
    }

    private void CloseMinigame()
    {
        GetTree().CallDeferred("set_pause", false);
        CallDeferred("queue_free");
    }
}