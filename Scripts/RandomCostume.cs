using Godot;
using System;

public partial class RandomCostume : Area2D
{
    private AnimatedSprite2D animatedSprite;
    private bool _alreadyCollected = false;  // ✅ YENİ: Çift toplama engelle

    [ExportGroup("Kostüm Ayarları")]
    [Export] public CostumeResource[] AvailableCostumes;
    [Export] public SpriteFrames Sprites;

    public override void _Ready()
    {
        animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

        if (animatedSprite != null)
        {
            animatedSprite.Play();
        }

        GD.Print("========== RANDOM COSTUME READY ==========");
        if (AvailableCostumes == null || AvailableCostumes.Length == 0)
        {
            GD.PrintErr("[RandomCostume] ❌ AvailableCostumes BOŞ! Inspector'da ayarla!");
        }
        else
        {
            GD.Print($"[RandomCostume] ✅ {AvailableCostumes.Length} kostüm mevcut:");
            for (int i = 0; i < AvailableCostumes.Length; i++)
            {
                if (AvailableCostumes[i] != null)
                    GD.Print($"  - [{i}] {AvailableCostumes[i].CostumeName}");
                else
                    GD.PrintErr($"  - [{i}] NULL!");
            }
        }
        GD.Print("==========================================");

        BodyEntered += OnBodyEntered;
    }

    // ✅ CRITICAL FIX: Physics callback'den HEMEN çık, işlemleri ertele
    private void OnBodyEntered(Node2D body)
    {
        if (_alreadyCollected) return;

        if (body.IsInGroup("player") && body is Player_controller player)
        {
            _alreadyCollected = true;
            GD.Print($"[RandomCostume] 🎮 Player temas etti!");

            // ✅ TÜM İŞLEMLERİ ERTELE!
            CallDeferred(nameof(ProcessCostumePickup), player);
        }
    }

    // ✅ YENİ: Physics callback DIŞINDA çalışır
    private void ProcessCostumePickup(Player_controller player)
    {
        if (player == null || !IsInstanceValid(player))
        {
            GD.PrintErr("[RandomCostume] ❌ Player geçersiz!");
            QueueFree();
            return;
        }

        GiveCostumeToPlayer(player);
        QueueFree();
    }

    private void GiveCostumeToPlayer(Player_controller player)
    {
        if (AvailableCostumes == null || AvailableCostumes.Length == 0)
        {
            GD.PrintErr("[RandomCostume] ❌ AvailableCostumes boş!");
            return;
        }

        int randomIndex = GD.RandRange(0, AvailableCostumes.Length - 1);
        CostumeResource newCostume = AvailableCostumes[randomIndex];

        if (newCostume == null)
        {
            GD.PrintErr($"[RandomCostume] ❌ Index {randomIndex}'deki kostüm null!");
            return;
        }

        GD.Print($"[RandomCostume] 🎲 Seçilen kostüm: {newCostume.CostumeName}");
        GD.Print("[RandomCostume] 📦 Player'ın mevcut kostümleri:");

        for (int i = 0; i < player.CostumeSlots.Length; i++)
        {
            if (player.CostumeSlots[i] != null)
                GD.Print($"  - Slot {i}: {player.CostumeSlots[i].CostumeName}");
            else
                GD.Print($"  - Slot {i}: BOŞ");
        }

        int existingSlotIndex = -1;
        for (int i = 0; i < player.CostumeSlots.Length; i++)
        {
            if (player.CostumeSlots[i] != null &&
                player.CostumeSlots[i].CostumeName == newCostume.CostumeName)
            {
                existingSlotIndex = i;
                break;
            }
        }

        if (existingSlotIndex >= 0)
        {
            GD.Print($"[RandomCostume] 💚 Aynı kostüm var (Slot {existingSlotIndex}), can dolduruluyor!");
            HealCostume(player, existingSlotIndex);
        }
        else
        {
            GD.Print("[RandomCostume] 🆕 Yeni kostüm ekleniyor!");
            AddOrSwapCostume(player, newCostume);
        }
    }

    private void HealCostume(Player_controller player, int slotIndex)
    {
        GD.Print($"[RandomCostume] 🩹 Slot {slotIndex} ({player.CostumeSlots[slotIndex].CostumeName}) canı dolduruluyor!");
        player.HealCostumeSlot(slotIndex);
    }

    private void AddOrSwapCostume(Player_controller player, CostumeResource newCostume)
    {
        int emptySlotIndex = -1;
        for (int i = 0; i < player.CostumeSlots.Length; i++)
        {
            if (player.CostumeSlots[i] == null)
            {
                emptySlotIndex = i;
                break;
            }
        }

        if (emptySlotIndex >= 0)
        {
            GD.Print($"[RandomCostume] 📥 Boş slot bulundu (Slot {emptySlotIndex}), kostüm ekleniyor!");
            player.SetCostumeAndEquip(emptySlotIndex, newCostume);
        }
        else
        {
            GD.Print("[RandomCostume] 🔄 Boş slot yok, aktif kostüm değiştiriliyor!");
            SwapWithActiveCostume(player, newCostume);
        }
    }

    private void SwapWithActiveCostume(Player_controller player, CostumeResource newCostume)
    {
        int activeSlotIndex = player.GetCurrentCostumeIndex();

        if (activeSlotIndex < 0)
        {
            GD.Print("[RandomCostume] ⚠️ Aktif kostüm yok, Slot 0'a yerleştiriliyor!");
            player.SetCostumeAndEquip(0, newCostume);
            return;
        }

        var currentCostume = player.GetCurrentCostume();
        GD.Print($"[RandomCostume] 🔄 Slot {activeSlotIndex} ({currentCostume?.CostumeName ?? "NULL"}) yerine {newCostume.CostumeName} giyiliyor!");

        player.SetCostumeAndEquip(activeSlotIndex, newCostume);

        GD.Print($"[RandomCostume] ✅ Kostüm değiştirildi! Yeni aktif: {newCostume.CostumeName}");
    }
}