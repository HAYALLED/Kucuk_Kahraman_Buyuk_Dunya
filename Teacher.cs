using Godot;
using System;

public partial class Teacher : Area2D
{
    [Export] public PackedScene MathMinigameScene;

    [ExportGroup("Minigame Ayarları")]
    [Export] public int QuestionCount = 2;
    [Export] public float TimeLimit = 30f;
    [Export] public string Difficulty = "Orta";

    [ExportGroup("Puan Ayarları")]
    [Export] public int PointsPerCorrect = 10;
    [Export] public int PointsPerWrong = -5;

    private bool playerInRange = false;
    private Node2D player;
    private Label interactionLabel;

    public override void _Ready()
    {
        // ✅ HER ZAMAN UserProfile'dan zorluğu al!
        Difficulty = UserProfile.Instance.Difficulty;
        GD.Print($"[TEACHER] 🎓 Zorluk: {Difficulty}");

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        interactionLabel = GetNodeOrNull<Label>("InteractionLabel");
        if (interactionLabel != null)
            interactionLabel.Visible = false;

        CollisionMask = 2;
    }

    public override void _Process(double delta)
    {
        if (playerInRange && Input.IsActionJustPressed("interaction"))
        {
            StartMinigame();
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInRange = true;
            player = body;

            if (interactionLabel != null)
                interactionLabel.Visible = true;
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInRange = false;
            player = null;

            if (interactionLabel != null)
                interactionLabel.Visible = false;
        }
    }

    private void StartMinigame()
    {
        if (MathMinigameScene == null)
        {
            GD.PrintErr("[TEACHER] MathMinigameScene atanmamış!");
            return;
        }

        var minigame = MathMinigameScene.Instantiate<MathMinigame>();
        minigame.QuestionCount = QuestionCount;
        minigame.TimeLimit = TimeLimit;
        minigame.Difficulty = Difficulty;
        minigame.GameType = MathMinigame.MinigameType.Teacher;
        minigame.OnMinigameComplete = OnMinigameResult;

        GetTree().CurrentScene.AddChild(minigame);
        GetTree().Paused = true;
        minigame.ProcessMode = ProcessModeEnum.Always;

        GD.Print($"[TEACHER] 📚 Minigame başlatıldı - Zorluk: {Difficulty}");
    }

    private void OnMinigameResult(int correct, int wrong, int total)
    {
        int points = (correct * PointsPerCorrect) + (wrong * PointsPerWrong);

        if (player != null && player.HasMethod("UpdateTeacherScore"))
        {
            player.Call("UpdateTeacherScore", points);
            GD.Print($"[TEACHER] ✅ {points} puan verildi!");
        }

        GD.Print($"[TEACHER] Sonuç: {correct} doğru, {wrong} yanlış = {points} puan");

        // ✅ MINIGAME BİTTİ, TEACHER NPC'Yİ SİL!
        QueueFree();
        GD.Print("[TEACHER] 👋 Teacher NPC yok oldu!");
    }
}