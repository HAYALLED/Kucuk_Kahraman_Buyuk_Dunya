using Godot;

public partial class Settings : Control
{
    // Ekran
    private CheckBox fullscreenCheck;
    private OptionButton resolutionOption;
    private CheckBox vsyncCheck;
    private OptionButton languageOption;

    // Ses
    private HSlider masterSlider;
    private HSlider musicSlider;
    private HSlider sfxSlider;

    // Oyun
    private OptionButton difficultyOption;
    private Button resetButton;

    // Geri
    private TextureButton backButton;

    // ✅ İLK YÜKLEME KONTROLÜ
    private bool _isInitializing = true;

    public override void _Ready()
    {
        // ===== EKRAN =====
        fullscreenCheck = GetNode<CheckBox>("MarginContainer/VBoxContainer/TabContainer/Ekran/VBoxContainer/FullscreenCheck");
        resolutionOption = GetNode<OptionButton>("MarginContainer/VBoxContainer/TabContainer/Ekran/VBoxContainer/HBoxContainer/ResolutionOption");
        languageOption = GetNode<OptionButton>("MarginContainer/VBoxContainer/TabContainer/Ekran/VBoxContainer/HBoxContainer/LanguageOption");
        vsyncCheck = GetNode<CheckBox>("MarginContainer/VBoxContainer/TabContainer/Ekran/VBoxContainer/VSyncCheck");

        // ===== SES =====
        masterSlider = GetNode<HSlider>("MarginContainer/VBoxContainer/TabContainer/Ses/VBoxContainer/Master/MasterSlider");
        musicSlider = GetNode<HSlider>("MarginContainer/VBoxContainer/TabContainer/Ses/VBoxContainer/Music/MusicSlider");
        sfxSlider = GetNode<HSlider>("MarginContainer/VBoxContainer/TabContainer/Ses/VBoxContainer/SFX/SFXSlider");

        // ===== OYUN =====
        difficultyOption = GetNode<OptionButton>("MarginContainer/VBoxContainer/TabContainer/Oyun/VBoxContainer/HBoxContainer/DifficultyOption");
        resetButton = GetNode<Button>("MarginContainer/VBoxContainer/TabContainer/Oyun/VBoxContainer/HBoxContainer/ResetButton");

        // ===== GERİ =====
        backButton = GetNode<TextureButton>("BackButton");

        InitializeOptions();
        ScaleUI();

        // ✅ 1. ÖNCE AYARLARI YÜKLEYELİM (signal'ler henüz bağlı değil)
        LoadCurrentSettings();

        // ✅ 2. SONRA SIGNAL'LERİ BAĞLAYALIM (artık ayarlar yüklü)
        ConnectSignals();

        // ✅ 3. ARTIK HAZIR - İNİTİALİZİNG KAPALI
        _isInitializing = false;

        GD.Print("[SETTINGS] Ayarlar sayfası hazır!");
    }

    private void ScaleUI()
    {
        // BackButton'ı büyüt
        if (backButton != null)
        {
            backButton.CustomMinimumSize = new Vector2(300, 60);
            backButton.AddThemeFontSizeOverride("font_size", 22);
        }

        // Reset Button'ı büyüt
        if (resetButton != null)
        {
            resetButton.CustomMinimumSize = new Vector2(400, 60);
            resetButton.AddThemeFontSizeOverride("font_size", 20);
        }

        // Slider'ları büyüt
        if (masterSlider != null)
            masterSlider.CustomMinimumSize = new Vector2(450, 30);

        if (musicSlider != null)
            musicSlider.CustomMinimumSize = new Vector2(450, 30);

        if (sfxSlider != null)
            sfxSlider.CustomMinimumSize = new Vector2(450, 30);

        GD.Print("[SETTINGS] UI scaled!");
    }

    private void InitializeOptions()
    {
        // Çözünürlükler (dile bağlı değil)
        resolutionOption.AddItem("1280x720");
        resolutionOption.AddItem("1920x1080");
        resolutionOption.AddItem("2560x1440");

        // Diller (kendi öz adlarıyla gösterilir, çevrilmez)
        languageOption.AddItem("Türkçe");
        languageOption.AddItem("English");

        // Zorluklar
        difficultyOption.AddItem(Tr("DIFFICULTY_EASY"));
        difficultyOption.AddItem(Tr("DIFFICULTY_MEDIUM"));
        difficultyOption.AddItem(Tr("DIFFICULTY_HARD"));
    }

    private void RefreshLocalizedOptionTexts()
    {
        difficultyOption.SetItemText(0, Tr("DIFFICULTY_EASY"));
        difficultyOption.SetItemText(1, Tr("DIFFICULTY_MEDIUM"));
        difficultyOption.SetItemText(2, Tr("DIFFICULTY_HARD"));
    }

    private void LoadCurrentSettings()
    {
        var profile = UserProfile.Instance;

        GD.Print("========================================");
        GD.Print("[SETTINGS] Ayarlar yükleniyor...");
        GD.Print($"[SETTINGS] Fullscreen: {profile.IsFullscreen}");
        GD.Print($"[SETTINGS] Resolution: {profile.Resolution.X}x{profile.Resolution.Y}");
        GD.Print($"[SETTINGS] VSync: {profile.VSync}");
        GD.Print("========================================");

        // Ekran
        fullscreenCheck.ButtonPressed = profile.IsFullscreen;
        vsyncCheck.ButtonPressed = profile.VSync;

        if (profile.Resolution.X == 1280)
            resolutionOption.Selected = 0;
        else if (profile.Resolution.X == 1920)
            resolutionOption.Selected = 1;
        else if (profile.Resolution.X == 2560)
            resolutionOption.Selected = 2;
        else
            resolutionOption.Selected = 1; // Default: 1920x1080

        languageOption.Selected = profile.Language == "en" ? 1 : 0;

        // Ses
        masterSlider.Value = profile.MasterVolume;
        musicSlider.Value = profile.MusicVolume;
        sfxSlider.Value = profile.SFXVolume;

        // Oyun
        difficultyOption.Selected = profile.Difficulty switch
        {
            "Kolay" => 0,
            "Orta" => 1,
            "Zor" => 2,
            _ => 1
        };

        GD.Print("[SETTINGS] ✅ Ayarlar checkbox'lara set edildi!");
    }

    private void ConnectSignals()
    {
        fullscreenCheck.Toggled += OnFullscreenToggled;
        resolutionOption.ItemSelected += OnResolutionSelected;
        languageOption.ItemSelected += OnLanguageSelected;
        vsyncCheck.Toggled += OnVSyncToggled;

        masterSlider.ValueChanged += OnMasterVolumeChanged;
        musicSlider.ValueChanged += OnMusicVolumeChanged;
        sfxSlider.ValueChanged += OnSFXVolumeChanged;

        difficultyOption.ItemSelected += OnDifficultySelected;
        resetButton.Pressed += OnResetPressed;
        backButton.Pressed += OnBackPressed;

        GD.Print("[SETTINGS] ✅ Tüm signal'lar bağlandı!");
    }

    // ===== EKRAN AYARLARI =====
    private void OnFullscreenToggled(bool pressed)
    {
        // ✅ İlk yüklemede tetiklenme - göz ardı et
        if (_isInitializing)
        {
            GD.Print("[SETTINGS] ⚠️ Fullscreen signal - ilk yükleme, göz ardı edildi");
            return;
        }

        GD.Print($"[SETTINGS] 🖥️ Fullscreen değiştirildi: {pressed}");

        UserProfile.Instance.IsFullscreen = pressed;
        UserProfile.Instance.ApplyDisplaySettings();
        UserProfile.Instance.SaveSettings();
    }

    private void OnResolutionSelected(long index)
    {
        // ✅ İlk yüklemede tetiklenme - göz ardı et
        if (_isInitializing)
        {
            GD.Print("[SETTINGS] ⚠️ Resolution signal - ilk yükleme, göz ardı edildi");
            return;
        }

        Vector2I resolution = index switch
        {
            0 => new Vector2I(1280, 720),
            1 => new Vector2I(1920, 1080),
            2 => new Vector2I(2560, 1440),
            _ => new Vector2I(1920, 1080)
        };

        GD.Print($"[SETTINGS] 📐 Resolution değiştirildi: {resolution.X}x{resolution.Y}");

        UserProfile.Instance.Resolution = resolution;
        UserProfile.Instance.ApplyDisplaySettings();
        UserProfile.Instance.SaveSettings();
    }

    private void OnLanguageSelected(long index)
    {
        // ✅ İlk yüklemede tetiklenme - göz ardı et
        if (_isInitializing)
        {
            GD.Print("[SETTINGS] ⚠️ Language signal - ilk yükleme, göz ardı edildi");
            return;
        }

        string locale = index switch
        {
            0 => "tr",
            1 => "en",
            _ => "tr"
        };

        GD.Print($"[SETTINGS] 🌐 Dil değiştirildi: {locale}");

        UserProfile.Instance.Language = locale;
        UserProfile.Instance.ApplyLanguageSettings();
        UserProfile.Instance.SaveSettings();

        RefreshLocalizedOptionTexts();
    }

    private void OnVSyncToggled(bool pressed)
    {
        // ✅ İlk yüklemede tetiklenme - göz ardı et
        if (_isInitializing)
        {
            GD.Print("[SETTINGS] ⚠️ VSync signal - ilk yükleme, göz ardı edildi");
            return;
        }

        GD.Print("========================================");
        GD.Print($"[SETTINGS] 🔄 VSync TOGGLE EVENT!");
        GD.Print($"[SETTINGS]    Yeni Değer: {pressed}");
        GD.Print($"[SETTINGS]    Eski Değer: {UserProfile.Instance.VSync}");
        GD.Print("========================================");

        UserProfile.Instance.VSync = pressed;
        UserProfile.Instance.ApplyDisplaySettings();
        UserProfile.Instance.SaveSettings();

        GD.Print($"[SETTINGS] ✅ VSync kaydedildi!");
    }

    // ===== SES AYARLARI =====
    private void OnMasterVolumeChanged(double value)
    {
        // ✅ İlk yüklemede tetiklenme - göz ardı et
        if (_isInitializing) return;

        UserProfile.Instance.MasterVolume = (float)value;
        UserProfile.Instance.ApplyAudioSettings();
        UserProfile.Instance.SaveSettings();
    }

    private void OnMusicVolumeChanged(double value)
    {
        // ✅ İlk yüklemede tetiklenme - göz ardı et
        if (_isInitializing) return;

        UserProfile.Instance.MusicVolume = (float)value;
        UserProfile.Instance.ApplyAudioSettings();
        UserProfile.Instance.SaveSettings();
    }

    private void OnSFXVolumeChanged(double value)
    {
        // ✅ İlk yüklemede tetiklenme - göz ardı et
        if (_isInitializing) return;

        UserProfile.Instance.SFXVolume = (float)value;
        UserProfile.Instance.ApplyAudioSettings();
        UserProfile.Instance.SaveSettings();
    }

    // ===== OYUN AYARLARI =====
    private void OnDifficultySelected(long index)
    {
        // ✅ İlk yüklemede tetiklenme - göz ardı et
        if (_isInitializing) return;

        string difficulty = index switch
        {
            0 => "Kolay",
            1 => "Orta",
            2 => "Zor",
            _ => "Orta"
        };

        UserProfile.Instance.Difficulty = difficulty;
        UserProfile.Instance.SaveSettings();
        GD.Print($"[SETTINGS] Zorluk: {difficulty}");
    }

    private void OnResetPressed()
    {
        var confirm = new ConfirmationDialog();
        confirm.DialogText = Tr("SETTINGS_RESET_CONFIRM_TEXT");
        confirm.Title = Tr("SETTINGS_RESET_CONFIRM_TITLE");
        confirm.OkButtonText = Tr("SETTINGS_RESET_OK");
        confirm.CancelButtonText = Tr("SETTINGS_RESET_CANCEL");

        confirm.Confirmed += () =>
        {
            UserProfile.Instance.ResetAllProgress();
            GD.Print("[SETTINGS] İlerleme sıfırlandı!");
            confirm.QueueFree();
        };

        confirm.Canceled += () =>
        {
            GD.Print("[SETTINGS] Sıfırlama iptal edildi.");
            confirm.QueueFree();
        };

        AddChild(confirm);
        confirm.PopupCentered(new Vector2I(400, 150));
    }

    private void OnBackPressed()
    {
        if (GetTree().Root.HasMeta("ReturnToPause"))
        {
            string pausedLevel = (string)GetTree().Root.GetMeta("PausedLevel");

            GD.Print($"[SETTINGS] 🔙 Pause'a geri dönülüyor: {pausedLevel}");

            // Meta'ları temizle
            GetTree().Root.RemoveMeta("ReturnToPause");
            GetTree().Root.RemoveMeta("PausedLevel");

            // Level'e geri dön
            GetTree().ChangeSceneToFile(pausedLevel);
        }
        else
        {
            // Normal akış: Ana menüye git
            GetTree().ChangeSceneToFile("res://Resources/main_menu.tscn");
        }
    }
}