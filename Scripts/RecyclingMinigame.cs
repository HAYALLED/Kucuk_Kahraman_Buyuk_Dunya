using Godot;
using System.Collections.Generic;

public partial class RecyclingMinigame : CanvasLayer
{
    [Export] public Texture2D[] MetalVariants = new Texture2D[0];
    [Export] public Texture2D[] GlassVariants = new Texture2D[0];
    [Export] public Texture2D[] PlasticVariants = new Texture2D[0];
    [Export] public Texture2D[] OrganicVariants = new Texture2D[0];
    [Export] public Texture2D[] PaperVariants = new Texture2D[0];
    [Export] public PackedScene DraggableTrashScene;

    [Export] public float CorrectTimeBonus = 10.0f;
    [Export] public float WrongTimePenalty = 5.0f;
    [Export] public float MaxTimeLimit = 120.0f;

    private static readonly string[] TrashNameKeys = { "RECYCLE_TRASH_METAL", "RECYCLE_TRASH_GLASS", "RECYCLE_TRASH_PLASTIC", "RECYCLE_TRASH_ORGANIC", "RECYCLE_TRASH_PAPER" };

    private static readonly int[][] BinAccepts = {
        new int[] { 3 },
        new int[] { 4 },
        new int[] { 1 },
        new int[] { 0 },
        new int[] { 2 }
    };

    private Control trashSpawnPoint;
    private ProgressBar fuseBar;
    private Label scoreLabel;
    private Label correctLabel;
    private Label wrongLabel;
    private Label timeLabel;
    private Label campaignLabel; // Kampanya aktif göstergesi
    private HBoxContainer binsContainer;
    private RecycleBin[] bins = new RecycleBin[5];

    private List<int> trashQueue = new List<int>();
    private int currentTrashIndex = 0;
    private int correctCount = 0;
    private int wrongCount = 0;
    [Export] public float StartTime = 60.0f; // Başlangıç süresi (Inspector'dan değiştirilebilir)
    private float timeLeft;
    private bool isPlaying = false;
    private Node2D playerRef;
    private DraggableTrash currentTrash;

    private int[] originalTrash = new int[5];
    // Kampanya aktifken biriken bonus/ceza puanları
    private float campaignBonusScore = 0f;

    public override void _Ready()
    {
        CreateFullscreenBackground();

        trashSpawnPoint = GetNode<Control>("GameArea/MainLayout/VBox/TrashSpawnArea/TrashSpawnPoint");
        fuseBar = GetNode<ProgressBar>("GameArea/MainLayout/VBox/FuseBar");
        scoreLabel = GetNode<Label>("GameArea/MainLayout/VBox/StatsRow/ScoreLabel");
        correctLabel = GetNode<Label>("GameArea/MainLayout/VBox/StatsRow/CorrectLabel");
        wrongLabel = GetNode<Label>("GameArea/MainLayout/VBox/StatsRow/WrongLabel");
        timeLabel = GetNodeOrNull<Label>("GameArea/MainLayout/VBox/StatsRow/TimeLabel");
        campaignLabel = GetNodeOrNull<Label>("GameArea/MainLayout/VBox/CampaignLabel");
        binsContainer = GetNode<HBoxContainer>("GameArea/MainLayout/VBox/BinsContainer");

        for (int i = 0; i < 5; i++)
        {
            bins[i] = binsContainer.GetChild<RecycleBin>(i);
            bins[i].AcceptedTrashTypes = BinAccepts[i];
        }

        fuseBar.MaxValue = StartTime;
        fuseBar.Value = StartTime;

        // Kampanya aktifse göster
        if (campaignLabel != null)
        {
            campaignLabel.Visible = TrashCampaignEvent.IsCampaignActive;
            if (TrashCampaignEvent.IsCampaignActive)
                campaignLabel.Text = $"{Tr("RECYCLE_CAMPAIGN_ACTIVE")} x{TrashCampaignEvent.ActiveScoreMultiplier}";
        }

        SetProcessInput(true);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
            CallDeferred(nameof(CancelMinigame));
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationTranslationChanged)
        {
            if (!IsNodeReady())
                return;

            if (campaignLabel != null && TrashCampaignEvent.IsCampaignActive)
                campaignLabel.Text = $"{Tr("RECYCLE_CAMPAIGN_ACTIVE")} x{TrashCampaignEvent.ActiveScoreMultiplier}";

            UpdateUI();
        }
    }

    public override void _Process(double delta)
    {
        if (!isPlaying) return;

        timeLeft -= (float)delta;
        fuseBar.Value = timeLeft;

        if (timeLabel != null)
            timeLabel.Text = string.Format(Tr("RECYCLE_TIME_FORMAT"), Mathf.CeilToInt(timeLeft));

        float percent = timeLeft / StartTime;
        if (percent > 0.5f) fuseBar.Modulate = Colors.Green;
        else if (percent > 0.25f) fuseBar.Modulate = Colors.Yellow;
        else fuseBar.Modulate = Colors.Red;

        if (timeLeft <= 0)
            CallDeferred(nameof(EndMinigame));
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
            gameArea.OffsetLeft = 16;
            gameArea.OffsetRight = -16;
            gameArea.OffsetTop = 16;
            gameArea.OffsetBottom = -16;
        }
    }

    public void Setup(int[] collectedTrash, Node2D player)
    {
        playerRef = player;
        trashQueue.Clear();
        campaignBonusScore = 0f;

        for (int i = 0; i < 5; i++) originalTrash[i] = collectedTrash[i];

        for (int type = 0; type < 5; type++)
            for (int i = 0; i < collectedTrash[type]; i++)
                trashQueue.Add(type);

        ShuffleQueue();

        if (trashQueue.Count == 0) { CallDeferred(nameof(CancelMinigame)); return; }

        isPlaying = true;
        timeLeft = StartTime;
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
        if (currentTrashIndex >= trashQueue.Count) { CallDeferred(nameof(EndMinigame)); return; }

        int trashType = trashQueue[currentTrashIndex];
        currentTrash = DraggableTrashScene.Instantiate<DraggableTrash>();
        trashSpawnPoint.AddChild(currentTrash);

        // Her çöp türünde birden fazla görsel varyant var; her spawn'da
        // varyantlardan sadece BİR TANESİ rastgele seçilip gösterilir.
        Texture2D texture = GetRandomTrashVariant(trashType);
        currentTrash.Setup(trashType, Tr(TrashNameKeys[trashType]), texture);
        currentTrash.SetOriginalPosition(Vector2.Zero);
        currentTrash.DroppedOnBin += OnTrashDropped;
    }

    private Texture2D GetRandomTrashVariant(int trashType)
    {
        Texture2D[] variants = trashType switch
        {
            0 => MetalVariants,
            1 => GlassVariants,
            2 => PlasticVariants,
            3 => OrganicVariants,
            4 => PaperVariants,
            _ => null
        };

        if (variants == null || variants.Length == 0)
            return null;

        int index = (int)(GD.Randi() % (uint)variants.Length);
        return variants[index];
    }

    private void OnTrashDropped(int binIndex, Control trash)
    {
        if (!isPlaying) return;

        var droppedTrash = trash as DraggableTrash;
        if (droppedTrash == null) return; // güvenlik kontrolü
        int trashType = droppedTrash.TrashType;
        bool isCorrect = bins[binIndex].AcceptsTrash(trashType);

        if (isCorrect)
        {
            correctCount++;
            bins[binIndex].FlashColor(Colors.Green);

            // Kampanya aktifse bonus zaman da çarpan alır
            float bonus = CorrectTimeBonus * (TrashCampaignEvent.IsCampaignActive
                ? TrashCampaignEvent.ActiveCorrectMultiplier : 1f);
            timeLeft = Mathf.Min(timeLeft + bonus, MaxTimeLimit);

            // Kampanya bonus puanı biriktir
            if (TrashCampaignEvent.IsCampaignActive)
                campaignBonusScore += 10f * (TrashCampaignEvent.ActiveCorrectMultiplier - 1f);

            ShowTimeBonus($"+{bonus:F0}s", Colors.Green);
        }
        else
        {
            wrongCount++;
            bins[binIndex].FlashColor(Colors.Red);

            float penalty = WrongTimePenalty * (TrashCampaignEvent.IsCampaignActive
                ? TrashCampaignEvent.ActiveWrongMultiplier : 1f);
            timeLeft = Mathf.Max(timeLeft - penalty, 0);

            // Yanlış: sadece zaman cezası çarpanı uygulanır, skor baseScore'da zaten düşülüyor
            // Kampanya yanlış: ek skor cezası
            if (TrashCampaignEvent.IsCampaignActive)
                campaignBonusScore -= 5f * (TrashCampaignEvent.ActiveWrongMultiplier - 1f);

            ShowTimeBonus($"-{penalty:F0}s", Colors.Red);
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
        correctLabel.Text = string.Format(Tr("RECYCLE_CORRECT_FORMAT"), correctCount);
        wrongLabel.Text = string.Format(Tr("RECYCLE_WRONG_FORMAT"), wrongCount);

        float baseScore = (correctCount * 10f) - (wrongCount * 5f);
        // Kampanya aktifse final skor çarpanı + biriken bonus
        float totalScore = TrashCampaignEvent.IsCampaignActive
            ? (baseScore * TrashCampaignEvent.ActiveScoreMultiplier) + campaignBonusScore
            : baseScore;

        scoreLabel.Text = TrashCampaignEvent.IsCampaignActive
            ? string.Format(Tr("RECYCLE_SCORE_CAMPAIGN_FORMAT"), (int)totalScore, TrashCampaignEvent.ActiveScoreMultiplier)
            : string.Format(Tr("RECYCLE_SCORE_FORMAT"), (int)totalScore);
    }

    private void EndMinigame()
    {
        isPlaying = false;
        GetTree().CallDeferred("set_pause", false);

        float baseScore = (correctCount * 10f) - (wrongCount * 5f);
        float finalScore = TrashCampaignEvent.IsCampaignActive
            ? (baseScore * TrashCampaignEvent.ActiveScoreMultiplier) + campaignBonusScore
            : baseScore;

        if (playerRef != null && playerRef.HasMethod("UpdateMinigameScore"))
            playerRef.Call("UpdateMinigameScore", (int)finalScore);

        if (playerRef != null && playerRef.HasMethod("ResetPoints"))
            playerRef.Call("ResetPoints");

        GD.Print($"[MINIGAME] Bitti! D:{correctCount} Y:{wrongCount} | Base:{baseScore} Final:{(int)finalScore}");
        CallDeferred("queue_free");
    }

    private void CancelMinigame()
    {
        isPlaying = false;
        GetTree().CallDeferred("set_pause", false);

        if (playerRef != null && playerRef.HasMethod("RestorePoints"))
            playerRef.Call("RestorePoints", originalTrash);
        else
            GD.PrintErr("[MINIGAME] ⚠️ Player'da RestorePoints metodu yok!");

        CallDeferred("queue_free");
    }
}