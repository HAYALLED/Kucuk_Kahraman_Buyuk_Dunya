using Godot;
using System;

public partial class Trash_Bird_Spawner_Hermit : CharacterBody2D
{
    // Temel Ayarlar
    [Export] public float Speed = 50.0f;
    [Export] public float Gravity = 980.0f;
    [Export] public int MaxHealth = 5;
    [Export] public float SpawnInterval = 2.0f;  // Kaç saniyede bir spawn
    [Export] public int MaxBirds = 30;            // Maksimum kuş sayısı
    private PackedScene BirdScene;

    // Değişkenler
    private int currentHealth;
    private int direction = 1;
    private bool isDead = false;
    private float spawnTimer = 0;
    private int currentBirdCount = 0;
    private bool isCovering = false;      // Kapanma durumu
    private bool isCovered = false;       // Tamamen kapalı mı
    [Export] public float CoverDistance = 100.0f;  // Bu mesafede kapanır
    [Export] public float UncoverDistance = 200.0f; // Bu mesafede açılır
    private Path2D birdPath;  // Değişkenler kısmına ekle
    private bool isStunned = false;
    private float stunTimer = 0;
    private float originalSpeed;
    // Node'lar
    private AnimatedSprite2D animatedSprite;
    private Area2D playerDetector;
    private RayCast2D raycastLeft;
    private RayCast2D raycastRight;
    private Node2D player;



    public override void _Ready()
    {
        originalSpeed = Speed;  // Orijinal hızı kaydet
        // Node'ları al
        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        playerDetector = GetNode<Area2D>("player_detector");
        raycastLeft = GetNode<RayCast2D>("RayCast2Dleft");
        raycastRight = GetNode<RayCast2D>("RayCast2Dright");


        AddToGroup("enemy");
        currentHealth = MaxHealth;

        // Player'ı bul
        var players = GetTree().GetNodesInGroup("player");
        if (players.Count > 0)
            player = players[0] as Node2D;
        playerDetector.CollisionMask = 2;
        // Sinyaller
        playerDetector.BodyEntered += OnPlayerEnterRange;
        playerDetector.BodyExited += OnPlayerExitRange;
        animatedSprite.Play("idle");

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

        BirdScene = GD.Load<PackedScene>("res://Assets/Scenes/Trash_Bird.tscn");



        // ✅ Path2D'yi bul (Level'de olmalı)
        birdPath = GetParent().GetNodeOrNull<Path2D>("Path2D_rapid");


        spawnTimer = 2.0f;
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

        // Player mesafe kontrolü
        float distanceToPlayer = float.MaxValue;
        if (player != null)
        {
            distanceToPlayer = GlobalPosition.DistanceTo(player.GlobalPosition);
        }

        // Kapanma/Açılma kontrolü
        if (!isCovered && !isCovering && distanceToPlayer <= CoverDistance)
        {
            // Player çok yakın - kapan!
            StartCovering();
            return;
        }
        else if (isCovered && distanceToPlayer > UncoverDistance)
        {
            // Player uzaklaştı - aç!
            StartUncovering();
            return;
        }

        // Kapalıysa hiçbir şey yapma
        if (isCovering || isCovered)
            return;

        // Spawn timer (sadece açıkken)
        spawnTimer -= (float)delta;
        if (spawnTimer <= 0 && currentBirdCount < MaxBirds)
        {
            SpawnBird();
            spawnTimer = SpawnInterval;
        }

        // Normal hareket
        Move(delta);
    }
    private async void StartCovering()
    {
        isCovering = true;
        Velocity = Vector2.Zero;


        // Covering animasyonu
        animatedSprite.Play("covering");

        // Animasyon bitene kadar bekle
        float frameCount = animatedSprite.SpriteFrames.GetFrameCount("covering");
        double fps = animatedSprite.SpriteFrames.GetAnimationSpeed("covering");
        double duration = frameCount / fps;

        await ToSignal(GetTree().CreateTimer(duration), SceneTreeTimer.SignalName.Timeout);

        // Tamamen kapalı duruma geç
        isCovering = false;
        isCovered = true;
        animatedSprite.Play("cover_idle");

    }

    private async void StartUncovering()
    {
        isCovered = false;
        isCovering = true;  // Açılma sırasında da "covering" flag'i kullan


        // Covering animasyonunu tersine oynat (veya ayrı bir animasyon varsa onu kullan)
        animatedSprite.Play("covering");
        animatedSprite.SpeedScale = -1;  // Tersine oynat
        animatedSprite.Frame = animatedSprite.SpriteFrames.GetFrameCount("covering") - 1;

        float frameCount = animatedSprite.SpriteFrames.GetFrameCount("covering");
        double fps = animatedSprite.SpriteFrames.GetAnimationSpeed("covering");
        double duration = frameCount / fps;

        await ToSignal(GetTree().CreateTimer(duration), SceneTreeTimer.SignalName.Timeout);

        // Normale dön
        animatedSprite.SpeedScale = 1;
        isCovering = false;
        animatedSprite.Play("walk");

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
        // Kapalıyken hareket etme
        if (isCovering || isCovered)
            return;

        Vector2 velocity = Velocity;

        if (!IsOnFloor())
            velocity.Y += Gravity * (float)delta;
        else
            velocity.Y = 0;

        velocity.X = direction * Speed;

        animatedSprite.FlipH = direction > 0;

        Velocity = velocity;
        MoveAndSlide();

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
    private void SpawnBird()
    {


        // ✅ PathFollow2D oluştur
        var pathFollow = new PathFollow2D();
        pathFollow.Rotates = false;  // Kuş kendi rotasyonunu yönetsin
        pathFollow.Loop = true;      // Yolun sonunda başa dönsün

        // Path2D'ye ekle
        birdPath.AddChild(pathFollow);

        // Kuşu oluştur ve PathFollow2D'ye ekle
        var bird = BirdScene.Instantiate<Node2D>();
        pathFollow.AddChild(bird);

        currentBirdCount++;

        // Kuş öldüğünde sayıyı azalt
        bird.TreeExited += () => OnBirdDied();

    }

    private void OnBirdDied()
    {
        currentBirdCount--;
    }

    public void TakeDamage(int damage = 1)
    {
        if (isDead)
            return;

        // ✅ Kapalıyken hasar almaz
        if (isCovered || isCovering)
        {
            return;
        }

        currentHealth -= damage;

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

        // Collision kapat
        var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (collision != null)
            collision.SetDeferred("disabled", true);

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
