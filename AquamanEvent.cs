using Godot;
using System;

public partial class AquamanEvent : Area2D
{
    [Export] public PackedScene MathMinigameScene;

    [ExportGroup("Minigame Ayarları")]
    [Export] public int QuestionCount = 3;
    [Export] public float TimeLimit = 45f;
    [Export] public string Difficulty = "Orta"; // ✅ Inspector'da default "Orta"

    [ExportGroup("Ödül Kostümü")]
    [Export] public CostumeResource RewardCostume;
    [Export] public int TargetSlot = 2;

    [ExportGroup("Ceza Ayarları")]
    [Export] public int FailDamage = 1;
    [Export] public float KnockbackForce = 300f;

    private bool playerInRange = false;
    private Node2D player;
    private Label interactionLabel;

    public override void _Ready()
    {
        // ✅ HER ZAMAN UserProfile'dan zorluğu al!
        Difficulty = UserProfile.Instance.Difficulty;
        GD.Print($"[AQUAMAN EVENT] 🌊 Zorluk: {Difficulty}");

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        interactionLabel = GetNodeOrNull<Label>("InteractionLabel");
        if (interactionLabel != null)
            interactionLabel.Visible = false;

        CollisionMask = 2;
        AddToGroup("interactable");
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
            GD.PrintErr("[AQUAMAN EVENT] MathMinigameScene null!");
            return;
        }

        var minigame = MathMinigameScene.Instantiate<MathMinigame>();
        minigame.QuestionCount = QuestionCount;
        minigame.TimeLimit = TimeLimit;
        minigame.Difficulty = Difficulty; // ✅ UserProfile'dan gelen!
        minigame.GameType = MathMinigame.MinigameType.SpecialEvent;
        minigame.RewardCostume = RewardCostume;

        minigame.OnMinigameComplete = OnMinigameResult;

        GetTree().CurrentScene.AddChild(minigame);
        GetTree().Paused = true;
        minigame.ProcessMode = ProcessModeEnum.Always;

        GD.Print($"[AQUAMAN EVENT] 🌊 Minigame başladı - Zorluk: {Difficulty}!");
    }

    private void OnMinigameResult(int correct, int wrong, int total)
    {
        if (player == null) return;

        GD.Print($"[AQUAMAN EVENT] Sonuç: {correct} doğru, {wrong} yanlış");

        if (correct == 3 && wrong == 0)
        {
            GiveTemporaryCostume(-1);
            GD.Print("[AQUAMAN EVENT] ✅ MÜKEMMEL! Level boyunca Aquaman kostümü!");
        }
        else if (correct == 2 && wrong == 1)
        {
            GiveTemporaryCostume(80f);
            GD.Print("[AQUAMAN EVENT] ✅ İYİ! 80 saniye Aquaman kostümü!");
        }
        else if (correct == 1 && wrong == 2)
        {
            GD.Print("[AQUAMAN EVENT] ⚠️ Yetersiz... Hiçbir şey olmadı.");
        }
        else if (correct == 0 && wrong == 3)
        {
            ApplyPunishment();
            GD.Print("[AQUAMAN EVENT] ❌ FELAKET! Hasar ve knockback!");
        }

        QueueFree();
    }

    private void GiveTemporaryCostume(float duration)
    {
        if (RewardCostume == null)
        {
            GD.PrintErr("[AQUAMAN EVENT] RewardCostume null!");
            return;
        }

        if (player.HasMethod("AddTemporaryCostume"))
        {
            player.Call("AddTemporaryCostume", RewardCostume, TargetSlot, duration);
            GD.Print($"[AQUAMAN EVENT] Kostüm verildi: {duration}sn");
        }
    }

    private void ApplyPunishment()
    {
        if (player.HasMethod("TakeDamage"))
        {
            player.Call("TakeDamage", FailDamage);
        }

        if (player is CharacterBody2D playerBody)
        {
            Vector2 knockDir = (player.GlobalPosition - GlobalPosition).Normalized();
            playerBody.Velocity = knockDir * KnockbackForce;
            GD.Print("[AQUAMAN EVENT] Knockback uygulandı!");
        }
    }
}