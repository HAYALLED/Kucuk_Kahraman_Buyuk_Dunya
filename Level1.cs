using Godot;
using System;

public partial class Level1 : Node2D
{
    [Export] public int MinimumScore = 100;

    private int currentLevelScore = 0;
    private bool levelCompleted = false;
    private Label messageLabel;
    private Player_controller player;

    public override void _Ready()
    {
        Database.Init();
        Database.InsertLevels(); // ✅ EKLE
        Database.InsertSampleMathQuestions();

        bool dbOk = Database.HealthCheck();
        if (dbOk)
            GD.Print("[DB] Veritabanı hazır ✅");
        else
            GD.PrintErr("[DB] Veritabanı HATALI ❌");

        CreateMessageLabel();

        AddPauseMenu();

        CheckReturnFromSettings();

        Vector2? returnPos = GetSecretReturnPosition();

        CallDeferred(nameof(FindPlayer));
        CallDeferred(nameof(RestorePlayerTrash));
        CallDeferred(nameof(RestorePlayerState));

        if (returnPos.HasValue)
        {
            GD.Print($"[LEVEL] 🔄 Secret'ten dönüş! Pos: {returnPos.Value}");
            CallDeferred(nameof(SetPlayerSpawnPosition), returnPos.Value);
        }

        GD.Print($"[LEVEL] Hedef: {MinimumScore} puan");
    }

    private void CheckReturnFromSettings()
    {
        if (GetTree().Root.HasMeta("ReturnToPause"))
        {
            GD.Print("[LEVEL] 🔙 Settings'den geri dönüldü, pause açılıyor...");

            // 0.1 saniye bekle (scene yüklensin)
            GetTree().CreateTimer(0.1).Timeout += () =>
            {
                // Pause'u aç
                GetTree().Paused = true;

                // PauseMenu'yu göster
                var pauseMenu = GetNodeOrNull<CanvasLayer>("PauseMenu");
                if (pauseMenu != null)
                {
                    pauseMenu.Show();
                }
            };

            // Meta'yı temizle (sadece ReturnToPause, PausedLevel Settings.cs'de temizlendi)
            GetTree().Root.RemoveMeta("ReturnToPause");
        }
    }

    private void AddPauseMenu()
    {
        var pauseScene = GD.Load<PackedScene>("res://Resources/PauseMenu.tscn");
        var pauseMenu = pauseScene.Instantiate();
        AddChild(pauseMenu);
        GD.Print("[LEVEL] ✅ Pause menüsü eklendi!");
    }
    private void RestorePlayerTrash()
    {
        var player = GetNodeOrNull<Player_controller>("Player");
        if (player == null)
        {
            var players = GetTree().GetNodesInGroup("player");
            if (players.Count > 0)
                player = players[0] as Player_controller;
        }

        if (player == null) return;

        var root = GetTree().Root;

        if (root.HasMeta("SavedTrash_Metal"))
        {
            int metal = (int)root.GetMeta("SavedTrash_Metal");
            int glass = (int)root.GetMeta("SavedTrash_Glass");
            int plastic = (int)root.GetMeta("SavedTrash_Plastic");
            int food = (int)root.GetMeta("SavedTrash_Food");
            int wood = (int)root.GetMeta("SavedTrash_Wood");

            if (metal > 0) player.AddMetal(metal);
            if (glass > 0) player.AddGlass(glass);
            if (plastic > 0) player.AddPlastic(plastic);
            if (food > 0) player.AddFood(food);
            if (wood > 0) player.AddWood(wood);

            int total = metal + glass + plastic + food + wood;
            GD.Print($"[LEVEL] 🔄 Çöpler geri yüklendi: {total} adet");

            root.RemoveMeta("SavedTrash_Metal");
            root.RemoveMeta("SavedTrash_Glass");
            root.RemoveMeta("SavedTrash_Plastic");
            root.RemoveMeta("SavedTrash_Food");
            root.RemoveMeta("SavedTrash_Wood");
        }
    }

    private void RestorePlayerState()
    {
        var player = GetNodeOrNull<Player_controller>("Player");
        if (player == null)
        {
            var players = GetTree().GetNodesInGroup("player");
            if (players.Count > 0)
                player = players[0] as Player_controller;
        }

        if (player == null) return;

        var root = GetTree().Root;

        try
        {
            // ✅ Can
            if (root.HasMeta("SavedHealth"))
            {
                int health = (int)root.GetMeta("SavedHealth");
                int maxHealth = (int)root.GetMeta("SavedMaxHealth");

                player.MaxHealth = maxHealth;

                int currentHealth = player.GetCurrentHealth();
                int diff = health - currentHealth;

                if (diff > 0)
                    player.Heal(diff);
                else if (diff < 0)
                    player.TakeDamage(-diff);

                player.UpdateHealthUI();

                GD.Print($"[LEVEL] 🔄 Can geri yüklendi: {health}/{maxHealth}");

                root.RemoveMeta("SavedHealth");
                root.RemoveMeta("SavedMaxHealth");
            }

            // ✅ Kostüm
            if (root.HasMeta("SavedCostume"))
            {
                int costumeIndex = (int)root.GetMeta("SavedCostume");

                Callable.From(() =>
                {
                    if (player != null && IsInstanceValid(player))
                    {
                        player.Call("RestoreCostume", costumeIndex);
                        GD.Print($"[LEVEL] 🔄 Kostüm geri yüklendi: {costumeIndex}");
                    }
                }).CallDeferred();

                root.RemoveMeta("SavedCostume");
            }

            // ✅ UI
            Callable.From(() =>
            {
                if (player != null && IsInstanceValid(player))
                {
                    player.UpdateHealthUI();
                }
            }).CallDeferred();
        }
        catch (Exception e)
        {
            GD.PrintErr($"[LEVEL] ❌ RestorePlayerState hatası: {e.Message}");
        }
    }

    private Vector2? GetSecretReturnPosition()
    {
        if (GetTree().Root.HasMeta("ReturnFromSecret"))
        {
            Vector2 pos = (Vector2)GetTree().Root.GetMeta("ReturnFromSecret");
            GetTree().Root.RemoveMeta("ReturnFromSecret");
            GD.Print($"[LEVEL] ✅ Dönüş pos bulundu: {pos}");
            return pos;
        }
        return null;
    }

    private void SetPlayerSpawnPosition(Vector2 pos)
    {
        if (player == null)
            player = GetNodeOrNull<Player_controller>("Player");

        if (player == null)
        {
            var players = GetTree().GetNodesInGroup("player");
            if (players.Count > 0)
                player = players[0] as Player_controller;
        }

        if (player != null)
        {
            player.GlobalPosition = pos;
            GD.Print($"[LEVEL] ✅ Player exit'e taşındı: {pos}");
            GD.Print($"[LEVEL] 🗑️ Çöp sayısı korundu: {player.TotalPoints}");
        }
    }

    private void FindPlayer()
    {
        player = GetNodeOrNull<Player_controller>("player");

        if (player == null)
        {
            GD.PrintErr("[LEVEL] ❌ Player bulunamadı!");
        }
        else
        {
            GD.Print("[LEVEL] ✅ Player bulundu!");
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
        GD.Print($"[LEVEL] 📚 Teacher puanı eklendi: +{points}, Toplam: {currentLevelScore}/{MinimumScore}");

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
        GD.Print($"[LEVEL] 🎮 Minigame puanı eklendi: +{points}, Toplam: {currentLevelScore}/{MinimumScore}");

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

    private void CheckLevelCompletion()
    {
        if (currentLevelScore >= MinimumScore)
        {
            LevelPassed();
        }
        else
        {
            LevelFailed();
        }
    }

    private void LevelPassed()
    {
        if (levelCompleted) return;
        levelCompleted = true;

        // ✅ SKORLARI KAYDET!
        SaveLevelScore();

        SaveGame.Instance.MarkLevelCompleted("level_1");
        currentLevelScore = 0;

        ShowMessage($"TEBRİKLER! Level 1 geçildi!", Colors.Green);
        GD.Print($"[LEVEL] ✅ LEVEL 1 GEÇİLDİ VE KAYDEDİLDİ!");

        GetTree().CreateTimer(3.0).Timeout += () =>
        {
            if (ResourceLoader.Exists("res://Assets/Scenes/Areas/Level2.tscn"))
                GetTree().ChangeSceneToFile("res://Assets/Scenes/Areas/Level2.tscn");
            else if (ResourceLoader.Exists("res://Assets/Scenes/Areas/level_2.tscn"))
                GetTree().ChangeSceneToFile("res://Assets/Scenes/Areas/level_2.tscn");
            else
                GetTree().ChangeSceneToFile("res://Resources/level_select.tscn");
        };
    }

    // ✅ TEK SaveLevelScore() METODU
    // ✅ TEK SaveLevelScore() METODU
    private void SaveLevelScore()
    {
        try
        {
            // ✅ DÜZELT: GetCurrentUserId() → CurrentUserID
            int userId = SaveGame.Instance.GetCurrentUserId();

            if (userId <= 0)
            {
                GD.PrintErr("[LEVEL] ❌ Geçerli kullanıcı yok, skor kaydedilemedi!");
                return;
            }

            // Level 1 = LevelID 1
            bool success = Database.SaveScore(userId, 1, currentLevelScore);

            if (success)
                GD.Print($"[LEVEL] ✅ Skor kaydedildi: User={userId}, Level=1, Score={currentLevelScore}");
            else
                GD.PrintErr("[LEVEL] ❌ Skor kaydetme başarısız!");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[LEVEL] ❌ SaveLevelScore hatası: {ex.Message}");
        }
    }
    private void LevelFailed()
    {
        int remaining = MinimumScore - currentLevelScore;
        ShowMessage($"Yetersiz! Daha {remaining} puan gerekli.", Colors.Orange);
        GD.Print($"[LEVEL] ⚠️ Yetersiz! {currentLevelScore}/{MinimumScore}");
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
        GD.Print("[LEVEL] Level skoru sıfırlandı!");

        if (player != null)
        {
            player.UpdateScoresUI(currentLevelScore, MinimumScore);
        }
    }
}