using Godot;
using System;

public partial class Level1 : Node2D
{
    [Export] public PackedScene RecyclingMinigameScene;
    [Export] public int MinimumScore = 100;

    private int currentLevelScore = 0;
    private bool levelCompleted = false;
    private Label messageLabel;
    private Player_controller player;

    public override void _Ready()
    {
        Database.Init();
        Database.InsertSampleMathQuestions();

        bool dbOk = Database.HealthCheck();
        if (dbOk)
            GD.Print("[DB] Veritabanı hazır ✅");
        else
            GD.PrintErr("[DB] Veritabanı HATALI ❌");

        CreateMessageLabel();

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
            // ✅ Kostüm - Callable ile
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

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("special_ability") && !levelCompleted)
        {
            TryStartMinigame();
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
            levelCompleted = true;
        }
    }

    public void AddMinigameScore(int points)
    {
        currentLevelScore += points;
        GD.Print($"[LEVEL] 🎮 Minigame puanı eklendi: +{points}, Toplam: {currentLevelScore}/{MinimumScore}");

        if (currentLevelScore >= MinimumScore)
        {
            ShowMessage($"Tebrikler! {currentLevelScore} puan topladın!\n(Hedef: {MinimumScore})", Colors.Green);
            levelCompleted = true;
        }
        else
        {
            int missing = MinimumScore - currentLevelScore;
            ShowMessage($"Toplam Puan: {currentLevelScore}/{MinimumScore}\nEksik: {missing} puan!", Colors.Yellow);
        }
    }

    private void TryStartMinigame()
    {
        if (player == null)
        {
            GD.PrintErr("[LEVEL] Player bulunamadı!");
            return;
        }

        if (player.TotalPoints == 0)
        {
            ShowMessage("Geri dönüşüm için çöpünüz yok! Önce çöp toplayın.", Colors.Red);
            GD.Print("[LEVEL] ❌ Çöp yok!");
            return;
        }

        StartMinigame();
    }

    private void StartMinigame()
    {
        var minigame = RecyclingMinigameScene.Instantiate<RecyclingMinigame>();
        AddChild(minigame);

        int[] collectedTrash = player.GetAllPoints();
        minigame.Setup(collectedTrash, player);

        GetTree().CallDeferred("set_pause", true);

        GD.Print($"[LEVEL] Minigame başladı! Çöp sayısı: {player.TotalPoints}");
    }

    public void OnMinigameFinished(int correct, int wrong, int minigameScore)
    {
        GD.Print($"[LEVEL] Minigame bitti! Puan: {minigameScore}");

        currentLevelScore += minigameScore;

        if (player != null)
        {
            player.UpdateScoresUI(currentLevelScore, MinimumScore);
        }

        if (currentLevelScore >= MinimumScore)
        {
            ShowMessage($"Tebrikler! {currentLevelScore} puan topladın!\n(Hedef: {MinimumScore})", Colors.Green);
            levelCompleted = true;
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
        levelCompleted = true;
        currentLevelScore = 0;

        ShowMessage($"TEBRİKLER! Level geçildi!", Colors.Green);
        GD.Print($"[LEVEL] ✅ GEÇTİN!");

        GetTree().CreateTimer(3.0).Timeout += () =>
        {
            if (ResourceLoader.Exists("res://Assets/Scenes/Areas/Level2.tscn"))
            {
                GetTree().ChangeSceneToFile("res://Assets/Scenes/Areas/Level2.tscn");
            }
            else
            {
                GD.Print("[LEVEL] Level2 yok, Level1 tekrar başlıyor!");
                GetTree().ReloadCurrentScene();
            }
        };
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