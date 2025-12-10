using Godot;
using System.Threading.Tasks;

public partial class TrashMan : CharacterBody2D
{
    // Temel Ayarlar
    [Export] public float Speed = 50.0f;
    [Export] public float Gravity = 980.0f;
    [Export] public int MaxHealth = 3;

    // Saldırı Ayarları
    [Export] public float AttackRange = 50.0f;
    [Export] public float AttackCooldown = 2.0f;

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
    // Node'lar
    private AnimatedSprite2D animatedSprite;
    private Area2D attackCollision;
    private CollisionShape2D attackShape;
    private Area2D playerDetector;
    private RayCast2D raycastLeft;
    private RayCast2D raycastRight;
    private Node2D player;

    public override void _Ready()
    {
        originalSpeed = Speed;  // Orijinal hızı kaydet
        // Node'ları al
        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        attackCollision = GetNode<Area2D>("attack_collision");
        playerDetector = GetNode<Area2D>("player_detector");
        raycastLeft = GetNode<RayCast2D>("RayCast2Dleft");
        raycastRight = GetNode<RayCast2D>("RayCast2Dright");
        AddToGroup("enemy");
        // Attack shape'i al
        attackShape = attackCollision.GetNode<CollisionShape2D>("CollisionShape2D");

        // Başlangıç ayarları
        currentHealth = MaxHealth;
        attackCollision.Monitoring = false; // Başta kapalı

        // Player'ı bul
        var players = GetTree().GetNodesInGroup("player");
        if (players.Count > 0)
            player = players[0] as Node2D;
        playerDetector.CollisionMask = 2;
        // Sinyaller
        attackCollision.BodyEntered += OnAttackHit;
        playerDetector.BodyEntered += OnPlayerEnterRange;
        playerDetector.BodyExited += OnPlayerExitRange;

        // Walk animasyonu başlat
        animatedSprite.Play("walk");
        if (raycastLeft != null)
        {
            raycastLeft.Enabled = true;
            raycastLeft.CollisionMask = 1; // Sadece layer 1
        }

        if (raycastRight != null)
        {
            raycastRight.Enabled = true;
            raycastRight.CollisionMask = 1; // Sadece layer 1
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
    public override void _PhysicsProcess(double delta)
    {
        // Stun kontrolü
        if (isStunned)
        {
            stunTimer -= (float)delta;
            if (stunTimer <= 0)
            {
                isStunned = false;
                Speed = originalSpeed;
            }
            return;  // Stunlıyken hareket etme
        }
        if (isDead)
            return;

        if (isHurt)
            return;
        // Attack timer
        if (attackTimer > 0)
            attackTimer -= (float)delta;

        // Saldırı yapıyorsa hareket etme
        if (isAttacking)
            return;

        // Player yakında mı?
        if (playerInRange && player != null && attackTimer <= 0)
        {
            float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
            direction = player.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
            animatedSprite.FlipH = direction > 0;
            StartAttack();
            return;
        }

        // Normal hareket
        Move(delta);
    }
    // Yeni fonksiyonlar ekle
    public void ApplyStun(float duration)
    {
        isStunned = true;
        stunTimer = duration;
    }

    public void ApplySlow(float slowPercent, float duration)
    {
        Speed = originalSpeed * (1.0f - slowPercent);

        // Süre sonunda normale dön
        GetTree().CreateTimer(duration).Timeout += () =>
        {
            if (!isStunned)  // Stun yoksa normale dön
            {
                Speed = originalSpeed;
            }
        };
    }
    private void Move(double delta)
    {
        Vector2 velocity = Velocity;

        // Yerçekimi
        if (!IsOnFloor())
            velocity.Y += Gravity * (float)delta;
        else
            velocity.Y = 0;

        // Yatay hareket
        velocity.X = direction * Speed;

        // Sprite yönü
        animatedSprite.FlipH = direction > 0;

        // Attack collision yönü
        if (attackShape != null)
        {
            attackShape.Position = new Vector2(direction * 20, 0);
        }

        Velocity = velocity;
        MoveAndSlide();

        // Duvar veya uçurum kontrolü
        CheckDirection();
    }

    private void CheckDirection()
    {
        if (raycastLeft == null || raycastRight == null)
            return;
        // Duvar kontrolü
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
                direction = -1; // Sola dön
            }
            else if (direction < 0 && !raycastLeft.IsColliding())
            {
                direction = 1; // Sağa dön
            }
        }
    }

    private async void StartAttack()
    {
        isAttacking = true;
        Velocity = Vector2.Zero;


        if (attackShape != null)
        {
            attackShape.Position = new Vector2(direction * 20, 0);
        }
        // Attack animasyonu
        animatedSprite.Play("attack");

        // Frame 9-11 bekle
        while (animatedSprite.Animation == "attack")
        {
            int frame = animatedSprite.Frame;

            // Frame 9, 10 veya 11 mi?
            if (frame >= 9 && frame <= 11)
            {
                attackCollision.Monitoring = true;
                await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
                attackCollision.Monitoring = false;
                break;
            }

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        // Cooldown ve reset
        attackTimer = AttackCooldown;
        isAttacking = false;
        animatedSprite.Play("walk");
    }

    private void OnPlayerDetected(Node2D body)
    {
        // Player detector'a girdi (opsiyonel)
        if (body.IsInGroup("player"))
        {
            GD.Print("Player algılandı!");
        }
    }

    private void OnAttackHit(Node2D body)
    {
        // Player'a hasar ver
        if (body.IsInGroup("player"))
        {
            if (body.HasMethod("TakeDamage"))
            {
                body.Call("TakeDamage", 1);
            }
        }
    }

    public void TakeDamage(int damage = 1)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        // Ölüm kontrolü
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // ✅ Hurt animasyonu oynat
        PlayHurt();
    }
    private async void PlayHurt()
    {
        if (isDead) return;

        isHurt = true;
        isAttacking = false;
        Velocity = Vector2.Zero;

        // ✅ Timer ile 2 kez hurt animasyonu
        for (int i = 0; i < 2; i++)
        {
            if (isDead) return;

            animatedSprite.Play("hurt");

            // Animasyon süresi kadar bekle (timer ile)
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
        if (isDead)
            return;

        isDead = true;
        isHurt = false;
        isAttacking = false;
        SetPhysicsProcess(false);

        var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (collision != null)
            collision.SetDeferred("disabled", true);

        if (attackCollision != null)
            attackCollision.Monitoring = false;

        if (playerDetector != null)
            playerDetector.Monitoring = false;

        if (animatedSprite.SpriteFrames.HasAnimation("death"))
        {
            animatedSprite.Play("death");

            // ✅ Timer ile QueueFree - sinyal kullanma
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