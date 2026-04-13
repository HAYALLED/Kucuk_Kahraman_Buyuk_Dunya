using Godot;
using System;
using System.Collections.Generic;

public partial class NPCSpawner : Node
{
    public enum NPCType
    {
        TEACHER,
        TAILOR,
        AQUAMAN_EVENT,
        ROBOT  // ✅ YENİ!
    }

    [ExportGroup("NPC Scenes")]
    [Export] public PackedScene TeacherScene;
    [Export] public PackedScene TailorScene;
    [Export] public PackedScene AquamanEventScene;
    [Export] public PackedScene RobotScene;  // ✅ YENİ!

    [ExportGroup("Spawn Oranları (%)")]
    [Export] public int TeacherChance = 35;     // ⚡ 45 → 35 (azaldı)
    [Export] public int TailorChance = 35;      // ⚡ 45 → 35 (azaldı)
    [Export] public int AquamanChance = 10;     // Aynı
    [Export] public int RobotChance = 20;       // ✅ YENİ!

    [ExportGroup("Spawn Pozisyonları")]
    [Export] public Godot.Collections.Array<Marker2D> SpawnPositions;

    private Dictionary<NPCType, Node2D> activeNPCs = new Dictionary<NPCType, Node2D>();

    public override void _Ready()
    {
        GD.Print("========== NPC SPAWNER BAŞLADI ==========");

        // ✅ FIX: Spawn pozisyonlarını bul
        if (SpawnPositions.Count == 0)
        {
            FindSpawnPositions();
        }

        // ✅ FIX: CallDeferred ile spawn et
        CallDeferred(nameof(SpawnRandomNPCs));
    }

    private void FindSpawnPositions()
    {
        var markers = GetTree().GetNodesInGroup("npc_spawn");

        GD.Print($"[NPC SPAWNER] 'npc_spawn' grubunda {markers.Count} node bulundu");

        foreach (var marker in markers)
        {
            if (marker is Marker2D marker2D)
            {
                SpawnPositions.Add(marker2D);
                GD.Print($"[NPC SPAWNER] ✅ Marker eklendi: {marker2D.Name} - Pos: {marker2D.GlobalPosition}");
            }
        }

        if (SpawnPositions.Count == 0)
        {
            GD.PrintErr("[NPC SPAWNER] ❌ Hiçbir Marker2D bulunamadı! 'npc_spawn' grubunu kontrol et!");
        }
    }

    private void SpawnRandomNPCs()
    {
        if (SpawnPositions.Count == 0)
        {
            GD.PrintErr("[NPC SPAWNER] ❌ Spawn pozisyonu bulunamadı!");
            return;
        }

        GD.Print($"[NPC SPAWNER] {SpawnPositions.Count} spawn noktası için NPC'ler oluşturuluyor...");

        // Her spawn noktası için bir NPC seç
        foreach (var spawnPos in SpawnPositions)
        {
            var npcType = SelectRandomNPCType();

            // Bu tipte zaten varsa atla
            if (activeNPCs.ContainsKey(npcType))
            {
                GD.Print($"[NPC SPAWNER] ⚠️ {npcType} zaten var, atlanıyor");
                continue;
            }

            // NPC'yi spawn et
            SpawnNPC(npcType, spawnPos.GlobalPosition);
        }

        GD.Print("========== NPC SPAWNER BİTTİ ==========");
    }

    private NPCType SelectRandomNPCType()
    {
        // ✅ FIX 1: Robot dahil toplam hesabı
        int total = TeacherChance + TailorChance + AquamanChance + RobotChance;

        // ✅ FIX 2: RandiRange kullan (int için!)
        int randomValue = GD.RandRange(0, total - 1);

        int cumulative = 0;

        cumulative += TeacherChance;
        if (randomValue < cumulative)
        {
            GD.Print($"[NPC SPAWNER] 🎲 TEACHER seçildi (roll: {randomValue}/{total})");
            return NPCType.TEACHER;
        }

        cumulative += TailorChance;
        if (randomValue < cumulative)
        {
            GD.Print($"[NPC SPAWNER] 🎲 TAILOR seçildi (roll: {randomValue}/{total})");
            return NPCType.TAILOR;
        }

        cumulative += AquamanChance;
        if (randomValue < cumulative)
        {
            GD.Print($"[NPC SPAWNER] 🎲 AQUAMAN_EVENT seçildi (roll: {randomValue}/{total})");
            return NPCType.AQUAMAN_EVENT;
        }

        // ✅ FIX 3: ROBOT dön! (AQUAMAN_EVENT değil!)
        GD.Print($"[NPC SPAWNER] 🎲 ROBOT seçildi (roll: {randomValue}/{total})");
        return NPCType.ROBOT;
    }

    private void SpawnNPC(NPCType npcType, Vector2 position)
    {
        // ✅ FIX 4: Robot case eklendi
        PackedScene scene = npcType switch
        {
            NPCType.TEACHER => TeacherScene,
            NPCType.TAILOR => TailorScene,
            NPCType.AQUAMAN_EVENT => AquamanEventScene,
            NPCType.ROBOT => RobotScene,  // ✅ ROBOT CASE!
            _ => null
        };

        if (scene == null)
        {
            GD.PrintErr($"[NPC SPAWNER] ❌ {npcType} scene NULL! Inspector'da scene'i ekle!");
            return;
        }

        // ✅ FIX: Instance'ı oluştur
        var instance = scene.Instantiate<Node2D>();
        instance.GlobalPosition = position;

        // ✅ FIX: GetParent() yerine GetTree().CurrentScene kullan
        GetTree().CurrentScene.CallDeferred("add_child", instance);

        activeNPCs[npcType] = instance;

        GD.Print($"[NPC SPAWNER] ✅ {npcType} spawn edildi: {position}");
    }

    // Dinamik oran değiştirme
    public void SetSpawnChance(NPCType npcType, int chance)
    {
        switch (npcType)
        {
            case NPCType.TEACHER:
                TeacherChance = chance;
                break;
            case NPCType.TAILOR:
                TailorChance = chance;
                break;
            case NPCType.AQUAMAN_EVENT:
                AquamanChance = chance;
                break;
            case NPCType.ROBOT:  // ✅ FIX 5: Robot case!
                RobotChance = chance;
                break;
        }

        GD.Print($"[NPC SPAWNER] {npcType} spawn şansı: %{chance}");
    }

    // NPC kaldırıldığında
    public void RemoveNPC(NPCType npcType)
    {
        if (activeNPCs.ContainsKey(npcType))
        {
            activeNPCs.Remove(npcType);
            GD.Print($"[NPC SPAWNER] {npcType} kaldırıldı");
        }
    }
}