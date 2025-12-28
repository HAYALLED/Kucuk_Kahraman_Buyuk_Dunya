using Godot;
using System;

public partial class TrashMinibossSecurity : CharacterBody2D
{
    // === STATE MACHINE ===
    public enum SecurityState
    {
        Idle,
        Walk,
        Alerted,
        BattleIdle,
        BattleWalk,
        Attack,
        Hurt,
        StrikeBack,
        Death
    }

    private SecurityState currentState = SecurityState.Idle;

    // === TEMEL AYARLAR ===
    [Export] public float Speed = 50.0f;
    [Export] public float Gravity = 980.0f;
    [Export] public int MaxHealth = 10;

    // === SALDIRI AYARLARI ===
    [Export] public float AttackRange = 150.0f;
    [Export] public float AttackCooldown = 1.5f;

    // === IDLE/WALK AYARLARI ===
    [Export] public float IdleDuration = 15.0f;      // Idle'da kalma süresi
    [Export] public float PatrolDuration = 5.0f;     // Yürüme süresi

    // === KALKAN AYARLARI ===
    [Export] public int ShieldThreshold = 3;         // Kaç hasarda kalkan düşer

    // === DEĞİŞKENLER ===
    private int currentHealth;
    private int direction = 1;
    private float stateTimer = 0;
    private float attackTimer = 0;
    private float originalSpeed;

    // Kalkan sistemi
    private int battleDamageCount = 0;               // Battle'da alınan hasar sayısı
    private bool canTakeDamageBeforeAlert = true;    // Alert öncesi 1 hasar hakkı
    private bool isInBattleMode = false;
    private bool shieldBroken = false;               // Kalkan kırıldı mı (hurt'ta hasar alabilir)

    // Stun sistemi
    private bool isStunned = false;
    private float stunTimer = 0;

    // === NODE'LAR ===
    private AnimatedSprite2D animatedSprite;
    private Area2D attackCollision;
    private CollisionShape2D attackShape;
    private Area2D playerDetector;
    private RayCast2D raycastLeft;
    private RayCast2D raycastRight;
    private Node2D player;
    private bool playerInRange = false;

    // Attack frame tracking
    private bool firstHitDone = false;
    private bool secondHitDone = false;

    public override void _Ready()
    {
        originalSpeed = Speed;
        currentHealth = MaxHealth;

        // Node'ları al
        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        attackCollision = GetNode<Area2D>("attack_collision");
        playerDetector = GetNode<Area2D>("player_detector");
        raycastLeft = GetNode<RayCast2D>("RayCast2Dleft");
        raycastRight = GetNode<RayCast2D>("RayCast2Dright");

        attackShape = attackCollision.GetNode<CollisionShape2D>("CollisionShape2D");

        AddToGroup("enemy");

        // Başlangıç ayarları
        attackCollision.Monitoring = false;
        playerDetector.CollisionMask = 2;

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
        playerDetector.BodyEntered += OnPlayerEnterRange;
        playerDetector.BodyExited += OnPlayerExitRange;
        animatedSprite.FrameChanged += OnFrameChanged;

        // ÖNEMLİ: AnimationFinished sinyalini düzgün bağla
        animatedSprite.AnimationFinished += OnAnimationFinished;


        // Başlangıç state
        ChangeState(SecurityState.Idle);
    }

    // === PLAYER ALGILAMA ===
    private void OnPlayerEnterRange(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInRange = true;
            player = body;

            // Idle veya Walk'taysa battle'a geç
            if (!isInBattleMode && currentState != SecurityState.Death)
            {
                TriggerAlert();
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

    // === ANİMASYON BİTTİ ===
    private void OnAnimationFinished()
    {
        string anim = animatedSprite.Animation;

        switch (currentState)
        {
            case SecurityState.Alerted:
                if (anim == "alerted")
                {
                    ChangeState(SecurityState.BattleIdle);
                }
                break;

            case SecurityState.Attack:
                if (anim == "attack")
                {
                    attackTimer = AttackCooldown;
                    DecideBattleAction();
                }
                break;

            case SecurityState.Hurt:
                if (anim == "hurt")
                {
                    ChangeState(SecurityState.StrikeBack);
                }
                break;

            case SecurityState.StrikeBack:
                if (anim == "strike_back")
                {
                    shieldBroken = false;
                    DecideBattleAction();
                }
                break;

            case SecurityState.Death:
                if (anim == "death")
                {
                    QueueFree();
                }
                break;
        }
    }

    // === PROJECTILE DOKUNMA (BaseProjectile'dan çağrılacak) ===
    public void OnProjectileHit()
    {
        if (!isInBattleMode && currentState != SecurityState.Death)
        {
            TriggerAlert();
        }
    }

    // === ALERT TETİKLEME ===
    private void TriggerAlert()
    {
        if (currentState == SecurityState.Alerted || isInBattleMode)
        {
            return;
        }

        ChangeState(SecurityState.Alerted);
    }

    // === STATE DEĞİŞTİRME ===
    private void ChangeState(SecurityState newState)
    {
        // Önceki state'ten çık
        ExitState(currentState);

        SecurityState oldState = currentState;
        currentState = newState;
        stateTimer = 0;


        // Yeni state'e gir
        EnterState(newState);
    }

    private void ExitState(SecurityState state)
    {
        switch (state)
        {
            case SecurityState.Attack:
            case SecurityState.StrikeBack:
                attackCollision.Monitoring = false;
                firstHitDone = false;
                secondHitDone = false;
                break;
        }
    }

    private void EnterState(SecurityState state)
    {
        switch (state)
        {
            case SecurityState.Idle:
                Velocity = Vector2.Zero;
                animatedSprite.Play("idle");
                stateTimer = IdleDuration;
                break;

            case SecurityState.Walk:
                animatedSprite.Play("walk");
                stateTimer = PatrolDuration;
                break;

            case SecurityState.Alerted:
                Velocity = Vector2.Zero;
                animatedSprite.Play("alerted");
                break;

            case SecurityState.BattleIdle:
                isInBattleMode = true;
                Velocity = Vector2.Zero;
                animatedSprite.Play("idle");
                break;

            case SecurityState.BattleWalk:
                isInBattleMode = true;
                animatedSprite.Play("walk");
                break;

            case SecurityState.Attack:
                Velocity = Vector2.Zero;
                animatedSprite.Play("attack");
                firstHitDone = false;
                secondHitDone = false;
                FacePlayer();
                break;

            case SecurityState.Hurt:
                Velocity = Vector2.Zero;
                animatedSprite.Play("hurt");
                shieldBroken = true;  // Artık hasar alabilir
                battleDamageCount = 0; // Sayacı sıfırla
                break;

            case SecurityState.StrikeBack:
                Velocity = Vector2.Zero;
                animatedSprite.Play("strike_back");
                FacePlayer();
                break;

            case SecurityState.Death:
                Velocity = Vector2.Zero;
                animatedSprite.Play("death");
                SetPhysicsProcess(false);
                DisableCollisions();
                break;
        }
    }

    // === PHYSICS PROCESS ===
    public override void _PhysicsProcess(double delta)
    {
        // Stun kontrolü
        if (isStunned)
        {
            stunTimer -= (float)delta;
            if (stunTimer <= 0)
            {
                isStunned = false;
            }
            return;
        }

        if (currentState == SecurityState.Death)
            return;

        // Yerçekimi
        ApplyGravity(delta);

        // Attack cooldown
        if (attackTimer > 0)
            attackTimer -= (float)delta;

        // State güncelle
        UpdateState(delta);

        MoveAndSlide();
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

    private void UpdateState(double delta)
    {
        switch (currentState)
        {
            case SecurityState.Idle:
                UpdateIdle(delta);
                break;

            case SecurityState.Walk:
                UpdateWalk(delta);
                break;

            case SecurityState.Alerted:
                // AnimationFinished sinyali halledecek
                break;

            case SecurityState.BattleIdle:
            case SecurityState.BattleWalk:
                UpdateBattle(delta);
                break;

            case SecurityState.Attack:
                // AnimationFinished sinyali halledecek
                break;

            case SecurityState.Hurt:
                // AnimationFinished sinyali halledecek
                break;

            case SecurityState.StrikeBack:
                // AnimationFinished sinyali halledecek
                break;
        }
    }

    // === IDLE STATE ===
    private void UpdateIdle(double delta)
    {
        stateTimer -= (float)delta;

        if (stateTimer <= 0)
        {
            ChangeState(SecurityState.Walk);
        }
    }

    // === WALK STATE (Patrol) ===
    private void UpdateWalk(double delta)
    {
        stateTimer -= (float)delta;

        // Hareket
        Vector2 velocity = Velocity;
        velocity.X = direction * Speed;
        Velocity = velocity;

        // Sprite yönü
        animatedSprite.FlipH = direction > 0;

        // Duvar/uçurum kontrolü
        CheckDirection();

        // Süre doldu mu?
        if (stateTimer <= 0)
        {
            ChangeState(SecurityState.Idle);
        }
    }

    // === BATTLE STATE ===
    private void UpdateBattle(double delta)
    {
        if (player == null || !playerInRange)
        {
            // Player uzaktaysa idle'da bekle
            if (currentState != SecurityState.BattleIdle)
            {
                Velocity = new Vector2(0, Velocity.Y);
                animatedSprite.Play("idle");
                currentState = SecurityState.BattleIdle;
            }
            return;
        }

        float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
        FacePlayer();

        // Saldırı menzilinde mi?
        if (distance <= AttackRange && attackTimer <= 0)
        {
            ChangeState(SecurityState.Attack);
        }
        else if (distance > AttackRange)
        {
            // Player'a doğru yürü
            if (currentState != SecurityState.BattleWalk)
            {
                currentState = SecurityState.BattleWalk;
                animatedSprite.Play("walk");
            }

            Vector2 velocity = Velocity;
            velocity.X = direction * Speed;
            Velocity = velocity;

            CheckDirection();
        }
    }

    private void DecideBattleAction()
    {
        if (player == null || !playerInRange)
        {
            ChangeState(SecurityState.BattleIdle);
            return;
        }

        float distance = GlobalPosition.DistanceTo(player.GlobalPosition);

        if (distance <= AttackRange && attackTimer <= 0)
        {
            ChangeState(SecurityState.Attack);
        }
        else
        {
            ChangeState(SecurityState.BattleWalk);
        }
    }

    // === FRAME CHANGED (Attack hit detection) ===
    private void OnFrameChanged()
    {
        int frame = animatedSprite.Frame;
        string anim = animatedSprite.Animation;

        // Attack: Frame 6-8 (ilk hit), Frame 10-12 (ikinci hit)
        if (anim == "attack")
        {
            if (frame >= 6 && frame <= 8 && !firstHitDone)
            {
                firstHitDone = true;
                DoAttackHit();
            }
            else if (frame >= 10 && frame <= 12 && !secondHitDone)
            {
                secondHitDone = true;
                DoAttackHit();
            }
        }
        // Strike_back: Frame 11-13
        else if (anim == "strike_back")
        {
            if (frame >= 11 && frame <= 13 && !firstHitDone)
            {
                firstHitDone = true;
                DoAttackHit();
            }
        }
    }

    private void DoAttackHit()
    {
        // Attack collision pozisyonunu ayarla
        if (attackShape != null)
        {
            attackShape.Position = new Vector2(direction * 20, 0);
        }

        // Kısa süreliğine collision aç
        attackCollision.Monitoring = true;

        GetTree().CreateTimer(0.1).Timeout += () =>
        {
            if (IsInstanceValid(this))
                attackCollision.Monitoring = false;
        };
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

    // === HASAR ALMA ===
    public void TakeDamage(int damage = 1)
    {
        if (currentState == SecurityState.Death)
        {
            return;
        }

        // ALERTED sırasında hasar ALAMAZ
        if (currentState == SecurityState.Alerted)
        {
            return;
        }

        // Alert öncesi sadece 1 hasar alabilir
        if (!isInBattleMode)
        {
            if (canTakeDamageBeforeAlert)
            {
                canTakeDamageBeforeAlert = false;
                currentHealth -= damage;

                // Hasar alınca alert tetikle
                TriggerAlert();
            }

            return;
        }

        // Battle mode'da
        if (isInBattleMode)
        {
            // Kalkan kırıksa (hurt state) gerçek hasar
            if (shieldBroken)
            {
                currentHealth -= damage;

                if (currentHealth <= 0)
                {
                    ChangeState(SecurityState.Death);
                }
                return;
            }

            // Kalkan sağlamsa hasar say
            battleDamageCount += damage;

            // Kalkan kırıldı mı?
            if (battleDamageCount >= ShieldThreshold)
            {
                ChangeState(SecurityState.Hurt);
            }
        }
    }

    // === YARDIMCI FONKSİYONLAR ===
    private void FacePlayer()
    {
        if (player != null)
        {
            direction = player.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
            animatedSprite.FlipH = direction > 0;
        }
    }

    private void CheckDirection()
    {
        if (raycastLeft == null || raycastRight == null)
            return;

        if (IsOnWall())
        {
            direction *= -1;
            return;
        }

        if (IsOnFloor())
        {
            if (direction > 0 && !raycastRight.IsColliding())
                direction = -1;
            else if (direction < 0 && !raycastLeft.IsColliding())
                direction = 1;
        }
    }

    private void DisableCollisions()
    {
        var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (collision != null)
            collision.SetDeferred("disabled", true);

        if (attackCollision != null)
            attackCollision.Monitoring = false;

        if (playerDetector != null)
            playerDetector.Monitoring = false;
    }

    // === STUN/SLOW ===
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
            {
                Speed = originalSpeed;
            }
        };
    }
}