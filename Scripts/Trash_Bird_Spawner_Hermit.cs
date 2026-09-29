using Godot;
using System.Collections.Generic;

public partial class Trash_Bird_Spawner_Hermit : CharacterBody2D
{
    [Export] public float Speed = 50.0f;
    [Export] public float Gravity = 980.0f;
    [Export] public int MaxHealth = 5;
    [Export] public float SpawnInterval = 2.0f;
    [Export] public int MaxBirds = 30;
    [Export] public float CoverDistance = 100.0f;
    [Export] public float UncoverDistance = 200.0f;
    [Export] public float DefaultBirdSpeed = 80f;

    [Export] public Path2D AssignedPath;

    private PackedScene birdScene;
    private int currentHealth;
    private bool isDead = false;
    private float spawnTimer = 0;
    private bool isCovering = false;
    private bool isCovered = false;
    private bool isStunned = false;
    private float stunTimer = 0;
    private float originalSpeed;
    private bool playerInRange = false;
    private int direction = 1;

    private AnimatedSprite2D animatedSprite;
    private Area2D playerDetector;
    private RayCast2D raycastLeft;
    private RayCast2D raycastRight;
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
        originalSpeed = Speed;
        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        playerDetector = GetNode<Area2D>("player_detector");
        raycastLeft = GetNode<RayCast2D>("RayCast2Dleft");
        raycastRight = GetNode<RayCast2D>("RayCast2Dright");

        AddToGroup("enemy");
        currentHealth = MaxHealth;

        var players = GetTree().GetNodesInGroup("player");
        if (players.Count > 0) player = players[0] as Node2D;

        playerDetector.CollisionMask = 2;
        playerDetector.BodyEntered += OnPlayerEnterRange;
        playerDetector.BodyExited += OnPlayerExitRange;

        if (raycastLeft != null) { raycastLeft.Enabled = true; raycastLeft.CollisionMask = 1; }
        if (raycastRight != null) { raycastRight.Enabled = true; raycastRight.CollisionMask = 1; }

        animatedSprite.Play("walk");
        birdScene = GD.Load<PackedScene>("res://Assets/Scenes/Trash_Bird.tscn");

        if (AssignedPath == null)
            GD.PrintErr("[HERMIT SPAWNER] ❌ AssignedPath atanmamış!");

        spawnTimer = 2.0f;
    }

    private void OnPlayerEnterRange(Node2D body)
    {
        if (body.IsInGroup("player")) { playerInRange = true; player = body; }
    }
    private void OnPlayerExitRange(Node2D body)
    {
        if (body.IsInGroup("player")) playerInRange = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (isStunned)
        {
            stunTimer -= (float)delta;
            if (stunTimer <= 0) { isStunned = false; Speed = originalSpeed; }
            return;
        }
        if (isDead) return;

        float distanceToPlayer = player != null
            ? GlobalPosition.DistanceTo(player.GlobalPosition) : float.MaxValue;

        if (!isCovered && !isCovering && distanceToPlayer <= CoverDistance)
        { StartCovering(); return; }
        else if (isCovered && distanceToPlayer > UncoverDistance)
        { StartUncovering(); return; }

        if (isCovering || isCovered) return;

        spawnTimer -= (float)delta;
        if (spawnTimer <= 0 && activeBirds.Count < MaxBirds)
        { SpawnBird(); spawnTimer = SpawnInterval; }

        AdvanceAndCheckBirds((float)delta);
        Move(delta);
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

    private async void StartCovering()
    {
        isCovering = true;
        Velocity = Vector2.Zero;
        animatedSprite.Play("covering");
        float fc = animatedSprite.SpriteFrames.GetFrameCount("covering");
        double fps = animatedSprite.SpriteFrames.GetAnimationSpeed("covering");
        await ToSignal(GetTree().CreateTimer(fc / fps), SceneTreeTimer.SignalName.Timeout);
        isCovering = false; isCovered = true;
        animatedSprite.Play("cover_idle");
    }

    private async void StartUncovering()
    {
        isCovered = false; isCovering = true;
        animatedSprite.Play("covering");
        animatedSprite.SpeedScale = -1;
        animatedSprite.Frame = animatedSprite.SpriteFrames.GetFrameCount("covering") - 1;
        float fc = animatedSprite.SpriteFrames.GetFrameCount("covering");
        double fps = animatedSprite.SpriteFrames.GetAnimationSpeed("covering");
        await ToSignal(GetTree().CreateTimer(fc / fps), SceneTreeTimer.SignalName.Timeout);
        animatedSprite.SpeedScale = 1;
        isCovering = false;
        animatedSprite.Play("walk");
    }

    public void ApplyStun(float duration) { isStunned = true; stunTimer = duration; }

    public void ApplySlow(float slowPercent, float duration)
    {
        Speed = originalSpeed * (1.0f - slowPercent);
        GetTree().CreateTimer(duration).Timeout += () =>
        { if (!isStunned) Speed = originalSpeed; };
    }

    private void Move(double delta)
    {
        if (isCovering || isCovered) return;
        Vector2 velocity = Velocity;
        velocity.Y = IsOnFloor() ? 0 : velocity.Y + Gravity * (float)delta;
        velocity.X = direction * Speed;
        animatedSprite.FlipH = direction > 0;
        Velocity = velocity;
        MoveAndSlide();
        CheckDirection();
    }

    private void CheckDirection()
    {
        if (raycastLeft == null || raycastRight == null) return;
        if (IsOnWall()) { direction *= -1; return; }
        if (IsOnFloor())
        {
            if (direction > 0 && !raycastRight.IsColliding()) direction = -1;
            else if (direction < 0 && !raycastLeft.IsColliding()) direction = 1;
        }
    }

    public void TakeDamage(int damage = 1)
    {
        if (isDead || isCovered || isCovering) return;
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