using Godot;
using System;

public partial class PointsGoldTrash : Area2D
{
    private AnimatedSprite2D animatedSprite;
    [Export] public int MinPoints = 10;
    [Export] public int MaxPoints = 25;
    private Node2D secretLevel = null;
    private bool isCollected = false;

    public override void _Ready()
    {
        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        animatedSprite.Play("BigPoints");
        animatedSprite.Frame = 0;
        animatedSprite.Pause();

        BodyEntered += OnBodyEntered;
        AddToGroup("points");

        GD.Print($"[GOLD TRASH] ✅ {Name} hazır");
    }

    public void SetSecretLevel(Node2D level)
    {
        secretLevel = level;
        GD.Print($"[GOLD TRASH] Secret level referansı alındı");
    }

    private void OnBodyEntered(Node2D body)
    {
        if (isCollected || !body.IsInGroup("player")) return;
        isCollected = true;

        GD.Print("[GOLD TRASH] 🎯 Altın çöp toplandı! Çıkış başlıyor...");

        // Random puan dağıt
        int totalPoints = GD.RandRange(MinPoints, MaxPoints);
        int plastic = 0, metal = 0, glass = 0, food = 0, wood = 0;

        for (int i = 0; i < totalPoints; i++)
        {
            int category = GD.RandRange(1, 5);
            switch (category)
            {
                case 1: plastic++; break;
                case 2: metal++; break;
                case 3: glass++; break;
                case 4: food++; break;
                case 5: wood++; break;
            }
        }

        // Player'a ekle
        if (plastic > 0 && body.HasMethod("AddPlastic"))
            body.Call("AddPlastic", plastic);
        if (metal > 0 && body.HasMethod("AddMetal"))
            body.Call("AddMetal", metal);
        if (glass > 0 && body.HasMethod("AddGlass"))
            body.Call("AddGlass", glass);
        if (food > 0 && body.HasMethod("AddFood"))
            body.Call("AddFood", food);
        if (wood > 0 && body.HasMethod("AddWood"))
            body.Call("AddWood", wood);

        GD.Print($"[GOLD TRASH] 🗑️ Toplandı! P:{plastic} M:{metal} G:{glass} F:{food} W:{wood}");

        // ✅ CRITICAL FIX: DOĞRUDAN Secret Level Exit'i tetikle!
        CallDeferred(nameof(TriggerSecretExit), body);
    }

    // ✅ YENİ: Secret level'den çıkışı tetikle
    private void TriggerSecretExit(Node2D player)
    {
        if (player == null || !IsInstanceValid(player))
        {
            GD.PrintErr("[GOLD TRASH] ❌ Player invalid!");
            return;
        }

        var root = GetTree().Root;
        var secretID = "";

        // ✅ Secret ID'yi al
        if (root.HasMeta("CurrentSecretID"))
        {
            secretID = (string)root.GetMeta("CurrentSecretID");
            GD.Print($"[GOLD TRASH] Secret ID: {secretID}");
        }

        // ✅ Player state'i kaydet
        SavePlayerState(player);

        // ✅ Secret'i tamamlandı olarak işaretle
        if (!string.IsNullOrEmpty(secretID))
        {
            root.SetMeta($"SecretCompleted_{secretID}", true);
            GD.Print($"[GOLD TRASH] ✅ {secretID} tamamlandı olarak işaretlendi!");
        }

        // ✅ Ana level'e dön
        string mainLevelPath = GetMainLevelPath();

        GD.Print($"[GOLD TRASH] 🚪 Ana level'e dönülüyor: {mainLevelPath}");

        // ✅ Self'i yok et
        QueueFree();

        // ✅ Scene değiştir
        GetTree().CallDeferred("change_scene_to_file", mainLevelPath);
    }

    // ✅ YENİ: Main level path'ini bul
    private string GetMainLevelPath()
    {
        var root = GetTree().Root;

        if (root.HasMeta("CurrentSecretID"))
        {
            string secretID = (string)root.GetMeta("CurrentSecretID");

            // ✅ Secret ID'ye göre ana level path'ini belirle
            if (secretID.StartsWith("level1"))
            {
                return "res://Assets/Scenes/Areas/area_1.tscn";
            }
            else if (secretID.StartsWith("level2"))
            {
                return "res://Assets/Scenes/Areas/Level2.tscn";
            }
        }

        // ✅ Default fallback
        return "res://Assets/Scenes/Areas/area_1.tscn";
    }

    // ✅ Player state'i kaydet
    private void SavePlayerState(Node2D player)
    {
        var root = GetTree().Root;

        try
        {
            // ✅ Çöpleri kaydet
            if (player.HasMethod("GetAllPoints"))
            {
                int[] trashCounts = (int[])player.Call("GetAllPoints");
                root.SetMeta("SavedTrash_Plastic", trashCounts[0]);
                root.SetMeta("SavedTrash_Metal", trashCounts[1]);
                root.SetMeta("SavedTrash_Glass", trashCounts[2]);
                root.SetMeta("SavedTrash_Food", trashCounts[3]);
                root.SetMeta("SavedTrash_Wood", trashCounts[4]);

                int total = trashCounts[0] + trashCounts[1] + trashCounts[2] + trashCounts[3] + trashCounts[4];
                GD.Print($"[GOLD TRASH] 💾 Çöpler kaydedildi: {total} adet");
            }

            // ✅ Kostüm kaydet
            if (player.HasMethod("GetCurrentCostumeIndex"))
            {
                int costumeIndex = (int)player.Call("GetCurrentCostumeIndex");
                root.SetMeta("SavedCostume", costumeIndex);
                GD.Print($"[GOLD TRASH] 💾 Kostüm kaydedildi: {costumeIndex}");
            }

            // ✅ Can kaydet
            if (player.HasMethod("GetCurrentHealth"))
            {
                int currentHealth = (int)player.Call("GetCurrentHealth");
                int maxHealth = (int)player.Get("MaxHealth");

                root.SetMeta("SavedHealth", currentHealth);
                root.SetMeta("SavedMaxHealth", maxHealth);

                GD.Print($"[GOLD TRASH] 💾 Can kaydedildi: {currentHealth}/{maxHealth}");
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"[GOLD TRASH] ❌ SavePlayerState hatası: {e.Message}");
        }
    }
}