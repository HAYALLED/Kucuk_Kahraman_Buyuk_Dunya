using Godot;
using System;

public partial class Level2_1SclvlMain : Node2D
{
    [Export] public string MainLevelPath = "res://Assets/Scenes/Areas/Level2.tscn";

    private Area2D _playerSpawn;
    private int totalPoints = 0;
    private int collectedPoints = 0;
    private string secretLevelID = "";

    public override void _Ready()
    {
        GD.Print("========== SECRET LEVEL BAŞLADI ==========");

        if (GetTree().Root.HasMeta("CurrentSecretID"))
        {
            secretLevelID = (string)GetTree().Root.GetMeta("CurrentSecretID");
            GD.Print($"[SECRET LEVEL] ID: {secretLevelID}");
        }

        _playerSpawn = GetNodeOrNull<Area2D>("player_spawn");

        if (_playerSpawn != null)
        {
            GD.Print($"[SECRET LEVEL] ✅ player_spawn: {_playerSpawn.GlobalPosition}");
        }

        // ✅ ASYNC ile doğru sıralama
        CallDeferred(nameof(InitializeLevel));
    }

    private async void InitializeLevel()
    {
        // ✅ 1 frame bekle - tüm node'lar hazır olsun
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        SpawnPlayer();

        // ✅ 1 frame daha bekle - player spawn olsun
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        RestorePlayerTrash();
        RestorePlayerState();
        CountTotalPoints();
    }

    private void SpawnPlayer()
    {
        var player = GetTree().GetFirstNodeInGroup("player") as Node2D;

        if (player == null)
        {
            GD.PrintErr("[SECRET LEVEL] ❌ Player bulunamadı!");
            return;
        }

        if (_playerSpawn != null)
        {
            player.GlobalPosition = _playerSpawn.GlobalPosition;
            GD.Print($"[SECRET LEVEL] ✅ Player spawn: {_playerSpawn.GlobalPosition}");
        }
    }

    private void RestorePlayerTrash()
    {
        var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        if (player == null) return;

        var root = GetTree().Root;

        if (root.HasMeta("SavedTrash_Metal"))
        {
            int metal = (int)root.GetMeta("SavedTrash_Metal");
            int glass = (int)root.GetMeta("SavedTrash_Glass");
            int plastic = (int)root.GetMeta("SavedTrash_Plastic");
            int food = (int)root.GetMeta("SavedTrash_Food");
            int wood = (int)root.GetMeta("SavedTrash_Wood");

            if (metal > 0 && player.HasMethod("AddMetal"))
                player.Call("AddMetal", metal);
            if (glass > 0 && player.HasMethod("AddGlass"))
                player.Call("AddGlass", glass);
            if (plastic > 0 && player.HasMethod("AddPlastic"))
                player.Call("AddPlastic", plastic);
            if (food > 0 && player.HasMethod("AddFood"))
                player.Call("AddFood", food);
            if (wood > 0 && player.HasMethod("AddWood"))
                player.Call("AddWood", wood);

            int total = metal + glass + plastic + food + wood;
            GD.Print($"[SECRET LEVEL] 🔄 Çöpler geri yüklendi: {total} adet");
        }
    }
    private async void RestorePlayerState()
    {
        var player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        if (player == null)
        {
            GD.PrintErr("[SECRET LEVEL] ❌ Player bulunamadı!");
            return;
        }

        var root = GetTree().Root;

        try
        {
            // ✅ Can restore et
            if (root.HasMeta("SavedHealth"))
            {
                int health = (int)root.GetMeta("SavedHealth");
                int maxHealth = (int)root.GetMeta("SavedMaxHealth");

                player.Set("MaxHealth", maxHealth);

                int currentHealth = (int)player.Call("GetCurrentHealth");
                int diff = health - currentHealth;

                if (diff > 0)
                    player.Call("Heal", diff);
                else if (diff < 0)
                    player.Call("TakeDamage", -diff);

                GD.Print($"[SECRET LEVEL] 🔄 Can geri yüklendi: {health}/{maxHealth}");
            }

            // ✅ 2 frame bekle - collision'lar tamamen hazır olsun
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            // ✅ Kostüm restore - ARTIK GÜVENLİ!
            if (root.HasMeta("SavedCostume"))
            {
                int costumeIndex = (int)root.GetMeta("SavedCostume");

                if (player != null && IsInstanceValid(player))
                {
                    player.Call("RestoreCostume", costumeIndex);
                    GD.Print($"[SECRET LEVEL] ✅ Kostüm geri yüklendi: {costumeIndex}");
                }
            }

            // ✅ 1 frame daha bekle
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            // ✅ UI güncelle
            if (player != null && IsInstanceValid(player))
            {
                player.Call("UpdateHealthUI");
                GD.Print("[SECRET LEVEL] ✅ Tüm restore işlemi tamamlandı!");
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"[SECRET LEVEL] ❌ RestorePlayerState hatası: {e.Message}");
        }
    }
    private void CountTotalPoints()
    {
        var points = GetTree().GetNodesInGroup("points");
        totalPoints = points.Count;
        collectedPoints = 0;

        GD.Print($"[SECRET LEVEL] 📊 Toplam {totalPoints} çöp");

        foreach (var point in points)
        {
            if (point is Node2D pointNode && pointNode.HasMethod("SetSecretLevel"))
            {
                pointNode.Call("SetSecretLevel", this);
            }
        }
    }

    public void OnPointCollected()
    {
        collectedPoints++;
        GD.Print($"[SECRET LEVEL] 🗑️ Çöp toplandı! {collectedPoints}/{totalPoints}");

        if (collectedPoints >= totalPoints)
        {
            OnAllPointsCollected();
        }
    }

    private void OnAllPointsCollected()
    {
        GD.Print("[SECRET LEVEL] ✅ Tamamlandı!");

        var player = GetTree().GetFirstNodeInGroup("player") as Node2D;

        if (player != null)
        {
            // Çöpleri kaydet
            if (player.HasMethod("GetAllPoints"))
            {
                int[] trashCounts = (int[])player.Call("GetAllPoints");
                GetTree().Root.SetMeta("SavedTrash_Plastic", trashCounts[0]);
                GetTree().Root.SetMeta("SavedTrash_Metal", trashCounts[1]);
                GetTree().Root.SetMeta("SavedTrash_Glass", trashCounts[2]);
                GetTree().Root.SetMeta("SavedTrash_Food", trashCounts[3]);
                GetTree().Root.SetMeta("SavedTrash_Wood", trashCounts[4]);

                int total = trashCounts[0] + trashCounts[1] + trashCounts[2] + trashCounts[3] + trashCounts[4];
                GD.Print($"[SECRET LEVEL] 💾 Güncel çöpler kaydedildi: {total} adet");
            }

            // Kostüm ve canı kaydet
            SavePlayerState(player);
        }

        if (!string.IsNullOrEmpty(secretLevelID))
        {
            GetTree().Root.SetMeta($"SecretCompleted_{secretLevelID}", true);
        }

        GetTree().CallDeferred("change_scene_to_file", MainLevelPath);
    }

    private void SavePlayerState(Node2D player)
    {
        var root = GetTree().Root;

        try
        {
            // Kostüm
            if (player.HasMethod("GetCurrentCostumeIndex"))
            {
                int costumeIndex = (int)player.Call("GetCurrentCostumeIndex");
                root.SetMeta("SavedCostume", costumeIndex);
                GD.Print($"[SECRET LEVEL] 💾 Kostüm kaydedildi: {costumeIndex}");
            }

            // Can
            if (player.HasMethod("GetCurrentHealth"))
            {
                int currentHealth = (int)player.Call("GetCurrentHealth");
                int maxHealth = (int)player.Get("MaxHealth");

                root.SetMeta("SavedHealth", currentHealth);
                root.SetMeta("SavedMaxHealth", maxHealth);

                GD.Print($"[SECRET LEVEL] 💾 Can kaydedildi: {currentHealth}/{maxHealth}");
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"[SECRET LEVEL] ❌ SavePlayerState hatası: {e.Message}");
        }
    }
}
