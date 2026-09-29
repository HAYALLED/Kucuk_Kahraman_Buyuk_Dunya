using Godot;
using System;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection;

public partial class Level1_2Sclvl : Area2D 
{
    [Export] public string SecretLevelPath = "res://Assets/Scenes/Areas/level1_2_sclvlMain.tscn";
    [Export] public string SecretLevelID = "level1_2";

    private CollisionShape2D _entrance;
    private CollisionShape2D _exit;
    private bool _alreadyEntered = false;

    public override void _Ready()
    {
        GD.Print("========== SECRET ENTRANCE DEBUG ==========");

        _entrance = GetNodeOrNull<CollisionShape2D>("1_2entrance");
        _exit = GetNodeOrNull<CollisionShape2D>("1_2exit");

        if (_entrance == null || _exit == null)
        {
            GD.PrintErr("[SECRET] ❌ Entrance/Exit bulunamadı!");
            return;
        }
        _exit.Disabled = true;
        // ✅ Ölüm sonrası temizlenmiş meta kontrolü
        if (GetTree().Root.HasMeta($"SecretCompleted_{SecretLevelID}"))
        {
            GD.Print($"[SECRET] ⚠️ {SecretLevelID} tamamlandı!");
            Modulate = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            CallDeferred("set_monitoring", false);
            return;
        }

        CollisionLayer = 0;
        CollisionMask = 2;
        BodyEntered += OnBodyEntered;

        GD.Print("[SECRET] ✅ Gizli alan hazır!");
        GD.Print($"[SECRET] Exit: {_exit.GlobalPosition}");
    }

    // ✅ CRITICAL FIX: Physics callback'den HEMEN çık!
    private void OnBodyEntered(Node2D body)
    {
        if (_alreadyEntered || !body.IsInGroup("player")) return;

        _alreadyEntered = true;
        GD.Print("[SECRET] Player girişi algılandı!");

        // ✅ TÜM İŞLEMLERİ DEFER ET!
        CallDeferred(nameof(ProcessSecretEntry), body);
    }

    // ✅ YENİ: Physics callback DIŞINDA çalışır
    private void ProcessSecretEntry(Node2D body)
    {
        if (body == null || !IsInstanceValid(body))
        {
            GD.PrintErr("[SECRET] ❌ Body geçersiz!");
            return;
        }

        GD.Print("[SECRET] 🔄 Veri kaydediliyor...");

        // Çöpleri kaydet
        if (body.HasMethod("GetAllPoints"))
        {
            int[] trashCounts = (int[])body.Call("GetAllPoints");
            GetTree().Root.SetMeta("SavedTrash_Plastic", trashCounts[0]);
            GetTree().Root.SetMeta("SavedTrash_Metal", trashCounts[1]);
            GetTree().Root.SetMeta("SavedTrash_Glass", trashCounts[2]);
            GetTree().Root.SetMeta("SavedTrash_Food", trashCounts[3]);
            GetTree().Root.SetMeta("SavedTrash_Wood", trashCounts[4]);

            int total = trashCounts[0] + trashCounts[1] + trashCounts[2] + trashCounts[3] + trashCounts[4];
            GD.Print($"[SECRET] 💾 Çöpler kaydedildi: {total} adet");
        }

        // Kostüm ve can kaydet
        SavePlayerState(body);

        // Dönüş pozisyonu
        Vector2 exitPos = _exit.GlobalPosition;
        GetTree().Root.SetMeta("ReturnFromSecret", exitPos);
        GetTree().Root.SetMeta("CurrentSecretID", SecretLevelID);

        GD.Print($"[SECRET] ✅ Secret level'e geçiliyor!");

        // ✅ Scene değişimini de defer et
        GetTree().CallDeferred("change_scene_to_file", SecretLevelPath);
    }

    private void SavePlayerState(Node2D body)
    {
        var root = GetTree().Root;

        try
        {
            if (body.HasMethod("GetCostumeHealthSaveData"))
            {
                var data = (Godot.Collections.Dictionary)body.Call("GetCostumeHealthSaveData");
                root.SetMeta("SavedCostumeHealthData", data);
                GD.Print("[SECRET] 💾 Tüm kostümlerin canı kaydedildi!");
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"[SECRET] ❌ SavePlayerState hatası: {e.Message}");
        }
    }
}