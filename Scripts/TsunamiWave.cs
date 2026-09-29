using Godot;
using System;
using System.Collections.Generic;

public partial class TsunamiWave : Area2D
{
    private int direction = 1;
    private float speed = 400.0f;
    private float lifetime = 10.0f;
    private int damage = 2;

    private HashSet<Node2D> damagedEnemies = new HashSet<Node2D>();
    private AnimatedSprite2D sprite;
    private CollisionShape2D collision;

    public override void _Ready()
    {
        AddToGroup("projectile");

        sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        GD.Print($"[TSUNAMI] 🔍 Monitoring: {Monitoring}");
        GD.Print($"[TSUNAMI] 🔍 CollisionLayer: {CollisionLayer}");
        GD.Print($"[TSUNAMI] 🔍 CollisionMask: {CollisionMask}");
        GD.Print($"[TSUNAMI] 🔍 CollisionShape2D: {collision != null}");

        if (sprite != null)
            sprite.Play("default");

        GetTree().CreateTimer(lifetime).Timeout += () => QueueFree();

        GD.Print("[TSUNAMI] 🌊 Tsunami oluşturuldu!");
    }

    public void Setup(int dir, int dmg, bool canStun, float stunDuration)
    {
        direction = dir;
        damage = dmg;

        if (sprite != null)
            sprite.FlipH = direction < 0;

        GD.Print($"[TSUNAMI] Setup: direction={dir}, damage={damage}");
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        // ✅ Hareket
        GlobalPosition += new Vector2(direction * speed * dt, 0);

        // ✅ OVERLAP KONTROL
        var overlappingBodies = GetOverlappingBodies();

        foreach (var body in overlappingBodies)
        {
            // Düşmana çarptı
            if (body is Node2D node && node.IsInGroup("enemy"))
            {
                // ✅ İlk çarpışta hasar ver
                if (!damagedEnemies.Contains(node))
                {
                    DamageEnemy(node);
                }

                // ✅ YENİ: Düşmanı tsunami'nin içinde taşı!
                node.GlobalPosition = GlobalPosition + new Vector2(0, -30);
            }

            // Duvara çarptı
            if (body is TileMap || body is StaticBody2D)
            {
                GD.Print("[TSUNAMI] 💥 Duvara çarptı!");
                QueueFree();
                return;
            }
        }
    }

    private void DamageEnemy(Node2D enemy)
    {
        damagedEnemies.Add(enemy);

        GD.Print($"[TSUNAMI] ⚔️ {enemy.Name} düşmanına hasar veriliyor...");

        if (enemy.HasMethod("TakeDamage"))
        {
            enemy.Call("TakeDamage", damage);
            GD.Print($"[TSUNAMI] ✅ {enemy.Name} düşmanına {damage} hasar verildi!");
        }
        else
        {
            GD.PrintErr($"[TSUNAMI] ❌ {enemy.Name} düşmanında TakeDamage metodu yok!");
        }
    }
}