using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
    private Button resumeButton;
    private Button restartButton;
    private Button mainMenuButton;
    private Button settingsButton;
    private Button quitButton;

    private string currentLevelPath = "";

    public override void _Ready()
    {
        // ===== NODE BAĞLANTILARI =====
        resumeButton = GetNode<Button>("MenuContainer/ResumeButton");
        restartButton = GetNode<Button>("MenuContainer/RestartButton");
        mainMenuButton = GetNode<Button>("MenuContainer/MainMenuButton");
        settingsButton = GetNode<Button>("MenuContainer/SettingsButton");
        quitButton = GetNode<Button>("MenuContainer/QuitButton");

        // ===== SİGNAL BAĞLANTILARI =====
        resumeButton.Pressed += OnResumePressed;
        restartButton.Pressed += OnRestartPressed;
        mainMenuButton.Pressed += OnMainMenuPressed;
        settingsButton.Pressed += OnSettingsPressed;
        quitButton.Pressed += OnQuitPressed;

        // ✅ Başlangıçta gizli
        Hide();

        GD.Print("[PAUSE] Pause menüsü hazır!");
    }

    public override void _Input(InputEvent @event)
    {
        // ✅ P tuşu kontrolü
        if (@event.IsActionPressed("ui_pause")) // P tuşu
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        bool isPaused = GetTree().Paused;

        if (isPaused)
        {
            // ✅ Resume
            Resume();
        }
        else
        {
            // ✅ Pause
            Pause();
        }
    }

    private void Pause()
    {
        GetTree().Paused = true;
        Show();
        GD.Print("[PAUSE] ⏸️ Oyun durduruldu!");
    }

    private void Resume()
    {
        GetTree().Paused = false;
        Hide();
        GD.Print("[PAUSE] ▶️ Oyun devam ediyor!");
    }

    // ===== BUTTON EVENTS =====
    private void OnResumePressed()
    {
        GD.Print("[PAUSE] ▶️ Devam et butonuna basıldı!");
        Resume();
    }

    private void OnRestartPressed()
    {
        GD.Print("[PAUSE] 🔄 Level yeniden başlatılıyor...");
        Resume(); // Önce pause'u kaldır

        // Mevcut leveli yeniden yükle
        string currentScene = GetTree().CurrentScene.SceneFilePath;
        GetTree().ReloadCurrentScene();
    }

    private void OnMainMenuPressed()
    {
        GD.Print("[PAUSE] 🏠 Ana menüye dönülüyor...");
        Resume(); // Önce pause'u kaldır
        GetTree().Root.RemoveMeta("PausedLevel");
        GetTree().ChangeSceneToFile("res://Resources/main_menu.tscn");
    }

    private void OnSettingsPressed()
    {
        string currentLevel = GetTree().CurrentScene.SceneFilePath;
        GetTree().Root.SetMeta("PausedLevel", currentLevel);
        GetTree().Root.SetMeta("ReturnToPause", true);

        GD.Print($"[PAUSE] 📌 Level kaydedildi: {currentLevel}");
        Resume();
        GetTree().ChangeSceneToFile("res://Resources/Settings.tscn");
    }

    private void OnQuitPressed()
    {
        GD.Print("[PAUSE] 🚪 Oyundan çıkılıyor...");
        GetTree().Quit();
    }
}