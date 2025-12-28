using Godot;
using System.Collections.Generic;

public partial class RecyclingMinigame : CanvasLayer
{
    [Export] public Texture2D[] TrashTextures = new Texture2D[5];
    [Export] public PackedScene DraggableTrashScene;

    [Export] public float CorrectTimeBonus = 10.0f;
    [Export] public float WrongTimePenalty = 5.0f;
    [Export] public float MaxTimeLimit = 120.0f;

    private static readonly string[] TrashNames = { "Metal", "Cam", "Plastik", "Organik", "Kağıt" };

    private static readonly int[][] BinAccepts = {
        new int[] { 3 },        // Index 0: FOOD (Organik)
        new int[] { 4 },        // Index 1: WOOD (Kağıt)
        new int[] { 1 },        // Index 2: GLASS (Cam)
        new int[] { 0 },        // Index 3: METAL (Metal)
        new int[] { 2 }         // Index 4: PLASTIC (Plastik)
    };

    // UI Elemanları
    private Control trashSpawnPoint;
    private ProgressBar fuseBar;
    private Label scoreLabel;
    private Label correctLabel;
    private Label wrongLabel;
    private Label timeLabel;
    private HBoxContainer binsContainer;
    private RecycleBin[] bins = new RecycleBin[5];

    // Oyun değişkenleri
    private List<int> trashQueue = new List<int>();
    private int currentTrashIndex = 0;
    private int correctCount = 0;
    private int wrongCount = 0;
    private float maxTime = 60.0f;
    private float timeLeft;
    private bool isPlaying = false;
    private Node2D playerRef;
    private DraggableTrash currentTrash;

    // ✅ YENİ: Orijinal çöp miktarlarını sakla!
    private int[] originalTrash = new int[5];

    public override void _Ready()
    {
        CreateFullscreenBackground();

        trashSpawnPoint = GetNode<Control>("GameArea/TrashSpawnPoint");
        fuseBar = GetNode<ProgressBar>("GameArea/FuseBar");
        scoreLabel = GetNode<Label>("GameArea/ScoreLabel");
        correctLabel = GetNode<Label>("GameArea/CorrectLabel");
        wrongLabel = GetNode<Label>("GameArea/WrongLabel");

        timeLabel = GetNodeOrNull<Label>("GameArea/TimeLabel");

        binsContainer = GetNode<HBoxContainer>("GameArea/BinsContainer");

        for (int i = 0; i < 5; i++)
        {
            bins[i] = binsContainer.GetChild<RecycleBin>(i);
            bins[i].AcceptedTrashTypes = BinAccepts[i];

            string acceptedNames = "";
            foreach (int type in BinAccepts[i])
            {
                acceptedNames += TrashNames[type] + " ";
            }
            GD.Print($"[BIN {i}] {bins[i].Name} kabul ediyor: {acceptedNames}");
        }

        fuseBar.MaxValue = maxTime;
        fuseBar.Value = maxTime;

        SetProcessInput(true);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            CallDeferred(nameof(CancelMinigame));
        }
    }

    public override void _Process(double delta)
    {
        if (!isPlaying) return;

        timeLeft -= (float)delta;
        fuseBar.Value = timeLeft;

        if (timeLabel != null)
        {
            timeLabel.Text = $"Süre: {Mathf.CeilToInt(timeLeft)}s";
        }

        float percent = timeLeft / maxTime;
        if (percent > 0.5f)
            fuseBar.Modulate = Colors.Green;
        else if (percent > 0.25f)
            fuseBar.Modulate = Colors.Yellow;
        else
            fuseBar.Modulate = Colors.Red;

        if (timeLeft <= 0)
        {
            CallDeferred(nameof(EndMinigame));
        }
    }

    private void CreateFullscreenBackground()
    {
        var overlay = new ColorRect();
        overlay.Name = "FullscreenOverlay";
        overlay.Color = new Color(0, 0, 0, 0.85f);
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlay.MouseFilter = Control.MouseFilterEnum.Stop;
        overlay.ZIndex = -1;

        AddChild(overlay);
        MoveChild(overlay, 0);

        var gameArea = GetNodeOrNull<Control>("GameArea");
        if (gameArea != null)
        {
            gameArea.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            gameArea.OffsetLeft = 100;
            gameArea.OffsetRight = -100;
            gameArea.OffsetTop = 100;
            gameArea.OffsetBottom = -100;

            GD.Print("[MINIGAME] ✅ Tam ekran!");
        }
    }

    public void Setup(int[] collectedTrash, Node2D player)
    {
        playerRef = player;
        trashQueue.Clear();

        // ✅ ORİJİNAL ÇÖPLERI KAYDET!
        for (int i = 0; i < 5; i++)
        {
            originalTrash[i] = collectedTrash[i];
        }

        for (int type = 0; type < 5; type++)
        {
            for (int i = 0; i < collectedTrash[type]; i++)
            {
                trashQueue.Add(type);
            }
        }

        ShuffleQueue();

        GD.Print($"[MINIGAME] Toplam {trashQueue.Count} çöp!");

        if (trashQueue.Count == 0)
        {
            GD.Print("[MINIGAME] Hiç çöp yok!");
            CallDeferred(nameof(CancelMinigame));
            return;
        }

        isPlaying = true;
        timeLeft = maxTime;
        currentTrashIndex = 0;
        correctCount = 0;
        wrongCount = 0;

        SpawnNextTrash();
        UpdateUI();
    }

    private void ShuffleQueue()
    {
        var rng = new RandomNumberGenerator();
        rng.Randomize();

        for (int i = trashQueue.Count - 1; i > 0; i--)
        {
            int j = rng.RandiRange(0, i);
            (trashQueue[i], trashQueue[j]) = (trashQueue[j], trashQueue[i]);
        }
    }

    private void SpawnNextTrash()
    {
        if (currentTrashIndex >= trashQueue.Count)
        {
            CallDeferred(nameof(EndMinigame));
            return;
        }

        int trashType = trashQueue[currentTrashIndex];

        currentTrash = DraggableTrashScene.Instantiate<DraggableTrash>();
        trashSpawnPoint.AddChild(currentTrash);

        Texture2D texture = trashType < TrashTextures.Length ? TrashTextures[trashType] : null;
        currentTrash.Setup(trashType, TrashNames[trashType], texture);
        currentTrash.SetOriginalPosition(Vector2.Zero);

        currentTrash.DroppedOnBin += OnTrashDropped;

        GD.Print($"[MINIGAME] Çöp: {TrashNames[trashType]}");
    }

    private void OnTrashDropped(int binIndex, Control trash)
    {
        if (!isPlaying) return;

        var droppedTrash = trash as DraggableTrash;
        int trashType = droppedTrash.TrashType;

        GD.Print($"[DROP] Çöp türü: {TrashNames[trashType]} (ID: {trashType}) → Bin: {bins[binIndex].Name} (Index: {binIndex})");

        bool isCorrect = bins[binIndex].AcceptsTrash(trashType);

        if (isCorrect)
        {
            correctCount++;
            bins[binIndex].FlashColor(Colors.Green);

            timeLeft = Mathf.Min(timeLeft + CorrectTimeBonus, MaxTimeLimit);
            ShowTimeBonus($"+{CorrectTimeBonus}s", Colors.Green);

            GD.Print($"[MINIGAME] ✅ Doğru! +{CorrectTimeBonus}s");
        }
        else
        {
            wrongCount++;
            bins[binIndex].FlashColor(Colors.Red);

            timeLeft = Mathf.Max(timeLeft - WrongTimePenalty, 0);
            ShowTimeBonus($"-{WrongTimePenalty}s", Colors.Red);

            GD.Print($"[MINIGAME] ❌ Yanlış! -{WrongTimePenalty}s");
        }

        currentTrash.CallDeferred("queue_free");
        currentTrash = null;

        UpdateUI();

        currentTrashIndex++;
        CallDeferred(nameof(SpawnNextTrash));
    }

    private async void ShowTimeBonus(string text, Color color)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", 32);
        label.Position = new Vector2(400, 100);
        AddChild(label);

        var tween = CreateTween();
        tween.TweenProperty(label, "position:y", 50, 1.0f);
        tween.Parallel().TweenProperty(label, "modulate:a", 0, 1.0f);

        await ToSignal(tween, Tween.SignalName.Finished);
        label.CallDeferred("queue_free");
    }

    private void UpdateUI()
    {
        correctLabel.Text = $"Doğru: {correctCount}";
        wrongLabel.Text = $"Yanlış: {wrongCount}";

        int score = (correctCount * 10) - (wrongCount * 5);
        scoreLabel.Text = $"Puan: {score}";
    }

    private void EndMinigame()
    {
        isPlaying = false;
        GetTree().CallDeferred("set_pause", false);

        int minigameScore = (correctCount * 10) - (wrongCount * 5);

        // ✅ BAŞARILI BİTİŞ: Player'a puan ver, çöpleri sıfırla
        if (playerRef != null && playerRef.HasMethod("UpdateMinigameScore"))
        {
            playerRef.Call("UpdateMinigameScore", minigameScore);
            GD.Print($"[MINIGAME] ✅ Player.UpdateMinigameScore çağrıldı: {minigameScore} puan");
        }

        if (playerRef != null && playerRef.HasMethod("ResetPoints"))
        {
            playerRef.Call("ResetPoints");
            GD.Print("[MINIGAME] ✅ Çöpler sıfırlandı (başarılı bitiş)");
        }

        GD.Print($"[MINIGAME] Bitti! D:{correctCount} Y:{wrongCount}, Puan: {minigameScore}");
        CallDeferred("queue_free");
    }

    private void CancelMinigame()
    {
        isPlaying = false;
        GetTree().CallDeferred("set_pause", false);

        // ✅ İPTAL: ÇÖPLERI GERİ VER!
        if (playerRef != null && playerRef.HasMethod("RestorePoints"))
        {
            playerRef.Call("RestorePoints", originalTrash);
            GD.Print($"[MINIGAME] ⚠️ İptal edildi! Çöpler geri verildi: [{originalTrash[0]}, {originalTrash[1]}, {originalTrash[2]}, {originalTrash[3]}, {originalTrash[4]}]");
        }
        else
        {
            GD.PrintErr("[MINIGAME] ⚠️ Player'da RestorePoints metodu yok!");
        }

        CallDeferred("queue_free");
    }
}