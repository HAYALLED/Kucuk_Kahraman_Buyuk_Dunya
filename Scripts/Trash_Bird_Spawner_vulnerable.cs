using Godot;
using System;

public partial class Trash_Bird_Spawner_vulnerable : CharacterBody2D
{
    // Temel Ayarlar
    [Export] public int MaxHealth = 8;
    [Export] public float SpawnInterval = 5.0f;  // Kaç saniyede bir spawn
    [Export] public int MaxBirds = 20;            // Maksimum kuş sayısı
    private PackedScene BirdScene;

    // Değişkenler
    private int currentHealth;
    private int direction = 1;
    private bool isDead = false;
    private float spawnTimer = 0;
    private int currentBirdCount = 0;
    // Node'lar
    private AnimatedSprite2D animatedSprite;

    [Export] public NodePath PathNodePath;  // Path2D'nin yolu
    private Path2D birdPath;



    public override void _Ready()
    {
        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        AddToGroup("enemy");
        currentHealth = MaxHealth;

        animatedSprite.Play("idle");

        BirdScene = GD.Load<PackedScene>("res://Assets/Scenes/Trash_Bird.tscn");


        // ✅ Path2D'yi bul (Level'de olmalı)
        birdPath = GetParent().GetNodeOrNull<Path2D>("Path2D_vulnerable");


        spawnTimer = 2.0f;
    }


    public override void _PhysicsProcess(double delta)
    {
        if (isDead)
            return;

        // Spawn timer
        spawnTimer -= (float)delta;

        if (spawnTimer <= 0 && currentBirdCount < MaxBirds)
        {
            SpawnBird();
            spawnTimer = SpawnInterval;
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
