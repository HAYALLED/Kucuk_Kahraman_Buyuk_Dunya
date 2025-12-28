using Godot;
using System;

public partial class TrashJuggernaut : CharacterBody2D
{
    // Temel Ayarlar
    [Export] public float Speed = 50.0f;
    [Export] public float Gravity = 980.0f;
    [Export] public int MaxHealth = 10;

    // Saldırı Ayarları
    [Export] public float AttackRange = 100.0f;
    [Export] public float AttackCooldown = 2.0f;

    // CHARGE AYARLARI
    [Export] public float ChargeSpeed = 300.0f;
    [Export] public float MaxChargeDuration = 3.0f;
    [Export] public float ChargeCooldown = 5.0f;
    [Export] public int ChargeDamage = 2;

    // ✅ ATLAMA AYARLARI
    [Export] public float JumpForce = -350.0f;         // Zıplama gücü
    [Export] public float PlatformJumpCooldown = 1.0f; // Atlama cooldown

    // Değişkenler
    private int currentHealth;
    private int direction = 1;
    private bool isDead = false;
    private bool isAttacking = false;
    private float attackTimer = 0;
    private bool isHurt = false;
    private bool isStunned = false;
    private float stunTimer = 0;
    private float originalSpeed;

    // CHARGE DEĞİŞKENLERİ
    private bool isCharging = false;
    private float chargeTimer = 0;
    private float chargeCooldownTimer = 0;
    private bool playerInChargeRange = false;
    private bool canCharge = true;

    // ✅ PLATFORM ATLAMA DEĞİŞKENLERİ
    private bool platformAhead = false;      // Önde platform var mı
    private float jumpCooldownTimer = 0;     // Atlama cooldown
    private bool isJumping = false;          // Zıplıyor mu

    // Node'lar
    private AnimatedSprite2D animatedSprite;
    private Area2D attackCollision;
    private CollisionShape2D attackShape;
    private Area2D playerDetector;
    private Area2D playerDetectorCharge;
    private Area2D platformDetector;
    private RayCast2D raycastLeft;
    private RayCast2D raycastRight;
    private RayCast2D platformRaycast;  // ✅ Önde platform kontrolü
    private Node2D player;

    public override void _Ready()
    {
        originalSpeed = Speed;

        // Node'ları al
        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        attackCollision = GetNode<Area2D>("attack_collision");
        playerDetector = GetNode<Area2D>("player_detector");
        playerDetectorCharge = GetNode<Area2D>("player_detector_charg");
        platformDetector = GetNode<Area2D>("platform_detector");
        raycastLeft = GetNode<RayCast2D>("RayCast2Dleft");
        raycastRight = GetNode<RayCast2D>("RayCast2Dright");

        AddToGroup("enemy");

        attackShape = attackCollision.GetNode<CollisionShape2D>("CollisionShape2D");

        currentHealth = MaxHealth;
        attackCollision.Monitoring = false;

        var players = GetTree().GetNodesInGroup("player");
        if (players.Count > 0)
            player = players[0] as Node2D;

        playerDetector.CollisionMask = 2;
        playerDetectorCharge.CollisionMask = 2;

        // Sinyaller
        attackCollision.BodyEntered += OnAttackHit;
        playerDetector.BodyEntered += OnPlayerEnterRange;
        playerDetector.BodyExited += OnPlayerExitRange;
        playerDetectorCharge.BodyEntered += OnPlayerEnterChargeRange;
        playerDetectorCharge.BodyExited += OnPlayerExitChargeRange;

        // ✅ Platform detector sinyalleri
        platformDetector.BodyEntered += OnPlatformDetected;
        platformDetector.BodyExited += OnPlatformLost;
        platformDetector.CollisionMask = 1;  // Ground layer
        platformDetector.Monitoring = true;

        animatedSprite.Play("walk");

        if (raycastLeft != null)
        {
            raycastLeft.Enabled = true;
            raycastLeft.CollisionMask = 1;
        }

        if (raycastRight != null)
        {
            raycastRight.Enabled = true;
            raycastRight.CollisionMask = 1;
        }
    }

    // ✅ PLATFORM ALGILAMA
    private void OnPlatformDetected(Node2D body)
    {
        if (body.IsInGroup("ground") || body is TileMap || body is StaticBody2D)
        {
            platformAhead = true;
        }
    }

    private void OnPlatformLost(Node2D body)
    {
        // Hala başka platform var mı kontrol et
        var overlapping = platformDetector.GetOverlappingBodies();
        platformAhead = overlapping.Count > 0;

        if (!platformAhead)
        {
            GD.Print("[JUGGERNAUT] Önde platform yok!");
        }
    }

    private bool playerInRange = false;

    private void OnPlayerEnterRange(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInRange = true;
            player = body;
        }
    }

    private void OnPlayerExitRange(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInRange = false;
        }
    }

    private void OnPlayerEnterChargeRange(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInChargeRange = true;
            player = body;

            if (canCharge && !isCharging && !isAttacking && !isHurt)
            {
                StartCharge();
            }
        }
    }

    private void OnPlayerExitChargeRange(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInChargeRange = false;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Cooldown timers
        if (chargeCooldownTimer > 0)
        {
            chargeCooldownTimer -= (float)delta;
            if (chargeCooldownTimer <= 0)
                canCharge = true;
        }

        // ✅ Jump cooldown
        if (jumpCooldownTimer > 0)
            jumpCooldownTimer -= (float)delta;

        // Stun kontrolü
        if (isStunned)
        {
            stunTimer -= (float)delta;
            if (stunTimer <= 0)
            {
                isStunned = false;
                Speed = originalSpeed;
            }
            return;
        }

        if (isDead) return;
        if (isHurt) return;

        if (attackTimer > 0)
            attackTimer -= (float)delta;

        // CHARGE DURUMU
        if (isCharging)
        {
            ChargeMove(delta);
            return;
        }

        if (isAttacking) return;

        // Yakın menzilde normal saldırı
        if (playerInRange && player != null && attackTimer <= 0)
        {
            direction = player.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
            animatedSprite.FlipH = direction > 0;
            StartAttack();
            return;
        }

        // Normal hareket
        Move(delta);
    }

    private void StartCharge()
    {
        if (player == null) return;

        isCharging = true;
        canCharge = false;
        chargeTimer = MaxChargeDuration;

        direction = player.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
        animatedSprite.FlipH = direction > 0;

        // ✅ Platform detector pozisyonunu güncelle
        UpdatePlatformDetectorPosition();

        if (animatedSprite.SpriteFrames.HasAnimation("charge_atk"))
        {
            animatedSprite.Play("charge_atk");
        }

        attackCollision.Monitoring = true;
    }

    private void ChargeMove(double delta)
    {
        Vector2 velocity = Velocity;

        velocity.Y += Gravity * (float)delta;
        velocity.X = direction * ChargeSpeed;

        Velocity = velocity;
        MoveAndSlide();

        chargeTimer -= (float)delta;

        if (chargeTimer <= 0)
        {
            StopCharge();
        }
        else if (!playerInChargeRange && chargeTimer < MaxChargeDuration - 0.5f)
        {
            StopCharge();
        }
        else if (IsOnWall())
        {
            StopCharge();
        }
    }

    private void StopCharge()
    {
        isCharging = false;
        attackCollision.Monitoring = false;
        chargeCooldownTimer = ChargeCooldown;

        animatedSprite.Play("walk");
    }

    public void ApplyStun(float duration)
    {
        isStunned = true;
        stunTimer = duration;

        if (isCharging)
            StopCharge();
    }

    public void ApplySlow(float slowPercent, float duration)
    {
        Speed = originalSpeed * (1.0f - slowPercent);

        GetTree().CreateTimer(duration).Timeout += () =>
        {
            if (!isStunned)
                Speed = originalSpeed;
        };
    }

    private void Move(double delta)
    {
        Vector2 velocity = Velocity;

        // Yerçekimi
        if (!IsOnFloor())
        {
            velocity.Y += Gravity * (float)delta;
            isJumping = true;
        }
        else
        {
            velocity.Y = 0;
            isJumping = false;
        }

        // Yatay hareket
        velocity.X = direction * Speed;

        animatedSprite.FlipH = direction > 0;

        if (attackShape != null)
        {
            attackShape.Position = new Vector2(direction * 20, 0);
        }

        // ✅ Platform detector pozisyonunu güncelle
        UpdatePlatformDetectorPosition();

        Velocity = velocity;
        MoveAndSlide();

        // Yön kontrolü (duvar ve uçurum)
        CheckDirection();
    }

    // ✅ Platform detector'ı yöne göre konumlandır
    private void UpdatePlatformDetectorPosition()
    {
        if (platformDetector == null) return;

        var shape = platformDetector.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (shape != null)
        {
            // Karakterin önünde ve aşağısında
            shape.Position = new Vector2(direction * 80, 50);
        }
    }

    private void CheckDirection()
    {
        if (raycastLeft == null || raycastRight == null) return;

        // Duvar kontrolü - her zaman dön
        if (IsOnWall())
        {
            direction *= -1;
            return;
        }

        // Uçurum kontrolü (sadece yerdeyken)
        if (IsOnFloor())
        {
            bool cliffAhead = false;

            if (direction > 0 && !raycastRight.IsColliding())
            {
                cliffAhead = true;
            }
            else if (direction < 0 && !raycastLeft.IsColliding())
            {
                cliffAhead = true;
            }

            // ✅ Uçurum varsa
            if (cliffAhead)
            {
                // Önde platform var mı?
                if (platformAhead && jumpCooldownTimer <= 0)
                {
                    // Platform var → ATLA!
                    JumpToPlatform();
                }
                else
                {
                    // Platform yok → DÖN
                    direction *= -1;
                }
            }
        }
    }

    // ✅ PLATFORMA ATLA
    private void JumpToPlatform()
    {
        if (!IsOnFloor()) return;

        Vector2 velocity = Velocity;
        velocity.Y = JumpForce;
        velocity.X = direction * Speed * 1.5f;  // Biraz hızlanarak atla
        Velocity = velocity;

        isJumping = true;
        jumpCooldownTimer = PlatformJumpCooldown;

    }

    private async void StartAttack()
    {
        isAttacking = true;
        Velocity = Vector2.Zero;

        if (attackShape != null)
        {
            attackShape.Position = new Vector2(direction * 20, 0);
        }

        animatedSprite.Play("attack");

        while (animatedSprite.Animation == "attack")
        {
            int frame = animatedSprite.Frame;

            if (frame >= 13 && frame <= 19)
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
        if (body.IsInGroup("player"))
        {
            if (body.HasMethod("TakeDamage"))
            {
                int damage = isCharging ? ChargeDamage : 1;
                body.Call("TakeDamage", damage);
            }
        }
    }

    public void TakeDamage(int damage = 1)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        PlayHurt();
    }

    private async void PlayHurt()
    {
        if (isDead) return;

        isHurt = true;
        isAttacking = false;

        if (isCharging)
            StopCharge();

        Velocity = Vector2.Zero;

        for (int i = 0; i < 2; i++)
        {
            if (isDead) return;

            animatedSprite.Play("hurt");

            double frameCount = animatedSprite.SpriteFrames.GetFrameCount("hurt");
            double fps = animatedSprite.SpriteFrames.GetAnimationSpeed("hurt");
            double duration = frameCount / fps;

            await ToSignal(GetTree().CreateTimer(duration), SceneTreeTimer.SignalName.Timeout);
        }

        if (isDead) return;

        isHurt = false;
        animatedSprite.Play("walk");
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        isHurt = false;
        isAttacking = false;
        isCharging = false;
        SetPhysicsProcess(false);

        var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (collision != null)
            collision.SetDeferred("disabled", true);

        if (attackCollision != null)
            attackCollision.Monitoring = false;

        if (playerDetector != null)
            playerDetector.Monitoring = false;

        if (playerDetectorCharge != null)
            playerDetectorCharge.Monitoring = false;

        if (animatedSprite.SpriteFrames.HasAnimation("death"))
        {
            animatedSprite.Play("death");

            float frameCount = animatedSprite.SpriteFrames.GetFrameCount("death");
            double fps = animatedSprite.SpriteFrames.GetAnimationSpeed("death");
            double duration = frameCount / fps;

            GetTree().CreateTimer(duration).Timeout += () =>
            {
                if (IsInstanceValid(this))
                    QueueFree();
            };
        }
        else
        {
            QueueFree();
        }
    }
}