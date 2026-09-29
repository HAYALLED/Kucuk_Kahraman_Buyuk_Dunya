using Godot;

public partial class TrashBird : CharacterBody2D
{
    [ExportGroup("Temel")]
    [Export] public float Speed = 300.0f;
    [Export] public int MaxHealth = 1;
    [Export] public float DiveSpeed = 400.0f;
    [Export] public float PathSpeed = 100.0f;
    [Export] public Vector2 BirdScale = Vector2.One;

    [ExportGroup("Stuck Sistemi")]
    [Export] public float StuckThreshold = 4f;
    [Export] public float UnstuckDuration = 1.5f;

    [ExportGroup("Dive Sistemi")]
    [Export] public float DiveReturnDelay = 4f;

    private int currentHealth;
    private int direction = 1;
    private bool isDead = false;
    private bool isDiving = false;
    private bool isStunned = false;
    private float stunTimer = 0f;
    private float originalSpeed;
    private Path2D assignedPath;
    private Vector2 lastPosition;
    private float stuckTimer = 0f;
    private bool isUnstucking = false;
    private bool wantsDive = false;
    // dive/stuck sonrası spawner dışına çıkınca true
    private bool selfManaged = false;
    private float inheritedSpeed = 0f; // son BirdRouteManager hızı

    private AnimatedSprite2D animatedSprite;
    private Area2D attackCollision;
    private CollisionShape2D attackShape;
    private CollisionShape2D bodyShape;
    private Area2D playerDetector;
    private Node2D player;
    private RayCast2D raycastLeft;
    private RayCast2D raycastRight;
    private bool playerInRange = false;

    public override void _Ready()
    {
        originalSpeed = Speed;

        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        attackCollision = GetNode<Area2D>("attack_collision");
        playerDetector = GetNode<Area2D>("player_detector");
        raycastLeft = GetNodeOrNull<RayCast2D>("RayCast2Dleft");
        raycastRight = GetNodeOrNull<RayCast2D>("RayCast2Dright");
        attackShape = attackCollision.GetNode<CollisionShape2D>("CollisionShape2D");
        bodyShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        AddToGroup("enemy");
        currentHealth = MaxHealth;
        attackCollision.Monitoring = false;

        var players = GetTree().GetNodesInGroup("player");
        if (players.Count > 0) player = players[0] as Node2D;

        playerDetector.CollisionMask = 2;
        attackCollision.BodyEntered += OnAttackHit;
        playerDetector.BodyEntered += OnPlayerEnterRange;
        playerDetector.BodyExited += OnPlayerExitRange;
        playerDetector.Monitoring = true;

        if (GetParent() is PathFollow2D pf && pf.GetParent() is Path2D p2d)
            assignedPath = p2d;

        GlobalScale = BirdScale;
        lastPosition = GlobalPosition;
        animatedSprite.Play("fly");

        if (raycastLeft != null) { raycastLeft.Enabled = true; raycastLeft.CollisionMask = 1; }
        if (raycastRight != null) { raycastRight.Enabled = true; raycastRight.CollisionMask = 1; }
    }

    private void OnPlayerEnterRange(Node2D body)
    {
        if (body.IsInGroup("player") && !isDiving && !wantsDive && !isDead)
        {
            playerInRange = true;
            player = body;
            wantsDive = true;
        }
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

        if (GlobalScale != BirdScale) GlobalScale = BirdScale;

        if (wantsDive)
        {
            wantsDive = false;
            StartDive();
            return;
        }

        if (!isDiving && !isUnstucking)
        {
            if (GlobalPosition.DistanceTo(lastPosition) < 2f)
            {
                stuckTimer += (float)delta;
                if (stuckTimer >= StuckThreshold) ReturnToPath();
            }
            else
            {
                stuckTimer = 0f;
                lastPosition = GlobalPosition;
            }
        }

        if (isDiving)
            DiveMove(delta);
        else if (GetParent() is PathFollow2D)
            PathMove(delta);
        else
            Move(delta);
    }

    private void PathMove(double delta)
    {
        if (!(GetParent() is PathFollow2D follow)) return;
        if (!(follow.GetParent() is Path2D path)) return;

        assignedPath = path;
        float baked = path.Curve.GetBakedLength();

        // selfManaged = true  → BirdRouteManager'da veya dive/stuck sonrası: bird kendi ilerler
        // selfManaged = false → plain Path2D normal spawn: spawner ilerletiyor, biz sadece sprite
        if (selfManaged)
        {
            if (path is BirdRouteManager brmCur) inheritedSpeed = brmCur.BirdSpeed;
            float speed = inheritedSpeed > 0f ? inheritedSpeed : PathSpeed;
            bool fwd = path is BirdRouteManager brm2 ? brm2.Forward : true;
            float thresh = path is BirdRouteManager brm3 ? brm3.EndThreshold : 10f;

            follow.Progress += (fwd ? 1f : -1f) * speed * (float)delta;

            bool atEnd = fwd
                ? follow.Progress >= baked - thresh
                : follow.Progress <= thresh;

            if (atEnd)
            {
                if (path is BirdRouteManager brmEnd)
                {
                    Path2D next = brmEnd.GetNextPath(path);
                    if (next != null)
                    {
                        bool nextFwd = next is BirdRouteManager brmN ? brmN.Forward : true;
                        var newFollow = new PathFollow2D();
                        newFollow.Rotates = false;
                        newFollow.Loop = false;
                        newFollow.Progress = nextFwd ? 0f : next.Curve.GetBakedLength();
                        next.AddChild(newFollow);
                        this.Reparent(newFollow);
                        Position = Vector2.Zero;
                        assignedPath = next;
                        follow.QueueFree();
                        return;
                    }
                }
                // ConnectedPaths yok veya plain Path2D → başa sar
                bool f = path is BirdRouteManager brm4 ? brm4.Forward : true;
                follow.Progress = f ? 0f : baked;
            }
        }

        animatedSprite.FlipH = follow.Progress > baked / 2f;
    }

    private void Move(double delta)
    {
        Vector2 v = Velocity;
        v.X = direction * Speed;
        v.Y = 0;
        animatedSprite.FlipH = direction > 0;
        if (attackShape != null) attackShape.Position = new Vector2(direction * 20, 0);
        Velocity = v;
        MoveAndSlide();
        if (IsOnWall()) direction *= -1;
    }

    private async void ReturnToPath()
    {
        if (isUnstucking || isDead) return;
        isUnstucking = true;
        stuckTimer = 0f;
        isDiving = false;

        attackCollision.SetDeferred("monitoring", false);
        if (bodyShape != null) bodyShape.SetDeferred("disabled", true);
        animatedSprite.Rotation = 0;
        animatedSprite.Play("fly");

        await ToSignal(GetTree().CreateTimer(UnstuckDuration), SceneTreeTimer.SignalName.Timeout);

        if (isDead || !IsInstanceValid(this)) return;

        if (bodyShape != null) bodyShape.SetDeferred("disabled", false);

        if (assignedPath != null && IsInstanceValid(assignedPath)
            && !(GetParent() is PathFollow2D))
        {
            bool fwd = assignedPath is BirdRouteManager brm ? brm.Forward : true;
            var follow = new PathFollow2D();
            follow.Rotates = false;
            follow.Loop = false;
            follow.Progress = fwd ? 0f : assignedPath.Curve.GetBakedLength();
            assignedPath.AddChild(follow);
            this.Reparent(follow);
            Position = Vector2.Zero;
            selfManaged = true;
        }

        lastPosition = GlobalPosition;
        isUnstucking = false;
    }

    private void StartDive()
    {
        isDiving = true;
        selfManaged = false;

        if (GetParent() is PathFollow2D pf)
        {
            if (pf.GetParent() is Path2D p2d) assignedPath = p2d;
            var pos = GlobalPosition;
            this.Reparent(GetTree().CurrentScene);
            GlobalPosition = pos;
        }

        direction = player.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
        animatedSprite.FlipH = direction > 0;
        animatedSprite.Play("dive");
        attackCollision.SetDeferred("monitoring", true);
        if (attackShape != null) attackShape.Position = new Vector2(direction * 20, 0);
        animatedSprite.Rotation = direction * Mathf.DegToRad(15);

        GetTree().CreateTimer(DiveReturnDelay).Timeout += () =>
        {
            if (!isDead && isDiving) ReturnToPath();
        };
    }

    private void DiveMove(double delta)
    {
        Vector2 v = Velocity;
        v.X = direction * DiveSpeed;
        v.Y = Mathf.Abs(v.X) * 0.27f;
        Velocity = v;
        MoveAndSlide();
        if (IsOnFloor()) Die();
    }

    public void ApplyStun(float duration)
    {
        isStunned = true;
        stunTimer = duration;
    }

    public void ApplySlow(float slowPercent, float duration)
    {
        Speed = originalSpeed * (1.0f - slowPercent);
        GetTree().CreateTimer(duration).Timeout += () =>
        { if (!isStunned) Speed = originalSpeed; };
    }

    private void OnAttackHit(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            if (body.HasMethod("TakeDamage")) body.Call("TakeDamage", 1);
            Die();
        }
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
        SetPhysicsProcess(false);

        if (bodyShape != null) bodyShape.SetDeferred("disabled", true);
        if (attackCollision != null) attackCollision.SetDeferred("monitoring", false);
        if (playerDetector != null) playerDetector.SetDeferred("monitoring", false);

        if (animatedSprite.SpriteFrames.HasAnimation("death"))
        {
            animatedSprite.Rotation = 0;
            animatedSprite.Play("death");
            float fc = animatedSprite.SpriteFrames.GetFrameCount("death");
            double fps = animatedSprite.SpriteFrames.GetAnimationSpeed("death");
            GetTree().CreateTimer(fc / fps).Timeout += () =>
            { if (IsInstanceValid(this)) QueueFree(); };
        }
        else QueueFree();
    }
}