using Godot;
using System.Collections.Generic;

public partial class TrashBirfSpawner_secured : CharacterBody2D
{
    [Export] public int MaxHealth = 8;
    [Export] public float SpawnInterval = 5.0f;
    [Export] public int MaxBirds = 20;
    [Export] public float DefaultBirdSpeed = 80f;

    [Export] public Path2D AssignedPath;

    [Export] public int MaxSecurityCount = 6;
    [Export] public float SecuritySpawnInterval = 60.0f;
    [Export] public int SecuritySpawnCount = 2;
    [Export] public float PlayerDetectionRange = 300.0f;
    [Export] public PackedScene TrashMinibossSecurityScene;

    private PackedScene birdScene;
    private int currentHealth;
    private bool isDead = false;
    private float spawnTimer = 0;
    private int currentSecurityCount = 0;
    private float securitySpawnTimer = 0;
    private bool hasSpawnedInitialSecurity = false;
    private bool playerInRange = false;

    private AnimatedSprite2D animatedSprite;
    private Area2D playerDetector;
    private Node2D player;

    private class BirdEntry
    {
        public Node2D Bird;
        public PathFollow2D Follow;
        public Path2D CurrentPath;
        public float Speed;
        public bool IsRerouting; // ← EKLENDİ
    }
    private List<BirdEntry> activeBirds = new();

    public override void _Ready()
    {
        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        playerDetector = GetNode<Area2D>("player_detector");

        AddToGroup("enemy");
        currentHealth = MaxHealth;

        var players = GetTree().GetNodesInGroup("player");
        if (players.Count > 0) player = players[0] as Node2D;

        playerDetector.CollisionMask = 2;
        playerDetector.BodyEntered += OnPlayerEnterRange;
        playerDetector.BodyExited += OnPlayerExitRange;
        animatedSprite.Play("idle");

        birdScene = GD.Load<PackedScene>("res://Assets/Scenes/Trash_Bird.tscn");

        if (AssignedPath == null)
            GD.PrintErr("[SECURED SPAWNER] ❌ AssignedPath atanmamış!");

        spawnTimer = 2.0f;
        securitySpawnTimer = 0;
    }

    private void OnPlayerEnterRange(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        playerInRange = true;
        player = body;
        if (!hasSpawnedInitialSecurity)
        {
            SpawnSecurityGuards();
            hasSpawnedInitialSecurity = true;
            securitySpawnTimer = SecuritySpawnInterval;
        }
    }
    private void OnPlayerExitRange(Node2D body)
    {
        if (body.IsInGroup("player")) playerInRange = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (isDead) return;

        spawnTimer -= (float)delta;
        if (spawnTimer <= 0 && activeBirds.Count < MaxBirds)
        { SpawnBird(); spawnTimer = SpawnInterval; }

        if (playerInRange)
        {
            securitySpawnTimer -= (float)delta;
            if (securitySpawnTimer <= 0)
            { SpawnSecurityGuards(); securitySpawnTimer = SecuritySpawnInterval; }
        }

        AdvanceAndCheckBirds((float)delta);
    }

    private void AdvanceAndCheckBirds(float delta)
    {
        for (int i = activeBirds.Count - 1; i >= 0; i--)
        {
            var entry = activeBirds[i];
            if (!IsInstanceValid(entry.Bird) || !IsInstanceValid(entry.Follow))
            { activeBirds.RemoveAt(i); continue; }

            AdvanceFollower(entry, delta);
            if (IsAtEnd(entry.CurrentPath, entry.Follow))
                RerouteBird(entry);
        }
    }

    private void SpawnBird()
    {
        if (AssignedPath == null) return;
        var follow = CreateFollower(AssignedPath);
        var bird = birdScene.Instantiate<Node2D>();
        follow.AddChild(bird);

        float entrySpeed = AssignedPath is BirdRouteManager brmSpd ? brmSpd.BirdSpeed : DefaultBirdSpeed;
        var entry = new BirdEntry { Bird = bird, Follow = follow, CurrentPath = AssignedPath, Speed = entrySpeed };
        activeBirds.Add(entry);

        if (AssignedPath is BirdRouteManager brm) brm.FireEnter();

        // ← DEĞİŞTİ
        bird.TreeExited += () => { if (!entry.IsRerouting) activeBirds.Remove(entry); };
    }

    private void RerouteBird(BirdEntry entry)
    {
        entry.IsRerouting = true; // ← EKLENDİ
        Path2D prevPath = entry.CurrentPath;

        if (prevPath is BirdRouteManager brmPrev) brmPrev.FireExit();

        Path2D nextPath = prevPath is BirdRouteManager brmNext
            ? brmNext.GetNextPath(prevPath)
            : null;

        if (nextPath == null)
        {
            bool fwdL = prevPath is BirdRouteManager brmL ? brmL.Forward : true;
            entry.Follow.Progress = fwdL ? 0f : prevPath.Curve.GetBakedLength();
            if (prevPath is BirdRouteManager brmRe) brmRe.FireEnter();
            entry.IsRerouting = false; // ← EKLENDİ
            return;
        }

        entry.Speed = nextPath is BirdRouteManager brmNS ? brmNS.BirdSpeed : entry.Speed;

        PathFollow2D oldFollow = entry.Follow;
        PathFollow2D newFollow = CreateFollower(nextPath);

        if (IsInstanceValid(entry.Bird))
        {
            entry.Bird.Reparent(newFollow, false);
            entry.Bird.Position = Vector2.Zero;
        }
        entry.Follow = newFollow;
        entry.CurrentPath = nextPath;

        if (nextPath is BirdRouteManager brmEnter) brmEnter.FireEnter();

        oldFollow.CallDeferred(Node.MethodName.QueueFree);
        entry.IsRerouting = false; // ← EKLENDİ
    }

    private PathFollow2D CreateFollower(Path2D path)
    {
        bool forward = path is BirdRouteManager brm ? brm.Forward : true;
        var follow = new PathFollow2D();
        follow.Rotates = false;
        follow.Loop = false;
        follow.Progress = forward ? 0f : path.Curve.GetBakedLength();
        path.AddChild(follow);
        return follow;
    }

    private void AdvanceFollower(BirdEntry entry, float delta)
    {
        bool forward = entry.CurrentPath is BirdRouteManager brm2 ? brm2.Forward : true;
        entry.Follow.Progress += (forward ? 1 : -1) * entry.Speed * delta;
    }

    private bool IsAtEnd(Path2D path, PathFollow2D follow)
    {
        if (path is BirdRouteManager brm)
            return brm.Forward
                ? follow.Progress >= path.Curve.GetBakedLength() - brm.EndThreshold
                : follow.Progress <= brm.EndThreshold;
        return follow.Progress >= path.Curve.GetBakedLength() - 10f;
    }

    private void SpawnSecurityGuards()
    {
        if (TrashMinibossSecurityScene == null) return;
        if (currentSecurityCount >= MaxSecurityCount) return;

        for (int i = 0; i < SecuritySpawnCount; i++)
        {
            var security = TrashMinibossSecurityScene.Instantiate<Node2D>();
            float offsetX = (i == 0) ? -80f : 80f;
            security.GlobalPosition = GlobalPosition + new Vector2(offsetX, 0f);
            GetTree().CurrentScene.AddChild(security);
            security.TreeExited += () => currentSecurityCount--;
        }
        currentSecurityCount += SecuritySpawnCount;
    }

    public void TakeDamage(int damage = 1)
    {
        if (isDead) return;
        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (collision != null) collision.SetDeferred("disabled", true);
        if (playerDetector != null) playerDetector.Monitoring = false;
        if (animatedSprite.SpriteFrames.HasAnimation("death"))
        {
            animatedSprite.Play("death");
            float fc = animatedSprite.SpriteFrames.GetFrameCount("death");
            double fps = animatedSprite.SpriteFrames.GetAnimationSpeed("death");
            GetTree().CreateTimer(fc / fps).Timeout += () =>
            { if (IsInstanceValid(this)) QueueFree(); };
        }
        else QueueFree();
    }
}