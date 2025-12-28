using Godot;
using System;

public partial class TrashBirfSpawner_secured : CharacterBody2D
{
    // Temel Ayarlar
    [Export] public int MaxHealth = 8;
    [Export] public float SpawnInterval = 5.0f;  // Kaç saniyede bir kuş spawn
    [Export] public int MaxBirds = 20;            // Maksimum kuş sayısı

    // Security limit
    [Export] public int MaxSecurityCount = 6;
    private int currentSecurityCount = 0;

    // ✅ SECURITY SPAWN AYARLARI
    [Export] public float SecuritySpawnInterval = 60.0f;  // Her 60 saniyede bir
    [Export] public int SecuritySpawnCount = 2;           // Kaç tane spawn olacak
    [Export] public float PlayerDetectionRange = 300.0f;  // Player mesafesi

    private PackedScene BirdScene;
    [Export] public PackedScene TrashMinibossSecurityScene;

    // Değişkenler
    private int currentHealth;
    private int direction = 1;
    private bool isDead = false;
    private float spawnTimer = 0;
    private int currentBirdCount = 0;

    // ✅ SECURITY SPAWN DEĞİŞKENLERİ
    private float securitySpawnTimer = 0;
    private bool hasSpawnedInitialSecurity = false;  // İlk spawn yapıldı mı?

    // Node'lar
    private AnimatedSprite2D animatedSprite;
    private Area2D playerDetector;
    private RayCast2D raycastLeft;
    private RayCast2D raycastRight;
    private Node2D player;

    [Export] public NodePath PathNodePath;  // Path2D'nin yolu
    private Path2D birdPath;

    public override void _Ready()
    {
        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        playerDetector = GetNode<Area2D>("player_detector");

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

        BirdScene = GD.Load<PackedScene>("res://Assets/Scenes/Trash_Bird.tscn");

        // Path2D'yi bul (Level'de olmalı)
        birdPath = GetParent().GetNodeOrNull<Path2D>("Path2D_secured");

        spawnTimer = 2.0f;
        securitySpawnTimer = 0;  // İlk spawn hemen olsun

        GD.Print("[SPAWNER] Sistem başlatıldı!");
        GD.Print($"[SPAWNER] Security Limit: {MaxSecurityCount}");
    }

    private bool playerInRange = false;

    private void OnPlayerEnterRange(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInRange = true;
            player = body;
            GD.Print("[SPAWNER] 🎯 Player menzile girdi!");

            // İlk kez girdiğinde hemen spawn et
            if (!hasSpawnedInitialSecurity)
            {
                SpawnSecurityGuards();
                hasSpawnedInitialSecurity = true;
                securitySpawnTimer = SecuritySpawnInterval;  // Timer'ı başlat
            }
        }
    }

    private void OnPlayerExitRange(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInRange = false;
            GD.Print("[SPAWNER] Player menzilden çıktı!");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (isDead)
            return;

        // Kuş spawn timer
        spawnTimer -= (float)delta;

        if (spawnTimer <= 0 && currentBirdCount < MaxBirds)
        {
            SpawnBird();
            spawnTimer = SpawnInterval;
        }

        // ✅ SECURITY SPAWN SİSTEMİ
        if (playerInRange)
        {
            securitySpawnTimer -= (float)delta;

            if (securitySpawnTimer <= 0)
            {
                SpawnSecurityGuards();
                securitySpawnTimer = SecuritySpawnInterval;  // 60 saniye sonra tekrar
                GD.Print($"[SPAWNER] ⏰ Bir sonraki spawn: {SecuritySpawnInterval}s");
            }
        }
    }

    // ✅ YENİ METOD: Security Guard Spawn
    private void SpawnSecurityGuards()
    {
        if (TrashMinibossSecurityScene == null)
        {
            GD.PrintErr("[SPAWNER] ❌ TrashMinibossSecurityScene atanmamış!");
            return;
        }

        // ✅ Limit kontrolü - MESAJLI
        if (currentSecurityCount >= MaxSecurityCount)
        {
            GD.Print($"[SPAWNER] ⚠️ Security limiti doldu! ({currentSecurityCount}/{MaxSecurityCount})");
            return;
        }

        GD.Print($"[SPAWNER] 🛡️ {SecuritySpawnCount} adet TrashMinibossSecurity spawn ediliyor!");

        for (int i = 0; i < SecuritySpawnCount; i++)
        {
            // Security oluştur
            var security = TrashMinibossSecurityScene.Instantiate<Node2D>();

            // Spawn pozisyonu: Spawner'ın sağında ve solunda
            float offsetX = (i == 0) ? -80f : 80f;  // İlki solda, ikincisi sağda
            float offsetY = 0f;

            Vector2 spawnPos = GlobalPosition + new Vector2(offsetX, offsetY);
            security.GlobalPosition = spawnPos;

            // Level'e ekle (CurrentScene)
            GetTree().CurrentScene.AddChild(security);

            // ✅ KRİTİK: Security öldüğünde sayacı azalt!
            security.TreeExited += () => OnSecurityDied();

            GD.Print($"[SPAWNER] 🛡️ Security {i + 1} spawn edildi! Pos: {spawnPos}");
        }

        currentSecurityCount += SecuritySpawnCount;
        GD.Print($"[SPAWNER] 📊 Toplam Security: {currentSecurityCount}/{MaxSecurityCount}");
    }

    // ✅ YENİ METOD: Security öldüğünde
    private void OnSecurityDied()
    {
        currentSecurityCount--;
        GD.Print($"[SPAWNER] 🛡️💀 Security öldü! Kalan: {currentSecurityCount}/{MaxSecurityCount}");
    }

    private void SpawnBird()
    {
        if (birdPath == null)
        {
            GD.PrintErr("[SPAWNER] ❌ Path2D bulunamadı!");
            return;
        }

        // PathFollow2D oluştur
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

        GD.Print($"[SPAWNER] 🐦 Kuş spawn edildi! Toplam: {currentBirdCount}/{MaxBirds}");
    }

    private void OnBirdDied()
    {
        currentBirdCount--;
        GD.Print($"[SPAWNER] 🐦 Kuş öldü! Kalan: {currentBirdCount}");
    }

    public void TakeDamage(int damage = 1)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        GD.Print($"[SPAWNER] 💔 HP: {currentHealth}/{MaxHealth}");

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
        GD.Print("[SPAWNER] ☠️ Spawner öldü!");

        // Collision kapat
        var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (collision != null)
            collision.SetDeferred("disabled", true);

        if (playerDetector != null)
            playerDetector.Monitoring = false;

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