using Godot;
using System;
using System.Collections.Generic;
using static Godot.TextServer;
using static System.Net.Mime.MediaTypeNames;

public partial class BubbleProjectile : Area2D
{
    private int direction = 1;
    private float speed = 300.0f;
    private float lifetime = 8.0f;
    private float stunDuration = 4.0f;

    private HashSet<Node2D> stunnedEnemies = new HashSet<Node2D>();
    private AnimatedSprite2D sprite;
    private CollisionShape2D collision;

    public override void _Ready()
    {
        AddToGroup("projectile");

        sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        // ✅ INSPECTOR AYARLARINI KULLAN - KOD İLE AYARLAMA YOK!

        GD.Print($"[BUBBLE] 🔍 Monitoring: {Monitoring}");
        GD.Print($"[BUBBLE] 🔍 CollisionLayer: {CollisionLayer}");
        GD.Print($"[BUBBLE] 🔍 CollisionMask: {CollisionMask}");
        GD.Print($"[BUBBLE] 🔍 CollisionShape2D: {collision != null}");

        if (sprite != null)
            sprite.Play("default");

        GetTree().CreateTimer(lifetime).Timeout += () =>
        {
            ReleaseAllEnemies();
            QueueFree();
        };

        GD.Print("[BUBBLE] 🌊 Bubble oluşturuldu!");
    }

    public void Setup(int dir, float duration = 4.0f)
    {
        direction = dir;
        stunDuration = duration;

        if (sprite != null)
            sprite.FlipH = direction < 0;

        GD.Print($"[BUBBLE] Yön: {dir}, Stun: {stunDuration}sn");
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        // ✅ Hareket
        GlobalPosition += new Vector2(direction * speed * dt, 0);

        // ✅ OVERLAP KONTROL
        var overlappingBodies = GetOverlappingBodies();

        // ✅ DEBUG: Kaç body algılandı?
        if (overlappingBodies.Count > 0)
        {
            GD.Print($"[BUBBLE] 🔍 {overlappingBodies.Count} body algılandı!");
        }

        foreach (var body in overlappingBodies)
        {
            // ✅ DEBUG: Her body'yi logla
            GD.Print($"[BUBBLE] 🔍 Body: {body.Name}, Type: {body.GetType().Name}, IsEnemy: {body.IsInGroup("enemy")}");

            if (body is Node2D node && node.IsInGroup("enemy"))
            {
                if (!stunnedEnemies.Contains(node))
                {
                    StunEnemy(node);
                }

                // Düşmanı taşı
                node.GlobalPosition = GlobalPosition + new Vector2(0, -20);
            }

            // Duvara çarptı
            if (body is TileMap || body is StaticBody2D)
            {
                GD.Print("[BUBBLE] 💥 Duvara çarptı!");
                ReleaseAllEnemies();
                QueueFree();
                return;
            }
        }
    }

    private void StunEnemy(Node2D enemy)
    {
        stunnedEnemies.Add(enemy);

        GD.Print($"[BUBBLE] ❄️ {enemy.Name} düşmanına stun veriliyor...");

        if (enemy.HasMethod("ApplyStun"))
        {
            enemy.Call("ApplyStun", stunDuration);
            GD.Print($"[BUBBLE] ✅ {enemy.Name} {stunDuration}sn stun'landı! (ApplyStun)");
        }
        else if (enemy.HasMethod("ApplySlow"))
        {
            enemy.Call("ApplySlow", 1.0f, stunDuration);
            GD.Print($"[BUBBLE] ✅ {enemy.Name} {stunDuration}sn slow'landı! (ApplySlow)");
        }
        else
        {
            GD.PrintErr($"[BUBBLE] ❌ {enemy.Name} düşmanında ApplyStun/ApplySlow yok!");
        }

        if (enemy is CharacterBody2D enemyBody)
        {
            enemyBody.Velocity = Vector2.Zero;
        }

        GD.Print($"[BUBBLE] 💧 {enemy.Name} yakalandı! Toplam: {stunnedEnemies.Count}");
    }

    private void ReleaseAllEnemies()
    {
        foreach (var enemy in stunnedEnemies)
        {
            if (IsInstanceValid(enemy) && enemy.HasMethod("ApplySlow"))
            {
                enemy.Call("ApplySlow", 0f, 0f);
            }
        }
        stunnedEnemies.Clear();
    }
}