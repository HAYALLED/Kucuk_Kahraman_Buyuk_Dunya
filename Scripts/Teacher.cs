using Godot;
using System;

public partial class Teacher : Area2D
{
    [Export] public PackedScene MathMinigameScene;

    [ExportGroup("Minigame Ayarları")]
    [Export] public int QuestionCount = 2;
    [Export] public float TimeLimit = 30f;
    [Export] public string Difficulty = "";

    [ExportGroup("Puan Ayarları")]
    [Export] public int PointsPerCorrect = 10;
    [Export] public int PointsPerWrong = -5;

    private bool playerInRange = false;
    private Node2D player;
    private Label interactionLabel;

    public override void _Ready()
    {
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
        if (MathMinigameScene == null) return;

        var minigame = MathMinigameScene.Instantiate<MathMinigame>();
        minigame.QuestionCount = QuestionCount;
        minigame.TimeLimit = TimeLimit;
        minigame.Difficulty = Difficulty;
        minigame.GameType = MathMinigame.MinigameType.Teacher;

        // Callback - sonuç gelince puan ver
        minigame.OnMinigameComplete = OnMinigameResult;

        GetTree().CurrentScene.AddChild(minigame);
        GetTree().CallDeferred("set_pause", true);
        minigame.ProcessMode = ProcessModeEnum.Always;
    }

    private void OnMinigameResult(int correct, int wrong, int total)
    {
        int points = (correct * PointsPerCorrect) + (wrong * PointsPerWrong);

        // ✅ BASIT ÇÖZÜM: Player'ın UpdateTeacherScore'unu çağır (AddMetal gibi!)
        if (player != null && player.HasMethod("UpdateTeacherScore"))
        {
            player.Call("UpdateTeacherScore", points);
            GD.Print($"[TEACHER] ✅ Player.UpdateTeacherScore çağrıldı: {points} puan");
        }
        else
        {
            GD.PrintErr("[TEACHER] ❌ Player'da UpdateTeacherScore metodu yok!");
        }

        GD.Print($"[TEACHER] Sonuç: {correct} doğru, {wrong} yanlış = {points} puan");
    }
}