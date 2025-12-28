using Godot;
using System;

public partial class TrashWizard : CharacterBody2D
{
    // === TEMEL AYARLAR ===
    [Export] public float Speed = 50.0f;
    [Export] public float Gravity = 980.0f;
    [Export] public int MaxHealth = 10;

    // === SALDIRI AYARLARI ===
    [Export] public float AttackRange = 60.0f;
    [Export] public float AttackCooldown = 2.0f;

    // === PROJECTILE AYARLARI ===
    [Export] public PackedScene ProjectileScene;
    [Export] public float ProjectileCooldown = 3.0f;
    [Export] public int ProjectileDamage = 1;

    // === SUMMON AYARLARI ===
    [Export] public PackedScene TrashManScene;
    [Export] public float SummonCooldown = 10.0f;
    [Export] public int MaxSummons = 3;

    // === DEĞİŞKENLER ===
    private int currentHealth;
    private int direction = 1;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool isProjectileAttacking = false;
    private bool isSummoning = false;
    private float attackTimer = 0;
    private float projectileCooldownTimer = 0;
    private float summonCooldownTimer = 0;
    private int currentSummonCount = 0;

    private bool isHurt = false;
    private bool isStunned = false;
    private float stunTimer = 0;
    private float originalSpeed;

    // === PLAYER TESPİT ===
    private bool playerInMeleeRange = false;
    private bool playerInProjectileRange = false;

    // === NODE'LAR ===
    private AnimatedSprite2D animatedSprite;
    private Area2D attackCollision;
    private CollisionShape2D attackShape;
    private Area2D playerDetector;
    private Area2D playerDetectorProjectile;
    private RayCast2D raycastLeft;
    private RayCast2D raycastRight;
    private Node2D player;

    public override void _Ready()
    {
        originalSpeed = Speed;
        currentHealth = MaxHealth;

        // Node'ları al
        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        attackCollision = GetNode<Area2D>("attack_collision");
        playerDetector = GetNode<Area2D>("player_detector");
        playerDetectorProjectile = GetNode<Area2D>("player_detector_projectile");
        raycastLeft = GetNode<RayCast2D>("RayCast2Dleft");
        raycastRight = GetNode<RayCast2D>("RayCast2Dright");

        attackShape = attackCollision.GetNode<CollisionShape2D>("CollisionShape2D");

        AddToGroup("enemy");

        // Başlangıç ayarları
        attackCollision.Monitoring = false;
        playerDetector.CollisionMask = 2;
        playerDetectorProjectile.CollisionMask = 2;

        // Player'ı bul
        var players = GetTree().GetNodesInGroup("player");
        if (players.Count > 0)
            player = players[0] as Node2D;

        // Raycast ayarları
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

        // Sinyaller
        attackCollision.BodyEntered += OnAttackHit;
        playerDetector.BodyEntered += OnPlayerEnterMeleeRange;
        playerDetector.BodyExited += OnPlayerExitMeleeRange;
        playerDetectorProjectile.BodyEntered += OnPlayerEnterProjectileRange;
        playerDetectorProjectile.BodyExited += OnPlayerExitProjectileRange;

        animatedSprite.Play("idle");
    }

    // === PLAYER TESPİT SİNYALLERİ ===
    private void OnPlayerEnterMeleeRange(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInMeleeRange = true;
            player = body;
        }
    }

    private void OnPlayerExitMeleeRange(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInMeleeRange = false;
        }
    }

    private void OnPlayerEnterProjectileRange(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInProjectileRange = true;
            player = body;
        }
    }

    private void OnPlayerExitProjectileRange(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInProjectileRange = false;
        }
    }

    // === PHYSICS PROCESS ===
    public override void _PhysicsProcess(double delta)
    {
        // Cooldown'ları azalt
        if (projectileCooldownTimer > 0)
            projectileCooldownTimer -= (float)delta;
        if (attackTimer > 0)
            attackTimer -= (float)delta;
        if (summonCooldownTimer > 0)
            summonCooldownTimer -= (float)delta;

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

        // Yerçekimi
        ApplyGravity(delta);

        // Saldırı/summon yapıyorsa SADECE yerçekimini uygula
        if (isAttacking || isProjectileAttacking || isSummoning)
        {
            Velocity = new Vector2(0, Velocity.Y);
            MoveAndSlide();
            return;
        }

        // === SALDIRI ÖNCELİKLERİ ===

        // 1. Yakın menzilde → Melee Attack
        if (playerInMeleeRange && player != null && attackTimer <= 0)
        {
            FacePlayer();
            StartMeleeAttack();
            return;
        }

        // 2. Uzak menzilde (ama yakında değil) → Projectile Attack
        if (playerInProjectileRange && !playerInMeleeRange && player != null && projectileCooldownTimer <= 0)
        {
            FacePlayer();
            StartProjectileAttack();
            return;
        }

        // 3. Player yok → Summon (TrashMan çağır)
        if (!playerInProjectileRange && !playerInMeleeRange && summonCooldownTimer <= 0)
        {
            if (currentSummonCount < MaxSummons)
            {
                StartSummon();
                return;
            }
        }

        // Normal hareket (idle veya yavaş patrol)
        Move(delta);
    }

    private void ApplyGravity(double delta)
    {
        Vector2 velocity = Velocity;
        if (!IsOnFloor())
            velocity.Y += Gravity * (float)delta;
        else
            velocity.Y = 0;
        Velocity = velocity;
    }

    // ═══════════════════════════════════════════
    // SUMMON SİSTEMİ
    // ═══════════════════════════════════════════

    private async void StartSummon()
    {


        isSummoning = true;
        Velocity = Vector2.Zero;


        if (animatedSprite.SpriteFrames.HasAnimation("summoning"))
        {
            animatedSprite.Play("summoning");
            await ToSignal(animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);
        }

        // TrashMan spawn et
        SpawnTrashMan();

        summonCooldownTimer = SummonCooldown;
        isSummoning = false;
        animatedSprite.Play("idle");

    }

    private void SpawnTrashMan()
    {
        var trashMan = TrashManScene.Instantiate<Node2D>();

        // Spawn pozisyonu: Wizard'ın yanında
        Vector2 spawnPos = GlobalPosition + new Vector2(direction * 50, 0);
        trashMan.GlobalPosition = spawnPos;

        GetTree().CurrentScene.AddChild(trashMan);
        currentSummonCount++;

        // TrashMan öldüğünde sayacı azalt
        trashMan.TreeExited += () =>
        {
            currentSummonCount = Mathf.Max(0, currentSummonCount - 1);
        };

    }

    // ═══════════════════════════════════════════
    // PROJECTILE SALDIRISI (Havaya atıp düşürme)
    // ═══════════════════════════════════════════

    private async void StartProjectileAttack()
    {
        isProjectileAttacking = true;
        Velocity = Vector2.Zero;


        if (animatedSprite.SpriteFrames.HasAnimation("projectile_attack"))
        {
            animatedSprite.Play("projectile_attack");

            bool projectileFired = false;

            // Frame 6-8'de projectile at
            while (animatedSprite.Animation == "projectile_attack" && animatedSprite.IsPlaying())
            {
                int frame = animatedSprite.Frame;

                if (frame >= 6 && frame <= 8 && !projectileFired)
                {
                    FireArcProjectile();
                    projectileFired = true;
                }

                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            // Animasyon bitene kadar bekle
            if (animatedSprite.Animation == "projectile_attack" && animatedSprite.IsPlaying())
            {
                await ToSignal(animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);
            }
        }

        projectileCooldownTimer = ProjectileCooldown;
        isProjectileAttacking = false;
        animatedSprite.Play("idle");

    }

    private void FireArcProjectile()
    {


        var projectileNode = ProjectileScene.Instantiate<Area2D>();

        // Spawn pozisyonu: Wizard'ın üstünde
        Vector2 spawnPos = GlobalPosition + new Vector2(direction * 20, -30);
        projectileNode.GlobalPosition = spawnPos;

        // Hedef pozisyon: Player'ın bulunduğu yer
        Vector2 targetPos = player.GlobalPosition;


        // Scene'e ekle
        GetTree().CurrentScene.AddChild(projectileNode);

        // SetupArc çağır (CallDeferred ile)
        projectileNode.CallDeferred("SetupArc", targetPos, ProjectileDamage);
    }


    private async void StartMeleeAttack()
    {
        isAttacking = true;
        Velocity = Vector2.Zero;

        if (attackShape != null)
        {
            attackShape.Position = new Vector2(direction * 25, 0);
        }


        animatedSprite.Play("attack");

        // Frame bazlı hasar
        bool hitApplied = false;
        while (animatedSprite.Animation == "attack" && animatedSprite.IsPlaying())
        {
            int frame = animatedSprite.Frame;

            if (frame >= 8 && frame <= 10 && !hitApplied)
            {
                attackCollision.Monitoring = true;
                await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
                attackCollision.Monitoring = false;
                hitApplied = true;
            }

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        // Animasyon bitene kadar bekle
        if (animatedSprite.Animation == "attack" && animatedSprite.IsPlaying())
        {
            await ToSignal(animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);
        }

        attackTimer = AttackCooldown;
        isAttacking = false;
        animatedSprite.Play("idle");

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
    // HAREKET
    // ═══════════════════════════════════════════

    private void Move(double delta)
    {
        Vector2 velocity = Velocity;

        // Yavaş hareket (patrol)
        velocity.X = direction * Speed * 0.3f;

        Velocity = velocity;
        MoveAndSlide();

        // Sprite yönü
        animatedSprite.FlipH = direction > 0;

        // Walk animasyonu
        if (animatedSprite.Animation != "walk")
        {
            if (animatedSprite.SpriteFrames.HasAnimation("walk"))
            {
                animatedSprite.Play("walk");
            }
            else
            {
                animatedSprite.Play("idle");
            }
        }

        CheckDirection();
    }

    private void CheckDirection()
    {
        if (raycastLeft == null || raycastRight == null) return;

        // Duvara çarptıysa dön
        if (IsOnWall())
        {
            direction *= -1;
            return;
        }

        // Uçurum kontrolü
        if (IsOnFloor())
        {
            if (direction > 0 && !raycastRight.IsColliding())
            {
                direction = -1;
            }
            else if (direction < 0 && !raycastLeft.IsColliding())
            {
                direction = 1;
            }
        }
    }

    private void FacePlayer()
    {
        if (player != null)
        {
            direction = player.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
            animatedSprite.FlipH = direction > 0;
        }
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
        {
            if (!isStunned && IsInstanceValid(this))
                Speed = originalSpeed;
        };
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
        isProjectileAttacking = false;
        isSummoning = false;
        Velocity = Vector2.Zero;

        animatedSprite.Play("hurt");

        float frameCount = animatedSprite.SpriteFrames.GetFrameCount("hurt");
        double fps = animatedSprite.SpriteFrames.GetAnimationSpeed("hurt");
        double duration = frameCount / fps;

        await ToSignal(GetTree().CreateTimer(duration), SceneTreeTimer.SignalName.Timeout);

        if (isDead) return;

        isHurt = false;
        animatedSprite.Play("idle");
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        isHurt = false;
        isAttacking = false;
        isProjectileAttacking = false;
        isSummoning = false;
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