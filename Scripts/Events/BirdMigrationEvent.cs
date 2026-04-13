using Godot;
using System.Collections.Generic;

public partial class BirdMigrationEvent : BaseEvent
{
    [ExportGroup("Kuş Göçü Ayarları")]
    [Export] public float TriggerChance { get; set; } = 0.50f;
    [Export] public float SpawnIntervalMultiplier { get; set; } = 2f;
    [Export] public int MaxBirdsMultiplier { get; set; } = 2;

    // .tscn eski veriyi buraya yükler — sessizce yutmak için [Export] var, kullanılmıyor
    [Export] public Node2D[] NormalSpawners;

    // EventManager'dan set edilir
    public Node2D[] MigrationSpawners;
    public PackedScene BirdScene;
    public Path2D[] MigrationPaths;
    public float BaseSpawnInterval = 3f;
    public int BaseMaxBirds = 5;
    public float BirdPathSpeed = 150f;

    // Spawner mantığının aynısı — PathFollow2D'yi event ilerletiyor
    private class BirdEntry
    {
        public Node2D Bird;
        public PathFollow2D Follow;
        public Path2D CurrentPath;
        public float Speed;
        public bool IsRerouting;
    }
    private List<BirdEntry> activeBirds = new();
    private float spawnTimer = 0f;

    protected override void OnSetup()
    {
        EventName = "Kuş Göçü";
    }

    public override void OnLevelStart()
    {
        if (!EnableEvent) return;
        if (GD.Randf() < TriggerChance)
            StartMigration();
        else
            GD.Print("[KUŞ GÖÇÜ] Bu seferde göç yok.");
    }

    public override void _Process(double delta)
    {
        if (!isActive || BirdScene == null || MigrationPaths == null || MigrationPaths.Length == 0) return;

        int maxBirds = BaseMaxBirds * MaxBirdsMultiplier;
        if (activeBirds.Count < maxBirds)
        {
            spawnTimer -= (float)delta;
            if (spawnTimer <= 0f)
            {
                SpawnBirdOnRandomPath();
                spawnTimer = BaseSpawnInterval / SpawnIntervalMultiplier;
            }
        }

        AdvanceAndCheckBirds((float)delta);
    }

    private void StartMigration()
    {
        isActive = true;
        spawnTimer = 0f;
        activeBirds.Clear();

        // ProcessMode SetDeferred ile değiştir — physics flush crash'ini önler
        if (MigrationSpawners != null)
            CallDeferred(nameof(DisableSpawners));

        GD.Print($"[KUŞ GÖÇÜ] MigrationSpawners null mu: {MigrationSpawners == null}");
        int n = MigrationSpawners?.Length ?? 0;
        int p = MigrationPaths?.Length ?? 0;
        GD.Print($"[KUŞ GÖÇÜ] 🐦 Göç başladı! {n} spawner kapatıldı, {p} path aktif.");
        OnEventStart();
    }

    private void DisableSpawners()
    {
        if (MigrationSpawners == null) return;
        foreach (var s in MigrationSpawners)
        {
            if (IsInstanceValid(s))
            {
                // ProcessMode + QueueFree — GetTree().CreateTimer() kullanan spawner'ları da durdurur
                s.ProcessMode = ProcessModeEnum.Disabled;
                s.QueueFree();
                GD.Print($"[KUŞ GÖÇÜ] ✅ Spawner kaldırıldı: {s.Name}");
            }
        }
    }

    private void SpawnBirdOnRandomPath()
    {
        int idx = GD.RandRange(0, MigrationPaths.Length - 1);
        Path2D path = MigrationPaths[idx];
        if (!IsInstanceValid(path)) return;

        bool fwd = path is BirdRouteManager brm ? brm.Forward : true;
        var follow = new PathFollow2D();
        follow.Rotates = false;
        follow.Loop = false;
        follow.Progress = fwd ? 0f : path.Curve.GetBakedLength();
        path.AddChild(follow);

        var bird = BirdScene.Instantiate<Node2D>();
        if (bird is TrashBird tb) tb.PathSpeed = BirdPathSpeed;
        follow.AddChild(bird);

        float spd = path is BirdRouteManager brmS ? brmS.BirdSpeed : BirdPathSpeed;
        var entry = new BirdEntry { Bird = bird, Follow = follow, CurrentPath = path, Speed = spd };
        activeBirds.Add(entry);

        if (path is BirdRouteManager brmE) brmE.FireEnter();

        // IsRerouting=true iken Reparent'tan gelen sahte TreeExited'ı yoksay
        bird.TreeExited += () => { if (!entry.IsRerouting) activeBirds.Remove(entry); };
    }

    private void AdvanceAndCheckBirds(float delta)
    {
        for (int i = activeBirds.Count - 1; i >= 0; i--)
        {
            var entry = activeBirds[i];
            if (!IsInstanceValid(entry.Bird) || !IsInstanceValid(entry.Follow))
            { activeBirds.RemoveAt(i); continue; }

            bool fwd = entry.CurrentPath is BirdRouteManager brm ? brm.Forward : true;
            entry.Follow.Progress += (fwd ? 1 : -1) * entry.Speed * delta;

            if (IsAtEnd(entry.CurrentPath, entry.Follow))
                RerouteBird(entry);
        }
    }

    private void RerouteBird(BirdEntry entry)
    {
        entry.IsRerouting = true;
        Path2D prevPath = entry.CurrentPath;
        if (prevPath is BirdRouteManager brmPrev) brmPrev.FireExit();

        Path2D nextPath = prevPath is BirdRouteManager brmNext
            ? brmNext.GetNextPath(prevPath) : null;

        if (nextPath == null)
        {
            bool fwdL = prevPath is BirdRouteManager brmL ? brmL.Forward : true;
            entry.Follow.Progress = fwdL ? 0f : prevPath.Curve.GetBakedLength();
            if (prevPath is BirdRouteManager brmRe) brmRe.FireEnter();
            entry.IsRerouting = false;
            return;
        }

        entry.Speed = nextPath is BirdRouteManager brmNS ? brmNS.BirdSpeed : entry.Speed;

        PathFollow2D oldFollow = entry.Follow;
        PathFollow2D newFollow = CreateFollower(nextPath);

        entry.Bird.Reparent(newFollow, false);
        entry.Bird.Position = Vector2.Zero;
        entry.Follow = newFollow;
        entry.CurrentPath = nextPath;

        if (nextPath is BirdRouteManager brmEnter) brmEnter.FireEnter();

        oldFollow.CallDeferred(Node.MethodName.QueueFree);
        entry.IsRerouting = false;
    }

    private PathFollow2D CreateFollower(Path2D path)
    {
        bool fwd = path is BirdRouteManager brm ? brm.Forward : true;
        var follow = new PathFollow2D();
        follow.Rotates = false;
        follow.Loop = false;
        follow.Progress = fwd ? 0f : path.Curve.GetBakedLength();
        path.AddChild(follow);
        return follow;
    }

    private bool IsAtEnd(Path2D path, PathFollow2D follow)
    {
        if (path is BirdRouteManager brm)
            return brm.Forward
                ? follow.Progress >= path.Curve.GetBakedLength() - brm.EndThreshold
                : follow.Progress <= brm.EndThreshold;
        return follow.Progress >= path.Curve.GetBakedLength() - 10f;
    }
}