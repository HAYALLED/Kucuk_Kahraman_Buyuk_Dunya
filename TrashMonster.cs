using Godot;
using System;

public partial class TrashMonster : CharacterBody2D
{
    // Temel Ayarlar
    [Export] public float Speed = 50.0f;
    [Export] public float Gravity = 980.0f;
    [Export] public int MaxHealth = 10;

    // Saldırı Ayarları
    [Export] public float AttackRange = 100.0f;
    [Export] public float AttackCooldown = 2.0f;

    // ✅ PROJECTILE AYARLARI
    [Export] public PackedScene ProjectileScene;
    [Export] public float ProjectileCooldown = 3.0f;
    [Export] public int ProjectileDamage = 1;

    // ✅ IDLE AYARLARI
    [Export] public float IdleWaitTime = 5.0f;       // Kaç saniye sonra idle'a geçer
    [Export] public float IdleDuration = 2.0f;       // Idle'da kalma süresi

    // ATLAMA AYARLARI
    [Export] public float JumpForce = -350.0f;
    [Export] public float PlatformJumpCooldown = 1.0f;

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

    // ✅ PROJECTILE DEĞİŞKENLERİ
    private bool playerInProjectileRange = false;
    private float projectileCooldownTimer = 0;
    private bool isProjectileAttacking = false;
    private int projectilesFired = 0;  // Bu saldırıda kaç projectile atıldı

    // ✅ IDLE DEĞİŞKENLERİ
    private float idleTimer = 0;           // Idle'a geçmek için sayaç
    private float idleStayTimer = 0;       // Idle'da kalma sayacı
    private bool isIdle = false;
    private bool isGoingIdle = false;
    private bool isWakingUp = false;

    // PLATFORM ATLAMA DEĞİŞKENLERİ
    private bool platformAhead = false;
    private float jumpCooldownTimer = 0;
    private bool isJumping = false;
    private bool isPrepJump = false;  // ✅ Atlama hazırlığı

    // Node'lar
    private AnimatedSprite2D animatedSprite;
    private Area2D attackCollision;
    private CollisionShape2D attackShape;
    private Area2D playerDetector;
    private Area2D playerDetectorProjectile;  // ✅ Projectile menzili
    private Area2D platformDetector;
    private RayCast2D raycastLeft;
    private RayCast2D raycastRight;
    private Node2D player;

    public override void _Ready()
    {
        originalSpeed = Speed;

        // Node'ları al
        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        attackCollision = GetNode<Area2D>("attack_collision");
        playerDetector = GetNode<Area2D>("player_detector");
        playerDetectorProjectile = GetNode<Area2D>("player_detector_projectile");  // ✅
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
        playerDetectorProjectile.CollisionMask = 2;  // ✅

        // Sinyaller
        attackCollision.BodyEntered += OnAttackHit;
        playerDetector.BodyEntered += OnPlayerEnterRange;
        playerDetector.BodyExited += OnPlayerExitRange;

        // ✅ Projectile range sinyalleri
        playerDetectorProjectile.BodyEntered += OnPlayerEnterProjectileRange;
        playerDetectorProjectile.BodyExited += OnPlayerExitProjectileRange;

        // Platform detector sinyalleri
        platformDetector.BodyEntered += OnPlatformDetected;
        platformDetector.BodyExited += OnPlatformLost;
        platformDetector.CollisionMask = 1;
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

        // Idle timer başlat
        idleTimer = IdleWaitTime;
    }

    // ✅ PROJECTILE RANGE SİNYALLERİ
    private void OnPlayerEnterProjectileRange(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInProjectileRange = true;
            player = body;


            // Idle'daysa uyan
            if (isIdle)
            {
                StartWakeUp();
            }
        }
    }

    private void OnPlayerExitProjectileRange(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInProjectileRange = false;
        }
    }

    // PLATFORM ALGILAMA
    private void OnPlatformDetected(Node2D body)
    {
        if (body.IsInGroup("ground") || body is TileMap || body is StaticBody2D)
        {
            platformAhead = true;
        }
    }

    private void OnPlatformLost(Node2D body)
    {
        var overlapping = platformDetector.GetOverlappingBodies();
        platformAhead = overlapping.Count > 0;
    }

    private bool playerInRange = false;

    private void OnPlayerEnterRange(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInRange = true;
            player = body;

            // Idle'daysa uyan
            if (isIdle)
            {
                StartWakeUp();
            }
        }
    }

    private void OnPlayerExitRange(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInRange = false;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Cooldown timers
        if (jumpCooldownTimer > 0)
            jumpCooldownTimer -= (float)delta;

        if (projectileCooldownTimer > 0)
            projectileCooldownTimer -= (float)delta;

        if (attackTimer > 0)
            attackTimer -= (float)delta;

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

        // ✅ IDLE SİSTEMİ
        if (isGoingIdle || isWakingUp)
            return;  // Animasyon bitene kadar bekle

        if (isIdle)
        {
            HandleIdle(delta);
            return;
        }

        // Atlama hazırlığı
        if (isPrepJump)
            return;

        // Saldırı durumları
        if (isAttacking || isProjectileAttacking)
            return;

        // ✅ SALDIRI ÖNCELİKLERİ

        // 1. Yakın menzilde melee saldırı
        if (playerInRange && player != null && attackTimer <= 0)
        {
            direction = player.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
            animatedSprite.FlipH = direction > 0;
            StartMeleeAttack();
            return;
        }

        // 2. Uzak menzilde projectile saldırısı
        if (playerInProjectileRange && !playerInRange && player != null && projectileCooldownTimer <= 0)
        {
            direction = player.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
            animatedSprite.FlipH = direction > 0;
            StartProjectileAttack();
            return;
        }

        // ✅ IDLE TIMER (player yoksa)
        if (!playerInRange && !playerInProjectileRange)
        {
            idleTimer -= (float)delta;
            if (idleTimer <= 0)
            {
                StartGoingIdle();
                return;
            }
        }
        else
        {
            // Player varsa timer'ı sıfırla
            idleTimer = IdleWaitTime;
        }

        // Normal hareket
        Move(delta);
    }

    // ═══════════════════════════════════════════
    // ✅ IDLE SİSTEMİ
    // ═══════════════════════════════════════════

    private async void StartGoingIdle()
    {
        isGoingIdle = true;
        Velocity = Vector2.Zero;



        if (animatedSprite.SpriteFrames.HasAnimation("going_idle"))
        {
            animatedSprite.Play("going_idle");

            float frameCount = animatedSprite.SpriteFrames.GetFrameCount("going_idle");
            double fps = animatedSprite.SpriteFrames.GetAnimationSpeed("going_idle");
            double duration = frameCount / fps;

            await ToSignal(GetTree().CreateTimer(duration), SceneTreeTimer.SignalName.Timeout);
        }

        isGoingIdle = false;
        isIdle = true;
        idleStayTimer = IdleDuration;

        if (animatedSprite.SpriteFrames.HasAnimation("idle"))
        {
            animatedSprite.Play("idle");
        }


    }

    private void HandleIdle(double delta)
    {
        idleStayTimer -= (float)delta;

        if (idleStayTimer <= 0)
        {
            StartWakeUp();
        }
    }

    private async void StartWakeUp()
    {
        if (isWakingUp) return;

        isIdle = false;
        isWakingUp = true;



        if (animatedSprite.SpriteFrames.HasAnimation("idle_wake"))
        {
            animatedSprite.Play("idle_wake");

            float frameCount = animatedSprite.SpriteFrames.GetFrameCount("idle_wake");
            double fps = animatedSprite.SpriteFrames.GetAnimationSpeed("idle_wake");
            double duration = frameCount / fps;

            await ToSignal(GetTree().CreateTimer(duration), SceneTreeTimer.SignalName.Timeout);
        }

        isWakingUp = false;
        idleTimer = IdleWaitTime;  // Timer'ı sıfırla

        animatedSprite.Play("walk");
    }

    // ═══════════════════════════════════════════
    // ✅ PROJECTILE SALDIRISI
    // ═══════════════════════════════════════════

    private async void StartProjectileAttack()
    {
        isProjectileAttacking = true;
        projectilesFired = 0;
        Velocity = Vector2.Zero;


        if (animatedSprite.SpriteFrames.HasAnimation("projectile_attack"))
        {
            animatedSprite.Play("projectile_attack");

            // Frame bazlı projectile atma
            while (animatedSprite.Animation == "projectile_attack")
            {
                int frame = animatedSprite.Frame;

                // İlk projectile: Frame 7-8
                if ((frame == 7 || frame == 8) && projectilesFired == 0)
                {
                    FireProjectile();
                    projectilesFired = 1;
                }
                // İkinci projectile: Frame 12-13
                else if ((frame == 12 || frame == 13) && projectilesFired == 1)
                {
                    FireProjectile();
                    projectilesFired = 2;
                }

                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

                // Animasyon bittiyse çık
                if (!animatedSprite.IsPlaying() || animatedSprite.Animation != "projectile_attack")
                    break;
            }

            // Animasyon tamamen bitene kadar bekle
            if (animatedSprite.Animation == "projectile_attack" && animatedSprite.IsPlaying())
            {
                await ToSignal(animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);
            }
        }

        projectileCooldownTimer = ProjectileCooldown;
        isProjectileAttacking = false;
        animatedSprite.Play("walk");

    }

    private void FireProjectile()
    {
        if (ProjectileScene == null)
        {
            return;
        }

        var projectile = ProjectileScene.Instantiate<Node2D>();

        // Pozisyon: Karakterin önünde
        Vector2 spawnPos = GlobalPosition + new Vector2(direction * 30, -10);
        projectile.GlobalPosition = spawnPos;

        // Projectile ayarları
        if (projectile.HasMethod("Setup"))
        {
            projectile.Call("Setup", direction, ProjectileDamage, false, 0f);
        }

        GetTree().CurrentScene.AddChild(projectile);

    }


    private async void StartMeleeAttack()
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

            if (frame >= 8 && frame <= 10)
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
                body.Call("TakeDamage", 1);
            }
        }
    }

    // ═══════════════════════════════════════════
    // HAREKET VE PLATFORM
    // ═══════════════════════════════════════════

    private void Move(double delta)
    {
        Vector2 velocity = Velocity;

        // Yerçekimi
        if (!IsOnFloor())
        {
            velocity.Y += Gravity * (float)delta;

            // ✅ Havada airborn animasyonu
            if (!isPrepJump && animatedSprite.Animation != "airborn")
            {
                if (animatedSprite.SpriteFrames.HasAnimation("airborn"))
                {
                    animatedSprite.Play("airborn");
                }
            }

            isJumping = true;
        }
        else
        {
            velocity.Y = 0;

            // Yere indiyse walk'a dön
            if (isJumping)
            {
                isJumping = false;
                animatedSprite.Play("walk");
            }
        }

        // Yatay hareket
        velocity.X = direction * Speed;

        animatedSprite.FlipH = direction > 0;

        if (attackShape != null)
        {
            attackShape.Position = new Vector2(direction * 20, 0);
        }

        UpdatePlatformDetectorPosition();

        Velocity = velocity;
        MoveAndSlide();

        CheckDirection();
    }

    private void UpdatePlatformDetectorPosition()
    {
        if (platformDetector == null) return;

        var shape = platformDetector.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (shape != null)
        {
            shape.Position = new Vector2(direction * 80, 50);
        }
    }

    private void CheckDirection()
    {
        if (raycastLeft == null || raycastRight == null) return;

        if (IsOnWall())
        {
            direction *= -1;
            return;
        }

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

            if (cliffAhead)
            {
                if (platformAhead && jumpCooldownTimer <= 0)
                {
                    StartPrepJump();
                }
                else
                {
                    direction *= -1;
                }
            }
        }
    }

    // ✅ ATLAMA HAZIRLIĞI
    private async void StartPrepJump()
    {
        isPrepJump = true;
        Velocity = Vector2.Zero;


        if (animatedSprite.SpriteFrames.HasAnimation("prep_jump"))
        {
            animatedSprite.Play("prep_jump");

            float frameCount = animatedSprite.SpriteFrames.GetFrameCount("prep_jump");
            double fps = animatedSprite.SpriteFrames.GetAnimationSpeed("prep_jump");
            double duration = frameCount / fps;

            await ToSignal(GetTree().CreateTimer(duration), SceneTreeTimer.SignalName.Timeout);
        }

        // Atla!
        JumpToPlatform();
        isPrepJump = false;
    }

    private void JumpToPlatform()
    {
        if (!IsOnFloor()) return;

        Vector2 velocity = Velocity;
        velocity.Y = JumpForce;
        velocity.X = direction * Speed * 1.5f;
        Velocity = velocity;

        isJumping = true;
        jumpCooldownTimer = PlatformJumpCooldown;

    }

    // ═══════════════════════════════════════════
    // HASAR VE ÖLÜM
    // ═══════════════════════════════════════════

    public void ApplyStun(float duration)
    {
        isStunned = true;
        stunTimer = duration;
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

    public void TakeDamage(int damage = 1)
    {
        if (isDead) return;

        currentHealth -= damage;

        // Idle'daysa uyan
        if (isIdle || isGoingIdle)
        {
            isIdle = false;
            isGoingIdle = false;
        }

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
        isProjectileAttacking = false;
        isPrepJump = false;

        Velocity = Vector2.Zero;

        animatedSprite.Play("hurt");

        float frameCount = animatedSprite.SpriteFrames.GetFrameCount("hurt");
        double fps = animatedSprite.SpriteFrames.GetAnimationSpeed("hurt");
        double duration = frameCount / fps;

        await ToSignal(GetTree().CreateTimer(duration), SceneTreeTimer.SignalName.Timeout);

        if (isDead) return;

        isHurt = false;
        idleTimer = IdleWaitTime;  // Hasar alınca idle timer sıfırla
        animatedSprite.Play("walk");
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        isHurt = false;
        isAttacking = false;
        isProjectileAttacking = false;
        SetPhysicsProcess(false);

        var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (collision != null)
            collision.SetDeferred("disabled", true);

        if (attackCollision != null)
            attackCollision.Monitoring = false;

        if (playerDetector != null)
            playerDetector.Monitoring = false;

        if (playerDetectorProjectile != null)
            playerDetectorProjectile.Monitoring = false;

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