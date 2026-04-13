using Godot;
using System;

public partial class Level3 : Node2D
{
    [Export] public int MinimumScore = 100;

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

        CreateMessageLabel();
        AddPauseMenu();
        CheckReturnFromSettings();

        Vector2? returnPos = GetSecretReturnPosition();

        CallDeferred(nameof(FindPlayer));
        CallDeferred(nameof(RestorePlayerTrash));
        CallDeferred(nameof(RestorePlayerState));
        CallDeferred(nameof(RestoreLeverStates));

        if (returnPos.HasValue)
            CallDeferred(nameof(SetPlayerSpawnPosition), returnPos.Value);
    }

    private void RestoreLeverStates()
    {
        var root = GetTree().Root;
        if (!root.HasMeta("SavedLeverStates")) return;

        var states = (Godot.Collections.Dictionary)root.GetMeta("SavedLeverStates");

        foreach (var key in states.Keys)
        {
            var node = GetNodeOrNull<lever>(key.ToString());
            if (node != null)
            {
                node.ForceActivate((bool)states[key]);
                GD.Print($"[LEVEL] Lever restore: {key} = {states[key]}");
            }
        }

        root.RemoveMeta("SavedLeverStates");
        GD.Print("[LEVEL] Lever durumları geri yüklendi.");
    }

    private void CheckReturnFromSettings()
    {
        if (GetTree().Root.HasMeta("ReturnToPause"))
        {
            GetTree().CreateTimer(0.1).Timeout += () =>
            {
                GetTree().Paused = true;
                var pauseMenu = GetNodeOrNull<CanvasLayer>("PauseMenu");
                if (pauseMenu != null) pauseMenu.Show();
            };
            GetTree().Root.RemoveMeta("ReturnToPause");
        }
    }

    private void AddPauseMenu()
    {
        var pauseScene = GD.Load<PackedScene>("res://Resources/PauseMenu.tscn");
        var pauseMenu = pauseScene.Instantiate();
        AddChild(pauseMenu);
    }

    private void RestorePlayerTrash()
    {
        var player = GetNodeOrNull<Player_controller>("Player");
        if (player == null)
        {
            var players = GetTree().GetNodesInGroup("player");
            if (players.Count > 0) player = players[0] as Player_controller;
        }
        if (player == null) return;

        var root = GetTree().Root;
        if (!root.HasMeta("SavedTrash_Metal")) return;

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

        root.RemoveMeta("SavedTrash_Metal");
        root.RemoveMeta("SavedTrash_Glass");
        root.RemoveMeta("SavedTrash_Plastic");
        root.RemoveMeta("SavedTrash_Food");
        root.RemoveMeta("SavedTrash_Wood");
    }

    private void RestorePlayerState()
    {
        var player = GetNodeOrNull<Player_controller>("Player");
        if (player == null)
        {
            var players = GetTree().GetNodesInGroup("player");
            if (players.Count > 0) player = players[0] as Player_controller;
        }
        if (player == null) return;

        var root = GetTree().Root;

        try
        {
            if (root.HasMeta("SavedHealth"))
            {
                int health = (int)root.GetMeta("SavedHealth");
                int maxHealth = (int)root.GetMeta("SavedMaxHealth");

                player.MaxHealth = maxHealth;

                int diff = health - player.GetCurrentHealth();
                if (diff > 0) player.Heal(diff);
                else if (diff < 0) player.TakeDamage(-diff);

                player.UpdateHealthUI();

                root.RemoveMeta("SavedHealth");
                root.RemoveMeta("SavedMaxHealth");
            }

            if (root.HasMeta("SavedCostume"))
            {
                int costumeIndex = (int)root.GetMeta("SavedCostume");
                Callable.From(() =>
                {
                    if (player != null && IsInstanceValid(player))
                        player.Call("RestoreCostume", costumeIndex);
                }).CallDeferred();
                root.RemoveMeta("SavedCostume");
            }

            Callable.From(() =>
            {
                if (player != null && IsInstanceValid(player))
                    player.UpdateHealthUI();
            }).CallDeferred();
        }
        catch (Exception e)
        {
            GD.PrintErr($"[LEVEL] RestorePlayerState hatası: {e.Message}");
        }
    }

    private Vector2? GetSecretReturnPosition()
    {
        if (!GetTree().Root.HasMeta("ReturnFromSecret")) return null;
        Vector2 pos = (Vector2)GetTree().Root.GetMeta("ReturnFromSecret");
        GetTree().Root.RemoveMeta("ReturnFromSecret");
        return pos;
    }

    private void SetPlayerSpawnPosition(Vector2 pos)
    {
        if (player == null) player = GetNodeOrNull<Player_controller>("Player");
        if (player == null)
        {
            var players = GetTree().GetNodesInGroup("player");
            if (players.Count > 0) player = players[0] as Player_controller;
        }
        if (player != null) player.GlobalPosition = pos;
    }

    private void FindPlayer()
    {
        player = GetNodeOrNull<Player_controller>("player");
        if (player != null)
            player.UpdateScoresUI(currentLevelScore, MinimumScore);
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
        if (player != null) player.UpdateScoresUI(currentLevelScore, MinimumScore);

        if (currentLevelScore >= MinimumScore)
        {
            ShowMessage($"Harika! {MinimumScore} puana ulaştınız!", Colors.Green);
            GetTree().CreateTimer(3.0).Timeout += LevelPassed;
        }
    }

    public void AddMinigameScore(int points)
    {
        currentLevelScore += points;

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

        SaveLevelScore();
        SaveGame.Instance.MarkLevelCompleted("level_3");
        currentLevelScore = 0;

        ShowMessage("TEBRİKLER! Level 3 geçildi!", Colors.Green);

        GetTree().CreateTimer(3.0).Timeout += () =>
        {
            if (ResourceLoader.Exists("res://Assets/Scenes/Areas/Level4.tscn"))
                GetTree().ChangeSceneToFile("res://Assets/Scenes/Areas/Level4.tscn");
            else
                GetTree().ChangeSceneToFile("res://Resources/level_select.tscn");
        };
    }

    private void SaveLevelScore()
    {
        try
        {
            int userId = SaveGame.Instance.GetCurrentUserId();
            if (userId <= 0) return;
            Database.SaveScore(userId, 3, currentLevelScore);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[LEVEL] SaveLevelScore hatası: {ex.Message}");
        }
    }

    private void LevelFailed()
    {
        int remaining = MinimumScore - currentLevelScore;
        ShowMessage($"Yetersiz! Daha {remaining} puan gerekli.", Colors.Orange);
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
        if (player != null) player.UpdateScoresUI(currentLevelScore, MinimumScore);
    }
}