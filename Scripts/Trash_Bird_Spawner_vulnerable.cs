using Godot;
using System.Collections.Generic;

public partial class Trash_Bird_Spawner_vulnerable : CharacterBody2D
{
    [Export] public int MaxHealth = 8;
    [Export] public float SpawnInterval = 5.0f;
    [Export] public int MaxBirds = 20;
    [Export] public float DefaultBirdSpeed = 80f;

    [Export] public Path2D AssignedPath;

    private PackedScene birdScene;
    private int currentHealth;
    private bool isDead = false;
    private float spawnTimer = 0;
    private AnimatedSprite2D animatedSprite;

    private class BirdEntry
    {
        public Node2D Bird;
        public PathFollow2D Follow;
        public Path2D CurrentPath;
        public float Speed; // spawn veya reroute'ta set edilir
        public bool IsRerouting; // ← EKLENDİ: Reparent sırasında TreeExited'ı engeller
    }
    private List<BirdEntry> activeBirds = new();

    public override void _Ready()
    {
        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        AddToGroup("enemy");
        currentHealth = MaxHealth;
        animatedSprite.Play("idle");
        birdScene = GD.Load<PackedScene>("res://Assets/Scenes/Trash_Bird.tscn");
        if (AssignedPath == null)
            GD.PrintErr("[VULNERABLE SPAWNER] ❌ AssignedPath atanmamış!");
        spawnTimer = 2.0f;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (isDead) return;
        spawnTimer -= (float)delta;
        if (spawnTimer <= 0 && activeBirds.Count < MaxBirds)
        { SpawnBird(); spawnTimer = SpawnInterval; }
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

        // ← DEĞİŞTİ: Reparent'tan gelen sahte TreeExited entry'yi silmesin
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
            // ConnectedPaths yok → başa sar (loop)
            bool fwdL = prevPath is BirdRouteManager brmL ? brmL.Forward : true;
            entry.Follow.Progress = fwdL ? 0f : prevPath.Curve.GetBakedLength();
            if (prevPath is BirdRouteManager brmRe) brmRe.FireEnter();
            entry.IsRerouting = false; // ← EKLENDİ
            return;
        }

        // Hızı bir sonraki path'e taşı (BirdRouteManager ise yeni hız, değilse eskiyi koru)
        entry.Speed = nextPath is BirdRouteManager brmNS ? brmNS.BirdSpeed : entry.Speed;

        PathFollow2D oldFollow = entry.Follow;
        PathFollow2D newFollow = CreateFollower(nextPath);

        // Önce yeni follow'a reparent et, sonra eski follow'u sil
        if (IsInstanceValid(entry.Bird))
        {
            entry.Bird.Reparent(newFollow, false);
            entry.Bird.Position = Vector2.Zero;
        }
        entry.Follow = newFollow;
        entry.CurrentPath = nextPath;

        if (nextPath is BirdRouteManager brmEnter) brmEnter.FireEnter();

        // Deferred free — aynı frame'de silme sorunu önler
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