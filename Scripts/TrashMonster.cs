using Godot;
using System;

public partial class TrashMonster : CharacterBody2D
{
    [Export] public float Speed = 50.0f;
    [Export] public float Gravity = 980.0f;
    [Export] public int MaxHealth = 10;
    [Export] public float AttackRange = 100.0f;
    [Export] public float AttackCooldown = 2.0f;
    [Export] public PackedScene ProjectileScene;
    [Export] public float ProjectileCooldown = 3.0f;
    [Export] public int ProjectileDamage = 1;
    [Export] public float IdleWaitTime = 5.0f;
    [Export] public float IdleDuration = 2.0f;
    [Export] public float JumpForce = -350.0f;
    [Export] public float PlatformJumpCooldown = 1.0f;
    [Export] public float ChaseJumpForceMultiplier = 1.3f;
    [Export] public float MinJumpForce = -250.0f;
    [Export] public float MaxJumpForce = -600.0f;

    // ✅ YENİ: Zıplama İleri Boost
    [ExportGroup("Zıplama Boost")]
    [Export] public float JumpForwardBoost = 150.0f;   // Zıplarken ek ileri hız (piksel/sn)

    // ✅ YENİ: Kovalama Hızlanma
    [ExportGroup("Kovalama Hızlanma")]
    [Export] public float ChaseMaxSpeedMultiplier = 2.0f;   // Max hız çarpanı (2.0 = 2 kat)
    [Export] public float ChaseAccelerationTime = 3.0f;     // Max hıza ulaşma süresi (saniye)

    private int currentHealth;
    private int direction = 1;
    private bool isDead = false;
    private bool isAttacking = false;
    private float attackTimer = 0;
    private bool isHurt = false;
    private bool isStunned = false;
    private float stunTimer = 0;
    private float originalSpeed;
    private int bonusDamage = 0; // TrashKingEvent

    private bool playerInProjectileRange = false;
    private float projectileCooldownTimer = 0;
    private bool isProjectileAttacking = false;
    private int projectilesFired = 0;

    private float idleTimer = 0;
    private float idleStayTimer = 0;
    private bool isIdle = false;
    private bool isGoingIdle = false;
    private bool isWakingUp = false;

    private float jumpCooldownTimer = 0;
    private bool isJumping = false;
    private bool isPrepJump = false;
    private bool isChasing = false;
    private bool justJumped = false;

    // ✅ Kovalama hızlanma durumu
    private float chaseSpeedMultiplier = 1.0f;
    private float chaseTimer = 0;

    private Vector2 jumpTargetPoint = Vector2.Zero;

    private AnimatedSprite2D animatedSprite;
    private Area2D attackCollision;
    private CollisionShape2D attackShape;
    private Area2D playerDetector;
    private Area2D playerDetectorProjectile;
    private RayCast2D raycastLeft;
    private RayCast2D raycastRight;
    private RayCast2D platformRayCast;
    private Node2D player;
    private bool playerInRange = false;

    public override void _Ready()
    {
        originalSpeed = Speed;

        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        attackCollision = GetNode<Area2D>("attack_collision");
        playerDetector = GetNode<Area2D>("player_detector");
        playerDetectorProjectile = GetNode<Area2D>("player_detector_projectile");
        raycastLeft = GetNode<RayCast2D>("RayCast2Dleft");
        raycastRight = GetNode<RayCast2D>("RayCast2Dright");

        platformRayCast = GetNodeOrNull<RayCast2D>("PlatformRayCast2");
        if (platformRayCast != null) { platformRayCast.Enabled = true; platformRayCast.CollisionMask = 1; }

        AddToGroup("enemy");
        attackShape = attackCollision.GetNode<CollisionShape2D>("CollisionShape2D");
        currentHealth = MaxHealth;
        attackCollision.Monitoring = false;

        var players = GetTree().GetNodesInGroup("player");
        if (players.Count > 0) player = players[0] as Node2D;

        playerDetector.CollisionMask = 2;
        playerDetectorProjectile.CollisionMask = 2;

        attackCollision.BodyEntered += OnAttackHit;
        playerDetector.BodyEntered += OnPlayerEnterRange;
        playerDetector.BodyExited += OnPlayerExitRange;
        playerDetectorProjectile.BodyEntered += OnPlayerEnterProjectileRange;
        playerDetectorProjectile.BodyExited += OnPlayerExitProjectileRange;

        animatedSprite.Play("walk");
        if (raycastLeft != null) { raycastLeft.Enabled = true; raycastLeft.CollisionMask = 1; }
        if (raycastRight != null) { raycastRight.Enabled = true; raycastRight.CollisionMask = 1; }

        idleTimer = IdleWaitTime;
    }

    // ========================================
    // PLATFORM KONTROL
    // ========================================
    private bool FindPlatformAndSetTarget()
    {
        if (platformRayCast == null) return false;

        platformRayCast.TargetPosition = new Vector2(direction * 250, 30);
        platformRayCast.ForceRaycastUpdate();
        if (platformRayCast.IsColliding())
        {
            Vector2 hit = platformRayCast.GetCollisionPoint();
            if (Mathf.Abs(hit.X - GlobalPosition.X) > 40)
            { jumpTargetPoint = hit; return true; }
        }

        platformRayCast.TargetPosition = new Vector2(direction * 150, -250);
        platformRayCast.ForceRaycastUpdate();
        if (platformRayCast.IsColliding())
        {
            Vector2 hit = platformRayCast.GetCollisionPoint();
            if (Mathf.Abs(hit.X - GlobalPosition.X) > 40)
            { jumpTargetPoint = hit; return true; }
        }

        platformRayCast.TargetPosition = new Vector2(direction * 150, 250);
        platformRayCast.ForceRaycastUpdate();
        if (platformRayCast.IsColliding())
        {
            Vector2 hit = platformRayCast.GetCollisionPoint();
            if (Mathf.Abs(hit.X - GlobalPosition.X) > 40)
            { jumpTargetPoint = hit; return true; }
        }

        return false;
    }

    private void CalculateAndJump()
    {
        if (!IsOnFloor()) return;

        float dx = jumpTargetPoint.X - GlobalPosition.X;
        float dy = jumpTargetPoint.Y - GlobalPosition.Y;
        float absDx = Mathf.Abs(dx);

        float flightTime = 0.6f;
        if (absDx > 200) flightTime = 0.8f;
        if (absDx > 350) flightTime = 1.0f;
        if (absDx < 80) flightTime = 0.4f;

        float vx = dx / flightTime;
        float vy = (dy - 0.5f * Gravity * flightTime * flightTime) / flightTime;

        vy = Mathf.Clamp(vy, MaxJumpForce, MinJumpForce);
        vx = Mathf.Clamp(vx, -500, 500);

        if (isChasing)
        {
            vy *= ChaseJumpForceMultiplier;
            vy = Mathf.Clamp(vy, MaxJumpForce * 1.3f, MinJumpForce);
        }

        // ✅ İleri boost ekle
        vx += direction * JumpForwardBoost;

        Velocity = new Vector2(vx, vy);
        isJumping = true;
        justJumped = true;
        jumpCooldownTimer = PlatformJumpCooldown;
    }

    // ========================================
    // PLAYER DETECTION
    // ========================================
    private void OnPlayerEnterProjectileRange(Node2D body)
    {
        if (body.IsInGroup("player")) { playerInProjectileRange = true; player = body; StartChasing(); if (isIdle) StartWakeUp(); }
    }
    private void OnPlayerExitProjectileRange(Node2D body)
    {
        if (body.IsInGroup("player")) { playerInProjectileRange = false; if (!playerInRange) StopChasing(); }
    }
    private void OnPlayerEnterRange(Node2D body)
    {
        if (body.IsInGroup("player")) { playerInRange = true; player = body; StartChasing(); if (isIdle) StartWakeUp(); }
    }
    private void OnPlayerExitRange(Node2D body)
    {
        if (body.IsInGroup("player")) { playerInRange = false; if (!playerInProjectileRange) StopChasing(); }
    }

    // ✅ Kovalama başla/bitir
    private void StartChasing()
    {
        if (!isChasing)
        {
            isChasing = true;
            chaseTimer = 0;
            chaseSpeedMultiplier = 1.0f;
        }
    }

    private void StopChasing()
    {
        isChasing = false;
        chaseTimer = 0;
        chaseSpeedMultiplier = 1.0f;
        Speed = originalSpeed;
    }

    // ✅ Kovalama hızlanma güncelle
    private void UpdateChaseAcceleration(float dt)
    {
        if (isChasing)
        {
            chaseTimer += dt;
            // Lerp: 1.0 → ChaseMaxSpeedMultiplier, ChaseAccelerationTime sürede
            float t = Mathf.Clamp(chaseTimer / ChaseAccelerationTime, 0, 1);
            chaseSpeedMultiplier = Mathf.Lerp(1.0f, ChaseMaxSpeedMultiplier, t);
            Speed = originalSpeed * chaseSpeedMultiplier;
        }
    }

    // ========================================
    // PHYSICS
    // ========================================
    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        if (jumpCooldownTimer > 0) jumpCooldownTimer -= dt;
        if (projectileCooldownTimer > 0) projectileCooldownTimer -= dt;
        if (attackTimer > 0) attackTimer -= dt;

        // ✅ Kovalama hızlanma
        UpdateChaseAcceleration(dt);

        if (isStunned)
        {
            stunTimer -= dt;
            if (stunTimer <= 0) { isStunned = false; Speed = originalSpeed; }
            return;
        }

        if (isDead) return;
        if (isHurt) return;
        if (isGoingIdle || isWakingUp) return;
        if (isIdle) { HandleIdle(delta); return; }
        if (isPrepJump) return;
        if (isAttacking || isProjectileAttacking) return;

        if (playerInRange && player != null && attackTimer <= 0)
        {
            direction = player.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
            animatedSprite.FlipH = direction > 0;
            StartMeleeAttack();
            return;
        }

        if (playerInProjectileRange && !playerInRange && player != null && projectileCooldownTimer <= 0)
        {
            direction = player.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
            animatedSprite.FlipH = direction > 0;
            StartProjectileAttack();
            return;
        }

        if (!playerInRange && !playerInProjectileRange)
        {
            idleTimer -= dt;
            if (idleTimer <= 0) { StartGoingIdle(); return; }
        }
        else { idleTimer = IdleWaitTime; }

        Move(delta);
    }

    // ========================================
    // IDLE
    // ========================================
    private async void StartGoingIdle()
    {
        isGoingIdle = true;
        Velocity = Vector2.Zero;
        if (animatedSprite.SpriteFrames.HasAnimation("going_idle"))
        {
            animatedSprite.Play("going_idle");
            float fc = animatedSprite.SpriteFrames.GetFrameCount("going_idle");
            double fps = animatedSprite.SpriteFrames.GetAnimationSpeed("going_idle");
            await ToSignal(GetTree().CreateTimer(fc / fps), SceneTreeTimer.SignalName.Timeout);
        }
        isGoingIdle = false;
        isIdle = true;
        idleStayTimer = IdleDuration;
        if (animatedSprite.SpriteFrames.HasAnimation("idle"))
            animatedSprite.Play("idle");
    }

    private void HandleIdle(double delta)
    {
        idleStayTimer -= (float)delta;
        if (idleStayTimer <= 0) StartWakeUp();
    }

    private async void StartWakeUp()
    {
        if (isWakingUp) return;
        isIdle = false;
        isWakingUp = true;
        if (animatedSprite.SpriteFrames.HasAnimation("idle_wake"))
        {
            animatedSprite.Play("idle_wake");
            float fc = animatedSprite.SpriteFrames.GetFrameCount("idle_wake");
            double fps = animatedSprite.SpriteFrames.GetAnimationSpeed("idle_wake");
            await ToSignal(GetTree().CreateTimer(fc / fps), SceneTreeTimer.SignalName.Timeout);
        }
        isWakingUp = false;
        idleTimer = IdleWaitTime;
        animatedSprite.Play("walk");
    }

    // ========================================
    // PROJECTILE
    // ========================================
    private async void StartProjectileAttack()
    {
        isProjectileAttacking = true;
        projectilesFired = 0;
        Velocity = Vector2.Zero;
        if (animatedSprite.SpriteFrames.HasAnimation("projectile_attack"))
        {
            animatedSprite.Play("projectile_attack");
            while (animatedSprite.Animation == "projectile_attack")
            {
                int frame = animatedSprite.Frame;
                if ((frame == 7 || frame == 8) && projectilesFired == 0) { FireProjectile(); projectilesFired = 1; }
                else if ((frame == 12 || frame == 13) && projectilesFired == 1) { FireProjectile(); projectilesFired = 2; }
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (!animatedSprite.IsPlaying() || animatedSprite.Animation != "projectile_attack") break;
            }
            if (animatedSprite.Animation == "projectile_attack" && animatedSprite.IsPlaying())
                await ToSignal(animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);
        }
        projectileCooldownTimer = ProjectileCooldown;
        isProjectileAttacking = false;
        animatedSprite.Play("walk");
    }

    private void FireProjectile()
    {
        if (ProjectileScene == null) return;
        var p = ProjectileScene.Instantiate<Node2D>();
        p.GlobalPosition = GlobalPosition + new Vector2(direction * 30, -10);
        if (p.HasMethod("Setup")) p.Call("Setup", direction, ProjectileDamage + bonusDamage, false, 0f);
        GetTree().CurrentScene.AddChild(p);
    }

    // ========================================
    // MELEE
    // ========================================
    private async void StartMeleeAttack()
    {
        isAttacking = true;
        Velocity = Vector2.Zero;
        if (attackShape != null) attackShape.Position = new Vector2(direction * 20, 0);
        animatedSprite.Play("attack");
        while (animatedSprite.Animation == "attack")
        {
            if (animatedSprite.Frame >= 8 && animatedSprite.Frame <= 10)
            {
                attackCollision.Monitoring = true;
                await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
                attackCollision.Monitoring = false;
                break;
            }
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        attackTimer = AttackCooldown;
        isAttacking = false;
        animatedSprite.Play("walk");
    }

    private void OnAttackHit(Node2D body)
    {
        if (body.IsInGroup("player") && body.HasMethod("TakeDamage")) body.Call("TakeDamage", 1 + bonusDamage);
    }

    // ========================================
    // HAREKET
    // ========================================
    private void Move(double delta)
    {
        Vector2 velocity = Velocity;

        if (justJumped)
        {
            justJumped = false;
            MoveAndSlide();
            return;
        }

        if (!IsOnFloor())
        {
            velocity.Y += Gravity * (float)delta;
            if (!isPrepJump && animatedSprite.Animation != "airborn")
                if (animatedSprite.SpriteFrames.HasAnimation("airborn"))
                    animatedSprite.Play("airborn");
            isJumping = true;
        }
        else
        {
            velocity.Y = 0;
            if (isJumping) { isJumping = false; animatedSprite.Play("walk"); }
        }

        if (isChasing && player != null && IsInstanceValid(player))
            direction = player.GlobalPosition.X > GlobalPosition.X ? 1 : -1;

        // ✅ Speed zaten UpdateChaseAcceleration tarafından güncelleniyor
        velocity.X = direction * Speed;
        animatedSprite.FlipH = direction > 0;
        if (attackShape != null) attackShape.Position = new Vector2(direction * 20, 0);

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
            bool cliffAhead = false;
            if (direction > 0 && !raycastRight.IsColliding()) cliffAhead = true;
            else if (direction < 0 && !raycastLeft.IsColliding()) cliffAhead = true;

            if (cliffAhead && jumpCooldownTimer <= 0)
            {
                if (FindPlatformAndSetTarget())
                {
                    if (isChasing) { StartPrepJump(); return; }
                    else
                    {
                        float dy = jumpTargetPoint.Y - GlobalPosition.Y;
                        if (Mathf.Abs(dy) < 80) { StartPrepJump(); return; }
                    }
                }
                direction *= -1;
            }
            else if (cliffAhead) { direction *= -1; }
        }
    }

    private async void StartPrepJump()
    {
        isPrepJump = true;
        Velocity = Vector2.Zero;
        if (animatedSprite.SpriteFrames.HasAnimation("prep_jump"))
        {
            animatedSprite.Play("prep_jump");
            float fc = animatedSprite.SpriteFrames.GetFrameCount("prep_jump");
            double fps = animatedSprite.SpriteFrames.GetAnimationSpeed("prep_jump");
            await ToSignal(GetTree().CreateTimer(fc / fps), SceneTreeTimer.SignalName.Timeout);
        }
        CalculateAndJump();
        isPrepJump = false;
    }

    // ========================================
    // HASAR / STUN / ÖLÜM
    // ========================================
    public void AddBonusDamage(int amount) { bonusDamage = Mathf.Max(0, bonusDamage + amount); }
    public void ApplyStun(float duration) { isStunned = true; stunTimer = duration; }
    public void ApplySlow(float slowPercent, float duration)
    {
        Speed = originalSpeed * (1.0f - slowPercent);
        GetTree().CreateTimer(duration).Timeout += () => { if (!isStunned) Speed = originalSpeed; };
    }

    public void TakeDamage(int damage = 1)
    {
        if (isDead) return;
        currentHealth -= damage;
        if (isIdle || isGoingIdle) { isIdle = false; isGoingIdle = false; }
        if (currentHealth <= 0) { Die(); return; }
        PlayHurt();
    }

    private async void PlayHurt()
    {
        if (isDead) return;
        isHurt = true; isAttacking = false; isProjectileAttacking = false; isPrepJump = false;
        Velocity = Vector2.Zero;
        animatedSprite.Play("hurt");
        float fc = animatedSprite.SpriteFrames.GetFrameCount("hurt");
        double fps = animatedSprite.SpriteFrames.GetAnimationSpeed("hurt");
        await ToSignal(GetTree().CreateTimer(fc / fps), SceneTreeTimer.SignalName.Timeout);
        if (isDead) return;
        isHurt = false;
        idleTimer = IdleWaitTime;
        animatedSprite.Play("walk");
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true; isHurt = false; isAttacking = false; isProjectileAttacking = false;
        SetPhysicsProcess(false);
        var col = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (col != null) col.SetDeferred("disabled", true);
        if (attackCollision != null) attackCollision.Monitoring = false;
        if (playerDetector != null) playerDetector.Monitoring = false;
        if (playerDetectorProjectile != null) playerDetectorProjectile.Monitoring = false;
        if (animatedSprite.SpriteFrames.HasAnimation("death"))
        {
            animatedSprite.Play("death");
            float fc = animatedSprite.SpriteFrames.GetFrameCount("death");
            double fps = animatedSprite.SpriteFrames.GetAnimationSpeed("death");
            GetTree().CreateTimer(fc / fps).Timeout += () => { if (IsInstanceValid(this)) QueueFree(); };
        }
        else QueueFree();
    }
}