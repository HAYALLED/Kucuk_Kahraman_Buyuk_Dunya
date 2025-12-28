using Godot;

public partial class LevelSelect : Control
{
    private TextureButton level1Button;
    private TextureButton level2Button;
    private TextureButton level3Button;
    private TextureButton level4Button;
    private TextureButton backButton;
    private TextureButton tutorialButton;
    // ===== LEVEL 1 TEXTURE'LARI =====
    [ExportGroup("Level 1 Normal")]
    [Export] public Texture2D Level1NormalIdle;
    [Export] public Texture2D Level1NormalHover;
    [Export] public Texture2D Level1NormalClicked;

    [ExportGroup("Level 1 Completed")]
    [Export] public Texture2D Level1CompletedIdle;
    [Export] public Texture2D Level1CompletedClicked;

    // ===== LEVEL 2 TEXTURE'LARI =====
    [ExportGroup("Level 2 Normal")]
    [Export] public Texture2D Level2NormalIdle;
    [Export] public Texture2D Level2NormalHover;
    [Export] public Texture2D Level2NormalClicked;

    [ExportGroup("Level 2 Completed")]
    [Export] public Texture2D Level2CompletedIdle;
    [Export] public Texture2D Level2CompletedClicked;

    // ===== LEVEL 3 TEXTURE'LARI =====
    [ExportGroup("Level 3 Normal")]
    [Export] public Texture2D Level3NormalIdle;
    [Export] public Texture2D Level3NormalHover;
    [Export] public Texture2D Level3NormalClicked;

    [ExportGroup("Level 3 Completed")]
    [Export] public Texture2D Level3CompletedIdle;
    [Export] public Texture2D Level3CompletedClicked;

    // ===== LEVEL 4 TEXTURE'LARI =====
    [ExportGroup("Level 4 Normal")]
    [Export] public Texture2D Level4NormalIdle;
    [Export] public Texture2D Level4NormalHover;
    [Export] public Texture2D Level4NormalClicked;

    [ExportGroup("Level 4 Completed")]
    [Export] public Texture2D Level4CompletedIdle;
    [Export] public Texture2D Level4CompletedClicked;

    public override void _Ready()
    {
        // Button'ları bul
        level1Button = GetNode<TextureButton>("HBoxContainer/level1Button");
        level2Button = GetNode<TextureButton>("HBoxContainer/level2Button");
        level3Button = GetNode<TextureButton>("HBoxContainer/level3Button");
        level4Button = GetNode<TextureButton>("HBoxContainer/level4Button");
        backButton = GetNode<TextureButton>("BackButton");
        tutorialButton = GetNode<TextureButton>("tutorialButton");
        // Signal'leri bağla tutorialButton
        level1Button.Pressed += () => LoadLevel("res://Assets/Scenes/Areas/area_1.tscn");
        level2Button.Pressed += () => LoadLevel("res://Assets/Scenes/Areas/Level2.tscn");
        level3Button.Pressed += () => LoadLevel("res://Assets/Scenes/Areas/level_3.tscn");
        level4Button.Pressed += () => LoadLevel("res://Assets/Scenes/Areas/level_4.tscn");
        tutorialButton.Pressed += () => LoadLevel("res://Assets/Scenes/Areas/Tutorial_Level.tscn");
        backButton.Pressed += OnBackPressed;
        // ✅ Tamamlanmış levelleri görsel olarak göster
        UpdateLevelButtons();

        GD.Print("[LEVEL SELECT] Level seçim ekranı hazır!");
    }

    // ✅ YENİ: TÜM TEXTURE'LARI GÜNCELLE
    private void UpdateLevelButtons()
    {
        // ✅ Aktif kullanıcı ID'sini al
        int userId = SaveGame.Instance.GetCurrentUserId();

        GD.Print($"[LEVEL SELECT] ========== UPDATE BAŞLADI ==========");
        GD.Print($"[LEVEL SELECT] Aktif kullanıcı ID: {userId}");

        if (userId <= 0)
        {
            GD.PrintErr("[LEVEL SELECT] ❌ Geçersiz kullanıcı ID! Tüm leveller normal görünecek.");
            SetLevelNormal(level1Button, Level1NormalIdle, Level1NormalHover, Level1NormalClicked);
            SetLevelNormal(level2Button, Level2NormalIdle, Level2NormalHover, Level2NormalClicked);
            SetLevelNormal(level3Button, Level3NormalIdle, Level3NormalHover, Level3NormalClicked);
            SetLevelNormal(level4Button, Level4NormalIdle, Level4NormalHover, Level4NormalClicked);
            return;
        }

        // ===== LEVEL 1 =====
        bool level1Completed = Database.HasUserCompletedLevel(userId, 1);
        GD.Print($"[LEVEL SELECT] Level 1 tamamlanma: {level1Completed}");

        if (level1Completed)
        {
            SetLevelCompleted(level1Button, Level1CompletedIdle, Level1CompletedClicked);
            GD.Print("[LEVEL SELECT] ✅ Level 1 texture'ı completed olarak değiştirildi!");
        }
        else
        {
            SetLevelNormal(level1Button, Level1NormalIdle, Level1NormalHover, Level1NormalClicked);
            GD.Print("[LEVEL SELECT] Level 1 texture'ı normal olarak ayarlandı.");
        }

        // ===== LEVEL 2 =====
        bool level2Completed = Database.HasUserCompletedLevel(userId, 2);
        GD.Print($"[LEVEL SELECT] Level 2 tamamlanma: {level2Completed}");

        if (level2Completed)
        {
            SetLevelCompleted(level2Button, Level2CompletedIdle, Level2CompletedClicked);
            GD.Print("[LEVEL SELECT] ✅ Level 2 texture'ı completed olarak değiştirildi!");
        }
        else
        {
            SetLevelNormal(level2Button, Level2NormalIdle, Level2NormalHover, Level2NormalClicked);
            GD.Print("[LEVEL SELECT] Level 2 texture'ı normal olarak ayarlandı.");
        }

        // ===== LEVEL 3 =====
        bool level3Completed = Database.HasUserCompletedLevel(userId, 3);
        GD.Print($"[LEVEL SELECT] Level 3 tamamlanma: {level3Completed}");

        if (level3Completed)
        {
            SetLevelCompleted(level3Button, Level3CompletedIdle, Level3CompletedClicked);
        }
        else
        {
            SetLevelNormal(level3Button, Level3NormalIdle, Level3NormalHover, Level3NormalClicked);
        }

        // ===== LEVEL 4 =====
        bool level4Completed = Database.HasUserCompletedLevel(userId, 4);
        GD.Print($"[LEVEL SELECT] Level 4 tamamlanma: {level4Completed}");

        if (level4Completed)
        {
            SetLevelCompleted(level4Button, Level4CompletedIdle, Level4CompletedClicked);
        }
        else
        {
            SetLevelNormal(level4Button, Level4NormalIdle, Level4NormalHover, Level4NormalClicked);
        }

        GD.Print($"[LEVEL SELECT] ========== UPDATE BİTTİ ==========");
    }

    private void SetLevelNormal(TextureButton button, Texture2D idle, Texture2D hover, Texture2D clicked)
    {
        if (idle != null)
            button.TextureNormal = idle;

        if (hover != null)
            button.TextureHover = hover;

        if (clicked != null)
            button.TexturePressed = clicked;
    }

    // ✅ Helper metod: Completed texture'ları ayarla
    private void SetLevelCompleted(TextureButton button, Texture2D idle, Texture2D clicked)
    {
        if (idle != null)
            button.TextureNormal = idle;

        if (clicked != null)
            button.TexturePressed = clicked;

        button.TextureHover = null; // Hover yok tamamlanmışta
    }

    private void LoadLevel(string levelPath)
    {
        GD.Print($"[LEVEL SELECT] Level yükleniyor: {levelPath}");
        GetTree().ChangeSceneToFile(levelPath);
    }

    private void OnBackPressed()
    {
        GD.Print("[LEVEL SELECT] Ana menüye dönülüyor...");
        GetTree().ChangeSceneToFile("res://Resources/main_menu.tscn");
    }
}