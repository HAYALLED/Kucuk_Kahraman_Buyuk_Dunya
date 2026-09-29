using Godot;
using System;

public partial class TrashJuggernaut : CharacterBody2D
{
    [Export] public float Speed = 50.0f;
    [Export] public float Gravity = 980.0f;
    [Export] public int MaxHealth = 10;
    [Export] public float AttackRange = 100.0f;
    [Export] public float AttackCooldown = 2.0f;
    [Export] public float ChargeSpeed = 300.0f;
    [Export] public float MaxChargeDuration = 4.0f;
    [Export] public float ChargeCooldown = 5.0f;
    [Export] public int ChargeDamage = 2;
    [Export] public float JumpForce = -350.0f;
    [Export] public float PlatformJumpCooldown = 1.0f;
    [Export] public float ChaseJumpForceMultiplier = 1.3f;
    [Export] public float MinJumpForce = -250.0f;
    [Export] public float MaxJumpForce = -600.0f;

    // ✅ YENİ: Zıplama İleri Boost
    [ExportGroup("Zıplama Boost")]
    [Export] public float JumpForwardBoost = 150.0f;

    // ✅ YENİ: Kovalama Hızlanma
    [ExportGroup("Kovalama Hızlanma")]
    [Export] public float ChaseMaxSpeedMultiplier = 2.0f;
    [Export] public float ChaseAccelerationTime = 3.0f;

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

    private bool isCharging = false;
    private float chargeTimer = 0;
    private float chargeCooldownTimer = 0;
    private bool playerInChargeRange = false;
    private bool canCharge = true;

    private float jumpCooldownTimer = 0;
    private bool isJumping = false;
    private bool isChasing = false;
    private bool playerInRange = false;
    private bool justJumped = false;

    private float chaseSpeedMultiplier = 1.0f;
    private float chaseTimer = 0;

    private Vector2 jumpTargetPoint = Vector2.Zero;

    private AnimatedSprite2D animatedSprite;
    private Area2D attackCollision;
    private CollisionShape2D attackShape;
    private Area2D playerDetector;
    private Area2D playerDetectorCharge;
    private RayCast2D raycastLeft;
    private RayCast2D raycastRight;
    private RayCast2D platformRayCast;
    private Node2D player;

    public override void _Ready()
    {
        originalSpeed = Speed;

        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        attackCollision = GetNode<Area2D>("attack_collision");
        playerDetector = GetNode<Area2D>("player_detector");
        playerDetectorCharge = GetNode<Area2D>("player_detector_charg");
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
        playerDetectorCharge.CollisionMask = 2;

        attackCollision.BodyEntered += OnAttackHit;
        playerDetector.BodyEntered += OnPlayerEnterRange;
        playerDetector.BodyExited += OnPlayerExitRange;
        playerDetectorCharge.BodyEntered += OnPlayerEnterChargeRange;
        playerDetectorCharge.BodyExited += OnPlayerExitChargeRange;

        animatedSprite.Play("walk");
        if (raycastLeft != null) { raycastLeft.Enabled = true; raycastLeft.CollisionMask = 1; }
        if (raycastRight != null) { raycastRight.Enabled = true; raycastRight.CollisionMask = 1; }
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

        // ✅ İleri boost
        vx += direction * JumpForwardBoost;

        Velocity = new Vector2(vx, vy);
        isJumping = true;
        justJumped = true;
        jumpCooldownTimer = PlatformJumpCooldown;
    }

    // ========================================
    // PLAYER DETECTION
    // ========================================
    private void OnPlayerEnterRange(Node2D body)
    {
        if (body.IsInGroup("player")) { playerInRange = true; player = body; StartChasing(); }
    }
    private void OnPlayerExitRange(Node2D body)
    {
        if (body.IsInGroup("player")) { playerInRange = false; if (!playerInChargeRange) StopChasing(); }
    }
    private void OnPlayerEnterChargeRange(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInChargeRange = true; player = body; StartChasing();
            if (canCharge && !isCharging && !isAttacking && !isHurt) StartCharge();
        }
    }
    private void OnPlayerExitChargeRange(Node2D body)
    {
        if (body.IsInGroup("player")) { playerInChargeRange = false; if (!playerInRange) StopChasing(); }
    }

    private void StartChasing()
    {
        if (!isChasing) { isChasing = true; chaseTimer = 0; chaseSpeedMultiplier = 1.0f; }
    }

    private void StopChasing()
    {
        isChasing = false; chaseTimer = 0; chaseSpeedMultiplier = 1.0f; Speed = originalSpeed;
    }

    private void UpdateChaseAcceleration(float dt)
    {
        if (isChasing && !isCharging)  // Charge sırasında ChargeSpeed kullanılır
        {
            chaseTimer += dt;
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
        if (chargeCooldownTimer > 0) { chargeCooldownTimer -= dt; if (chargeCooldownTimer <= 0) canCharge = true; }
        if (jumpCooldownTimer > 0) jumpCooldownTimer -= dt;
        if (attackTimer > 0) attackTimer -= dt;

        UpdateChaseAcceleration(dt);

        if (isStunned) { stunTimer -= dt; if (stunTimer <= 0) { isStunned = false; Speed = originalSpeed; } return; }
        if (isDead) return;
        if (isHurt) return;
        if (isCharging) { ChargeMove(delta); return; }
        if (isAttacking) return;

        if (playerInRange && player != null && attackTimer <= 0)
        {
            direction = player.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
            animatedSprite.FlipH = direction > 0;
            StartAttack();
            return;
        }

        Move(delta);
    }

    // ========================================
    // CHARGE
    // ========================================
    private void StartCharge()
    {
        if (player == null) return;
        isCharging = true; canCharge = false; chargeTimer = MaxChargeDuration;
        direction = player.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
        animatedSprite.FlipH = direction > 0;
        if (animatedSprite.SpriteFrames.HasAnimation("charge_atk")) animatedSprite.Play("charge_atk");
        attackCollision.Monitoring = true;
    }

    private void ChargeMove(double delta)
    {
        Vector2 velocity = Velocity;
        velocity.Y += Gravity * (float)delta;
        velocity.X = direction * ChargeSpeed;  // Charge kendi hızını kullanır
        Velocity = velocity;
        MoveAndSlide();
        chargeTimer -= (float)delta;

        if (IsOnWall()) { StopCharge(); return; }
        if (chargeTimer <= 0)
        {
            if (playerInChargeRange)
            {
                chargeTimer = MaxChargeDuration;
                if (player != null) { direction = player.GlobalPosition.X > GlobalPosition.X ? 1 : -1; animatedSprite.FlipH = direction > 0; }
            }
            else StopCharge();
        }
    }

    private void StopCharge()
    {
        isCharging = false; attackCollision.Monitoring = false;
        chargeCooldownTimer = ChargeCooldown; animatedSprite.Play("walk");
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

        if (!IsOnFloor()) { velocity.Y += Gravity * (float)delta; isJumping = true; }
        else { velocity.Y = 0; isJumping = false; }

        if (isChasing && player != null && IsInstanceValid(player))
            direction = player.GlobalPosition.X > GlobalPosition.X ? 1 : -1;

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
                    if (isChasing) { CalculateAndJump(); return; }
                    else
                    {
                        float dy = jumpTargetPoint.Y - GlobalPosition.Y;
                        if (Mathf.Abs(dy) < 80) { CalculateAndJump(); return; }
                    }
                }
                direction *= -1;
            }
            else if (cliffAhead) { direction *= -1; }
        }
    }

    // ========================================
    // SALDIRI
    // ========================================
    private async void StartAttack()
    {
        isAttacking = true; Velocity = Vector2.Zero;
        if (attackShape != null) attackShape.Position = new Vector2(direction * 20, 0);
        animatedSprite.Play("attack");
        while (animatedSprite.Animation == "attack")
        {
            if (animatedSprite.Frame >= 13 && animatedSprite.Frame <= 19)
            {
                attackCollision.Monitoring = true;
                await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
                attackCollision.Monitoring = false;
                break;
            }
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        attackTimer = AttackCooldown; isAttacking = false; animatedSprite.Play("walk");
    }

    private void OnAttackHit(Node2D body)
    {
        if (body.IsInGroup("player") && body.HasMethod("TakeDamage"))
            body.Call("TakeDamage", isCharging ? ChargeDamage + bonusDamage : 1 + bonusDamage);
    }

    // ========================================
    // HASAR / STUN / ÖLÜM
    // ========================================
    public void AddBonusDamage(int amount) { bonusDamage = Mathf.Max(0, bonusDamage + amount); }

    public void ApplyStun(float duration) { isStunned = true; stunTimer = duration; if (isCharging) StopCharge(); }
    public void ApplySlow(float slowPercent, float duration)
    {
        Speed = originalSpeed * (1.0f - slowPercent);
        GetTree().CreateTimer(duration).Timeout += () => { if (!isStunned) Speed = originalSpeed; };
    }

    public void TakeDamage(int damage = 1)
    {
        if (isDead) return;
        currentHealth -= damage;
        if (currentHealth <= 0) { Die(); return; }
        PlayHurt();
    }

    private async void PlayHurt()
    {
        if (isDead) return;
        isHurt = true; isAttacking = false;
        if (isCharging) StopCharge();
        Velocity = Vector2.Zero;
        for (int i = 0; i < 2; i++)
        {
            if (isDead) return;
            animatedSprite.Play("hurt");
            double fc = animatedSprite.SpriteFrames.GetFrameCount("hurt");
            double fps = animatedSprite.SpriteFrames.GetAnimationSpeed("hurt");
            await ToSignal(GetTree().CreateTimer(fc / fps), SceneTreeTimer.SignalName.Timeout);
        }
        if (isDead) return;
        isHurt = false; animatedSprite.Play("walk");
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true; isHurt = false; isAttacking = false; isCharging = false;
        SetPhysicsProcess(false);
        var col = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (col != null) col.SetDeferred("disabled", true);
        if (attackCollision != null) attackCollision.Monitoring = false;
        if (playerDetector != null) playerDetector.Monitoring = false;
        if (playerDetectorCharge != null) playerDetectorCharge.Monitoring = false;
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