using Godot;
using System;

public partial class TrashBird : CharacterBody2D
{
    [Export] public float Speed = 300.0f;
    [Export] public float JumpVelocity = -400.0f;
    [Export] public int MaxHealth = 1;


    // Değişkenler
    private int currentHealth;
    private int direction = 1;
    private bool isDead = false;
    private bool isAttacking = false;
    private float attackTimer = 0;
    [Export] public float DiveSpeed = 400.0f;
    private bool isDiving = false;
    private Vector2 originalPosition;  // Başlangıç pozisyonu
    private bool isReturning = false;  // Geri dönüş durumu
    // Path takibi için
    private bool isStunned = false;
    private float stunTimer = 0;
    private float originalSpeed;
    private PathFollow2D pathFollow;
    private bool isOnPath = true;
    [Export] public float PathSpeed = 100.0f;  // Path üzerindeki hız


    // Node'lar
    private AnimatedSprite2D animatedSprite;
    private Area2D attackCollision;
    private CollisionShape2D attackShape;
    private Area2D playerDetector;
    private Node2D player;
    private RayCast2D raycastLeft;
    private RayCast2D raycastRight;
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
        // ✅ EKLE: Monitoring'leri kontrol et ve aç
        playerDetector.Monitoring = true;
        originalPosition = GlobalPosition;
        // Walk animasyonu başlat
        animatedSprite.Play("fly");
        if (GetParent() is PathFollow2D pf)
        {
            pathFollow = pf;
            isOnPath = true;
        }
        else
        {
            isOnPath = false;
        }

        originalPosition = GlobalPosition;
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

        if (body.IsInGroup("player") && !isDiving && !isDead)
        {
            player = body;
            StartDive();
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

        if (isDiving)
        {
            DiveMove(delta);
        }
        else if (isReturning)
        {
            ReturnMove(delta);
        }
        else if (isOnPath && pathFollow != null)
        {
            // ✅ Path üzerinde hareket
            PathMove(delta);
        }
        else
        {
            Move(delta);
        }
    }
    private void PathMove(double delta)
    {
        // Path üzerinde ilerle
        pathFollow.Progress += PathSpeed * (float)delta;

        // Sprite yönü (hareket yönüne göre)
        if (pathFollow.Progress > 0)
        {
            animatedSprite.FlipH = Velocity.X > 0;
        }
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
    }

    private void StartDive()
    {
        isDiving = true;
        isOnPath = false;  // ✅ Path'ten çık

        // ✅ PathFollow2D'den çıkar, Level'e taşı
        if (pathFollow != null)
        {
            var currentPos = GlobalPosition;
            this.Reparent(GetTree().CurrentScene);
            GlobalPosition = currentPos;
            pathFollow = null;
        }

        direction = player.GlobalPosition.X > GlobalPosition.X ? 1 : -1;
        animatedSprite.FlipH = direction > 0;

        animatedSprite.Play("dive");

        attackCollision.Monitoring = true;

        if (attackShape != null)
        {
            attackShape.Position = new Vector2(direction * 20, 0);
        }

        animatedSprite.Rotation = direction * Mathf.DegToRad(15);


        GetTree().CreateTimer(10.0).Timeout += OnDiveTimeout;
    }

    // ✅ 10 saniye sonra geri dön
    private void OnDiveTimeout()
    {
        // Ölmediyse ve hala dalışta ise geri dön
        if (!isDead && isDiving)
        {
            StartReturn();
        }
    }
    private void DiveMove(double delta)
    {
        Vector2 velocity = Velocity;

        // 15 derece açı ile dalış (tan(15°) ≈ 0.27)
        velocity.X = direction * DiveSpeed;
        velocity.Y = Mathf.Abs(velocity.X) * 0.27f;

        Velocity = velocity;
        MoveAndSlide();

        // Yere çarptıysa öl
        if (IsOnFloor())
        {
            Die();
        }
    }
    private void StartReturn()
    {
        isDiving = false;
        isReturning = true;

        attackCollision.Monitoring = false;
        animatedSprite.Rotation = 0;
        animatedSprite.Play("fly");

    }

    private void ReturnMove(double delta)
    {
        Vector2 velocity = Velocity;

        // Yukarı ve başlangıç noktasına doğru git
        Vector2 directionToOrigin = (originalPosition - GlobalPosition).Normalized();

        velocity.X = directionToOrigin.X * Speed;
        velocity.Y = directionToOrigin.Y * Speed;

        // Sprite yönü
        animatedSprite.FlipH = velocity.X > 0;

        Velocity = velocity;
        MoveAndSlide();

        // Başlangıç noktasına yaklaştıysa normal moda dön
        if (GlobalPosition.DistanceTo(originalPosition) < 20)
        {
            isReturning = false;
            GlobalPosition = originalPosition;
            direction = 1;
        }
    }


    private void OnAttackHit(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            if (body.HasMethod("TakeDamage"))
            {
                body.Call("TakeDamage", 1);
            }

            // Saldırı sonrası öl
            Die();
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
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
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
            animatedSprite.Rotation = 0;
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
