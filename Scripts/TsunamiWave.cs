using Godot;
using System;
using System.Collections.Generic;

public partial class TsunamiWave : Area2D
{
    private int direction = 1;
    private float speed = 400.0f;
    private float lifetime = 10.0f;
    private List<Node2D> collectedEnemies = new List<Node2D>();
    private bool isCollecting = true;

    private AnimatedSprite2D sprite;
    private CollisionShape2D collision;

    public override void _Ready()
    {
        AddToGroup("projectile");

        sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        BodyEntered += OnBodyEntered;

        // Collision ayarları
        CollisionLayer = 0;
        CollisionMask = 1 | 4; // Layer 1 (platformlar) + Layer 3 (düşmanlar)

        // Sprite animasyonunu başlat
        if (sprite != null)
            sprite.Play("default");

        // Lifetime sonunda düşmanları fırlat
        var timer = GetTree().CreateTimer(lifetime);
        timer.Timeout += ThrowEnemies;

        GD.Print("[TSUNAMI] Tsunami oluşturuldu!");
    }

    public void Setup(int dir, int damage, bool canStun, float stunDuration)
    {
        direction = dir;
        if (sprite != null)
            sprite.FlipH = direction < 0;

        GD.Print($"[TSUNAMI] Setup: direction={dir}");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!isCollecting) return;

        float dt = (float)delta;

        // İlerle
        GlobalPosition += new Vector2(direction * speed * dt, 0);

        // Toplanan düşmanları yanında taşı
        for (int i = collectedEnemies.Count - 1; i >= 0; i--)
        {
            var enemy = collectedEnemies[i];
            if (!IsInstanceValid(enemy))
            {
                collectedEnemies.RemoveAt(i);
                continue;
            }

            enemy.GlobalPosition = GlobalPosition + new Vector2(0, -30);
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        // Düşman topla
        if (body.IsInGroup("enemy") && isCollecting)
        {
            if (!collectedEnemies.Contains(body))
            {
                collectedEnemies.Add(body);

                // Düşmanı dondur
                if (body.HasMethod("Freeze"))
                {
                    body.Call("Freeze");
                }
                else if (body.HasMethod("ApplySlow"))
                {
                    body.Call("ApplySlow", 1.0f, 999f); // Tamamen durdur
                }

                // Düşman fiziksel hareketi durdur
                if (body is CharacterBody2D enemyBody)
                {
                    enemyBody.Velocity = Vector2.Zero;
                }

                GD.Print($"[TSUNAMI] 🌊 {body.Name} toplandı! Toplam: {collectedEnemies.Count}");
            }
        }
        // Duvara çarptı
        else if (body is TileMap || body is StaticBody2D)
        {
            GD.Print("[TSUNAMI] Duvara çarptı!");
            ThrowEnemies();
        }
    }

    private void ThrowEnemies()
    {
        isCollecting = false;

        GD.Print($"[TSUNAMI] Düşmanlar fırlatılıyor: {collectedEnemies.Count}");

        for (int i = collectedEnemies.Count - 1; i >= 0; i--)
        {
            var enemy = collectedEnemies[i];
            if (!IsInstanceValid(enemy))
            {
                collectedEnemies.RemoveAt(i);
                continue;
            }

            // Düşmanı fırlat
            if (enemy is CharacterBody2D enemyBody)
            {
                enemyBody.Velocity = new Vector2(direction * 800, -500);
                GD.Print($"[TSUNAMI] ⚡ {enemy.Name} fırlatıldı!");
            }

            // Unfreeze
            if (enemy.HasMethod("Unfreeze"))
            {
                enemy.Call("Unfreeze");
            }
            else if (enemy.HasMethod("ApplySlow"))
            {
                enemy.Call("ApplySlow", 0f, 0f); // Slow'u kaldır
            }
        }

        collectedEnemies.Clear();
        QueueFree();
    }
}