using Godot;
using System;

public partial class TailorAunt : Area2D
{
    [Export] public PackedScene MathMinigameScene;

    [ExportGroup("Minigame Ayarları")]
    [Export] public int QuestionCount = 2;
    [Export] public float TimeLimit = 30f;
    [Export] public string Difficulty = "Orta";

    [ExportGroup("Tailor Ayarları")]
    [Export] public bool UseActiveSlot = true;
    [Export] public int TargetCostumeSlot = 0;

    private bool playerInRange = false;
    private Node2D player;
    private Label interactionLabel;

    public override void _Ready()
    {
        // ✅ HER ZAMAN UserProfile'dan zorluğu al!
        Difficulty = UserProfile.Instance.Difficulty;
        GD.Print($"[TAILOR] 🧵 Zorluk: {Difficulty}");

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
            GD.PrintErr("[TAILOR] MathMinigameScene atanmamış!");
            return;
        }

        if (player == null)
        {
            GD.PrintErr("[TAILOR] Player bulunamadı!");
            return;
        }

        int targetSlot = GetTargetCostumeSlot();

        if (targetSlot < 0)
        {
            GD.Print("[TAILOR] ⚠️ Aktif kostüm yok veya geçersiz slot!");
            return;
        }

        GD.Print($"[TAILOR] 🧵 Minigame başlıyor - Slot: {targetSlot}, Zorluk: {Difficulty}");

        var minigame = MathMinigameScene.Instantiate<MathMinigame>();
        minigame.QuestionCount = QuestionCount;
        minigame.TimeLimit = TimeLimit;
        minigame.Difficulty = Difficulty;
        minigame.GameType = MathMinigame.MinigameType.Tailor;
        minigame.CostumeSlotIndex = targetSlot;
        minigame.OnMinigameComplete = OnMinigameResult;

        GetTree().CurrentScene.AddChild(minigame);
        GetTree().Paused = true;
        minigame.ProcessMode = ProcessModeEnum.Always;
    }

    private int GetTargetCostumeSlot()
    {
        if (player == null)
            return -1;

        if (UseActiveSlot)
        {
            if (player.HasMethod("GetCurrentCostumeIndex"))
            {
                try
                {
                    Variant result = player.Call("GetCurrentCostumeIndex");
                    int activeSlot = result.AsInt32();
                    return activeSlot;
                }
                catch
                {
                    return TargetCostumeSlot;
                }
            }
            else
            {
                return TargetCostumeSlot;
            }
        }

        return TargetCostumeSlot;
    }

    private void OnMinigameResult(int correct, int wrong, int total)
    {
        if (player == null)
        {
            // ✅ YİNE DE SİL!
            QueueFree();
            GD.Print("[TAILOR] 👋 Tailor NPC yok oldu! (Player null)");
            return;
        }

        int targetSlot = GetTargetCostumeSlot();

        if (targetSlot >= 0)
        {
            float successRate = total > 0 ? (float)correct / total : 0;

            // %100 doğru = Kostüm yenilenir
            if (wrong == 0 && correct == total)
            {
                if (player.HasMethod("HealCostumeSlot"))
                {
                    player.Call("HealCostumeSlot", targetSlot);
                    GD.Print($"[TAILOR] ✅ Kostüm slot {targetSlot} yenilendi!");
                }
            }
            // %50'den az = Kostüm yok olur
            else if (successRate < 0.5f)
            {
                if (player.HasMethod("DestroyCostumeSlot"))
                {
                    player.Call("DestroyCostumeSlot", targetSlot);
                    GD.Print($"[TAILOR] ❌ Kostüm slot {targetSlot} yok edildi!");
                }
            }
            else
            {
                GD.Print("[TAILOR] ⚠️ Sonuç belirsiz, hiçbir şey olmadı.");
            }
        }

        // ✅ MINIGAME BİTTİ, TAILOR NPC'Yİ SİL!
        QueueFree();
        GD.Print("[TAILOR] 👋 Tailor NPC yok oldu!");
    }
}