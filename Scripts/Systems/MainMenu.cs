using Godot;

public partial class MainMenu : Control
{
    private TextureButton startButton;
    private TextureButton settingsButton;
    private TextureButton exitButton;
    private TextureButton profileButton;
    private TextureButton scoretableButton;
    public override void _Ready()
    {
        // Button'ları bul
        startButton = GetNode<TextureButton>("start");
        settingsButton = GetNode<TextureButton>("settings");
        exitButton = GetNode<TextureButton>("exit"); // ✅ "exit" olmalı!
        profileButton = GetNode<TextureButton>("profile"); // ✅ "profile" olmalı!
        scoretableButton = GetNode<TextureButton>("scoretable");
        // Signal'leri bağla
        startButton.Pressed += OnStartPressed;
        settingsButton.Pressed += OnSettingsPressed;
        exitButton.Pressed += OnExitPressed;
        profileButton.Pressed += OnProfilePressed;
        scoretableButton.Pressed += OnscoretablePressed;
        GD.Print("[MENU] Ana menü hazır!");
    }

    private void OnStartPressed()
    {
        GD.Print("[MENU] BAŞLAT'a basıldı - Level seçim ekranına gidiliyor...");
        GetTree().ChangeSceneToFile("res://Resources/level_select.tscn");
    }
    private void OnscoretablePressed()
    {
        GetTree().ChangeSceneToFile("res://Resources/ScoreBoard.tscn");
    }
    private void OnProfilePressed()
    {
        GD.Print("[MENU] PROFİL'e basıldı");
        GetTree().ChangeSceneToFile("res://Resources/ProfileManager.tscn");
    }

    private void OnSettingsPressed()
    {
        GD.Print("[MENU] AYARLAR'a basıldı - Ayarlar sayfasına gidiliyor...");
        GetTree().ChangeSceneToFile("res://Resources/Settings.tscn");
    }

    private void OnExitPressed()
    {
        GD.Print("[MENU] Oyundan çıkılıyor...");
        GetTree().Quit();
    }
}