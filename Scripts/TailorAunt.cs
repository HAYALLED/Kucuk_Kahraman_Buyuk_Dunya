using Godot;
using System;

public partial class TailorAunt : Area2D
{
    [Export] public PackedScene MathMinigameScene;

    [ExportGroup("Minigame Ayarları")]
    [Export] public int QuestionCount = 2;
    [Export] public float TimeLimit = 30f;
    [Export] public string Difficulty = "";

    [ExportGroup("Tailor Ayarları")]
    [Export] public bool UseActiveSlot = true;  // Aktif slot'u mu kullan?
    [Export] public int TargetCostumeSlot = 0;  // Manuel slot seçimi (UseActiveSlot = false ise)

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

        // ✅ Aktif kostüm slot'unu al
        int targetSlot = GetTargetCostumeSlot();

        if (targetSlot < 0)
        {
            GD.Print("[TAILOR] ⚠️ Aktif kostüm yok veya geçersiz slot!");
            return;
        }

        GD.Print($"[TAILOR] Minigame başlıyor - Hedef slot: {targetSlot}");

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

    // ✅ DÜZELTİLMİŞ: Hedef kostüm slot'unu belirle
    private int GetTargetCostumeSlot()
    {
        if (player == null)
            return -1;

        if (UseActiveSlot)
        {
            // Player'ın aktif kostüm index'ini al
            if (player.HasMethod("GetCurrentCostumeIndex"))
            {
                try
                {
                    // ✅ DÜZELTME: Variant'ı doğru şekilde int'e çevir
                    Variant result = player.Call("GetCurrentCostumeIndex");
                    int activeSlot = result.AsInt32();

                    GD.Print($"[TAILOR] ✅ Aktif slot kullanılıyor: {activeSlot}");
                    return activeSlot;
                }
                catch (Exception e)
                {
                    GD.PrintErr($"[TAILOR] ❌ Slot alınırken hata: {e.Message}");
                    GD.Print($"[TAILOR] Manuel slot'a geçiliyor: {TargetCostumeSlot}");
                    return TargetCostumeSlot;
                }
            }
            else
            {
                GD.PrintErr("[TAILOR] ❌ Player'da GetCurrentCostumeIndex metodu yok!");
                GD.PrintErr("[TAILOR] Player_controller.cs'ye ekle:");
                GD.PrintErr("    public int GetCurrentCostumeIndex()");
                GD.PrintErr("    {");
                GD.PrintErr("        return currentCostumeIndex;");
                GD.PrintErr("    }");
                GD.Print($"[TAILOR] Manuel slot kullanılıyor: {TargetCostumeSlot}");
                return TargetCostumeSlot;
            }
        }

        // UseActiveSlot = false ise manuel slot kullan
        GD.Print($"[TAILOR] Manuel slot kullanılıyor: {TargetCostumeSlot}");
        return TargetCostumeSlot;
    }

    private void OnMinigameResult(int correct, int wrong, int total)
    {
        if (player == null)
        {
            GD.PrintErr("[TAILOR] Player kayboldu!");
            return;
        }

        // ✅ Aktif slot'u tekrar al
        int targetSlot = GetTargetCostumeSlot();

        if (targetSlot < 0)
        {
            GD.PrintErr("[TAILOR] ⚠️ Geçersiz slot!");
            return;
        }

        float successRate = total > 0 ? (float)correct / total : 0;

        GD.Print($"[TAILOR] 📊 Sonuç: {correct}/{total} doğru ({successRate * 100:F0}%)");

        // %100 doğru (0 yanlış) = Kostüm yenilenir
        if (wrong == 0 && correct == total)
        {
            if (player.HasMethod("HealCostumeSlot"))
            {
                player.Call("HealCostumeSlot", targetSlot);
                GD.Print($"[TAILOR] ✅ Kostüm slot {targetSlot} yenilendi!");
            }
            else
            {
                GD.PrintErr("[TAILOR] ⚠️ Player'da HealCostumeSlot metodu yok!");
            }
        }
        // %50'den fazla yanlış = Kostüm yok olur
        else if (successRate < 0.5f)
        {
            if (player.HasMethod("DestroyCostumeSlot"))
            {
                player.Call("DestroyCostumeSlot", targetSlot);
                GD.Print($"[TAILOR] ❌ Kostüm slot {targetSlot} yok edildi!");
            }
            else
            {
                GD.PrintErr("[TAILOR] ⚠️ Player'da DestroyCostumeSlot metodu yok!");
            }
        }
        // Arada = Hiçbir şey olmaz
        else
        {
            GD.Print("[TAILOR] ⚠️ Sonuç belirsiz, hiçbir şey olmadı.");
        }
    }
}