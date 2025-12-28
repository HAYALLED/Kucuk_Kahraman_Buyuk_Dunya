using Godot;
using System;

public partial class Tutorial_Level : Node2D
{
    [Export] public int MinimumScore = 10;

    private int currentLevelScore = 0;
    private bool levelCompleted = false;
    private Label messageLabel;
    private Player_controller player;

    public override void _Ready()
    {
        Database.Init();
        Database.InsertLevels();
        Database.InsertSampleMathQuestions();

        bool dbOk = Database.HealthCheck();
        if (dbOk)
            GD.Print("[DB] Veritabanı hazır ✅");
        else
            GD.PrintErr("[DB] Veritabanı HATALI ❌");

        CreateMessageLabel();
        AddPauseMenu();
        CheckReturnFromSettings();

        // ✅ TUTORIAL'DA SECRET LEVEL YOK!
        // GetSecretReturnPosition() ve SetPlayerSpawnPosition() kaldırıldı!

        CallDeferred(nameof(FindPlayer));

        // ✅ TUTORIAL'DA RESTORE GEREK YOK (ilk level bu!)
        // RestorePlayerTrash() ve RestorePlayerState() kaldırıldı!

        GD.Print($"[TUTORIAL] 🎓 Hoş geldiniz! Hedef: {MinimumScore} puan");
    }

    private void CheckReturnFromSettings()
    {
        if (GetTree().Root.HasMeta("ReturnToPause"))
        {
            GD.Print("[TUTORIAL] 🔙 Settings'den geri dönüldü, pause açılıyor...");

            GetTree().CreateTimer(0.1).Timeout += () =>
            {
                GetTree().Paused = true;

                var pauseMenu = GetNodeOrNull<CanvasLayer>("PauseMenu");
                if (pauseMenu != null)
                {
                    pauseMenu.Show();
                }
            };

            GetTree().Root.RemoveMeta("ReturnToPause");
        }
    }

    private void AddPauseMenu()
    {
        var pauseScene = GD.Load<PackedScene>("res://Resources/PauseMenu.tscn");
        var pauseMenu = pauseScene.Instantiate();
        AddChild(pauseMenu);
        GD.Print("[TUTORIAL] ✅ Pause menüsü eklendi!");
    }

    private void FindPlayer()
    {
        // ✅ FIX: "Player" (büyük harf) - Scene'deki node adı ile eşleşmeli!
        player = GetNodeOrNull<Player_controller>("player");

        if (player == null)
        {
            // ✅ Fallback: Group ile ara
            var players = GetTree().GetNodesInGroup("player");
            if (players.Count > 0)
                player = players[0] as Player_controller;
        }

        if (player == null)
        {
            GD.PrintErr("[TUTORIAL] ❌ Player bulunamadı!");
        }
        else
        {
            GD.Print("[TUTORIAL] ✅ Player bulundu!");
            player.UpdateScoresUI(currentLevelScore, MinimumScore);
        }
    }

    private void CreateMessageLabel()
    {
        var uiLayer = new CanvasLayer();
        uiLayer.Name = "MessageUI";
        uiLayer.Layer = 200;
        AddChild(uiLayer);

        messageLabel = new Label();
        messageLabel.Name = "MessageLabel";
        messageLabel.Position = new Vector2(400, 50);
        messageLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        messageLabel.AddThemeFontSizeOverride("font_size", 24);
        messageLabel.Visible = false;
        uiLayer.AddChild(messageLabel);
    }

    public void AddTeacherScore(int points)
    {
        currentLevelScore += points;
        GD.Print($"[TUTORIAL] 📚 Teacher puanı eklendi: +{points}, Toplam: {currentLevelScore}/{MinimumScore}");

        if (player != null)
        {
            player.UpdateScoresUI(currentLevelScore, MinimumScore);
        }

        if (currentLevelScore >= MinimumScore)
        {
            ShowMessage($"Harika! {MinimumScore} puana ulaştınız!", Colors.Green);
            GetTree().CreateTimer(3.0).Timeout += LevelPassed;
        }
    }

    public void AddMinigameScore(int points)
    {
        currentLevelScore += points;
        GD.Print($"[TUTORIAL] 🎮 Minigame puanı eklendi: +{points}, Toplam: {currentLevelScore}/{MinimumScore}");

        if (player != null)
        {
            player.UpdateScoresUI(currentLevelScore, MinimumScore);
        }

        if (currentLevelScore >= MinimumScore)
        {
            ShowMessage($"Tebrikler! {currentLevelScore} puan topladın!\n(Hedef: {MinimumScore})", Colors.Green);
            GetTree().CreateTimer(3.0).Timeout += LevelPassed;
        }
        else
        {
            int missing = MinimumScore - currentLevelScore;
            ShowMessage($"Toplam Puan: {currentLevelScore}/{MinimumScore}\nEksik: {missing} puan!", Colors.Yellow);
        }
    }

    private void LevelPassed()
    {
        if (levelCompleted) return;
        levelCompleted = true;

        // ✅ SKORLARI KAYDET!
        SaveLevelScore();

        // ✅ TUTORIAL LEVEL COMPLETED!
        SaveGame.Instance.MarkLevelCompleted("tutorial");

        ShowMessage($"TEBRİKLER! Tutorial tamamlandı!", Colors.Green);
        GD.Print($"[TUTORIAL] ✅ TUTORIAL TAMAMLANDI VE KAYDEDİLDİ!");

        // ✅ FIX: Gereksiz if'ler kaldırıldı!
        GetTree().CreateTimer(3.0).Timeout += () =>
        {
            // ✅ Level seçme ekranına git
            string levelSelectPath = "res://Resources/level_select.tscn";

            if (ResourceLoader.Exists(levelSelectPath))
            {
                GD.Print("[TUTORIAL] 📋 Level seçme ekranına gidiliyor...");
                GetTree().ChangeSceneToFile(levelSelectPath);
            }
            else
            {
                GD.PrintErr($"[TUTORIAL] ❌ Level select bulunamadı: {levelSelectPath}");
                // ✅ Fallback: Ana menüye dön
                GetTree().ChangeSceneToFile("res://Resources/main_menu.tscn");
            }
        };
    }

    private void SaveLevelScore()
    {
        try
        {
            int userId = UserProfile.Instance.CurrentUserID;

            if (userId <= 0)
            {
                GD.PrintErr("[TUTORIAL] ❌ Geçerli kullanıcı yok, skor kaydedilemedi!");
                return;
            }


            bool success = Database.SaveScore(userId, 0, currentLevelScore);


            if (success)
                GD.Print($"[TUTORIAL] ✅ Skor kaydedildi: User={userId}, Level=Tutorial, Score={currentLevelScore}");
            else
                GD.PrintErr("[TUTORIAL] ❌ Skor kaydetme başarısız!");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[TUTORIAL] ❌ SaveLevelScore hatası: {ex.Message}");
        }
    }

    private async void ShowMessage(string text, Color color)
    {
        if (messageLabel == null) return;

        messageLabel.Text = text;
        messageLabel.AddThemeColorOverride("font_color", color);
        messageLabel.Visible = true;

        await ToSignal(GetTree().CreateTimer(4.0), SceneTreeTimer.SignalName.Timeout);
        messageLabel.Visible = false;
    }

    public int GetCurrentScore() => currentLevelScore;
    public int GetRequiredScore() => MinimumScore;

    public void ResetLevelScore()
    {
        currentLevelScore = 0;
        GD.Print("[TUTORIAL] Level skoru sıfırlandı!");

        if (player != null)
        {
            player.UpdateScoresUI(currentLevelScore, MinimumScore);
        }
    }
}