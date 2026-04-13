using Godot;
using System.Collections.Generic;

public partial class TrashRainEvent : BaseEvent
{
    [ExportGroup("Çöp Yağmuru Ayarları")]
    [Export] public float CheckInterval { get; set; } = 60f;
    [Export] public float TriggerChance { get; set; } = 0.20f;  // 20%
    [Export] public float SpawnMultiplier { get; set; } = 1.5f;

    // ===== 5 ÇÖPE SCENE REFERANSI =====
    [ExportGroup("Çöp Sahneleri")]
    [Export] public PackedScene PlasticScene { get; set; }   // PointsPlastic
    [Export] public PackedScene MetalScene { get; set; }   // PointsMetal
    [Export] public PackedScene GlassScene { get; set; }   // PointsGlass
    [Export] public PackedScene FoodScene { get; set; }   // PointsFood
    [Export] public PackedScene WoodScene { get; set; }   // PointsWood

    [ExportGroup("Spawn Ayarları")]
    [Export] public int BaseTrashCount { get; set; } = 10;
    [Export] public float SpawnSpreadX { get; set; } = 400f;  // Player etrafında X yayılımı

    [ExportGroup("Düşme Ayarları")]
    [Export] public float FallSpeed { get; set; } = 80f;      // Piksel/saniye
    [Export] public float FadeInTime { get; set; } = 2f;      // Opaklık artış süresi
    [Export] public float StartOpacity { get; set; } = 0.2f;  // Başlangıç opaklık (%20)

    // ===========================
    private float checkTimer = 0f;

    // Düşen çöp takibi
    private class FallingItem
    {
        public Node2D Node;
        public bool Landed;
    }
    private readonly List<FallingItem> fallingItems = new();

    protected override void OnSetup()
    {
        EventName = "Çöp Yağmuru";
        checkTimer = CheckInterval;
    }

    public override void _Process(double delta)
    {
        if (!EnableEvent) return;
        float dt = (float)delta;

        UpdateFallingItems(dt);

        checkTimer -= dt;
        if (checkTimer <= 0)
        {
            checkTimer = CheckInterval;
            TryTrigger();
        }
    }

    private void UpdateFallingItems(float dt)
    {
        var spaceState = GetTree().Root.GetWorld2D().DirectSpaceState;

        for (int i = fallingItems.Count - 1; i >= 0; i--)
        {
            var item = fallingItems[i];

            // Geçersiz node temizle
            if (item.Node == null || !IsInstanceValid(item.Node))
            {
                fallingItems.RemoveAt(i);
                continue;
            }

            if (item.Landed) continue;

            // Aşağı doğru raycast — platform var mı?
            float checkDist = FallSpeed * dt + 6f;
            var query = PhysicsRayQueryParameters2D.Create(
                item.Node.GlobalPosition,
                item.Node.GlobalPosition + Vector2.Down * checkDist
            );
            query.CollisionMask = 1;          // Layer 1 = platform/zemin
            query.CollideWithBodies = true;
            query.CollideWithAreas = false;  // Area2D (diğer çöpler) sayılmasın

            var result = spaceState.IntersectRay(query);

            if (result.Count > 0)
            {
                // Platforma değdi → sabitle
                Vector2 hitPos = (Vector2)result["position"];
                item.Node.GlobalPosition = hitPos;
                item.Landed = true;
                GD.Print($"[ÇÖP YAĞMURU] ✅ {item.Node.Name} platforma indi.");
            }
            else
            {
                // Düşmeye devam
                item.Node.GlobalPosition += Vector2.Down * FallSpeed * dt;
            }
        }
    }

    private void TryTrigger()
    {
        if (GD.Randf() < TriggerChance)
            SpawnExtraTrash();
    }

    private void SpawnExtraTrash()
    {
        // Hangi scene'ler dolu?
        var validScenes = new List<PackedScene>();
        if (PlasticScene != null) validScenes.Add(PlasticScene);
        if (MetalScene != null) validScenes.Add(MetalScene);
        if (GlassScene != null) validScenes.Add(GlassScene);
        if (FoodScene != null) validScenes.Add(FoodScene);
        if (WoodScene != null) validScenes.Add(WoodScene);

        if (validScenes.Count == 0)
        {
            // Eski fallback: trash_spawner group
            var spawners = GetTree().GetNodesInGroup("trash_spawner");
            foreach (var spawner in spawners)
                if (spawner.HasMethod("SetMultiplier"))
                    spawner.Call("SetMultiplier", SpawnMultiplier);
            GD.Print($"[ÇÖP YAĞMURU] Spawner'lara x{SpawnMultiplier} çarpanı verildi!");
            OnEventStart();
            return;
        }

        var player = manager?.GetPlayer();
        if (player == null)
        {
            GD.PrintErr("[ÇÖP YAĞMURU] ❌ Player bulunamadı!");
            return;
        }

        // Ekran üstü Y koordinatı
        float screenHalfH = GetViewport().GetVisibleRect().Size.Y / 2f;
        float spawnY = player.GlobalPosition.Y - screenHalfH - 20f; // Ekranın hemen dışı, görünmez

        int count = (int)(BaseTrashCount * SpawnMultiplier);
        var level = GetTree().CurrentScene;

        for (int i = 0; i < count; i++)
        {
            // Rastgele çöp türü seç
            int pick = GD.RandRange(0, validScenes.Count - 1);
            var trash = validScenes[pick].Instantiate<Node2D>();

            // X: player etrafında yayılmış rastgele konum
            float spawnX = player.GlobalPosition.X + (float)GD.RandRange(-SpawnSpreadX, SpawnSpreadX);
            trash.GlobalPosition = new Vector2(spawnX, spawnY);

            // Başlangıç opaklığı %20
            trash.Modulate = new Color(1f, 1f, 1f, StartOpacity);

            level.AddChild(trash);

            // 2 saniyede %100 opaklığa gel (hızlı giriş efekti)
            var tween = trash.CreateTween();
            tween.TweenProperty(trash, "modulate:a", 1.0f, FadeInTime);

            // Düşme listesine ekle
            fallingItems.Add(new FallingItem { Node = trash, Landed = false });
        }

        GD.Print($"[ÇÖP YAĞMURU] 🗑️ {count} çöp yağmura başladı! (x{SpawnMultiplier})");
        OnEventStart();
    }
}