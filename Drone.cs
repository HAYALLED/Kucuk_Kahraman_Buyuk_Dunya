using Godot;
using System;

public partial class Drone : Area2D
{
    [Export] public float Speed = 400f;
    [Export] public float DetectionRadius = 500f;
    [Export] public int Damage = 2;
    [Export] public float Lifetime = 10.0f;

    [Export] public float OrbitRadius = 80f;
    [Export] public float OrbitSpeed = 2.0f;

    private Player_controller player;
    private AnimatedSprite2D animatedSprite;
    private Node2D targetEnemy;
    private bool hasTarget = false;
    private bool isOrbiting = true;
    private float lifetimeTimer = 0;
    private float orbitAngle = 0;

    private CollisionShape2D hitBox;
    private CollisionShape2D detectionZone;

    public override void _Ready()
    {
        hitBox = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        detectionZone = GetNodeOrNull<CollisionShape2D>("CollisionShape2D2");

        if (hitBox == null)
        {
            GD.PrintErr("[DRONE] ❌ CollisionShape2D (gövde) bulunamadı!");
        }
        else
        {
            GD.Print("[DRONE] ✅ HitBox bulundu!");
        }

        if (detectionZone == null)
        {
            GD.PrintErr("[DRONE] ❌ CollisionShape2D2 (algılama) bulunamadı!");
        }
        else
        {
            GD.Print("[DRONE] ✅ DetectionZone bulundu!");
        }

        BodyEntered += OnBodyEntered;

        animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

        if (animatedSprite != null && animatedSprite.SpriteFrames != null)
        {
            if (animatedSprite.SpriteFrames.HasAnimation("fly"))
            {
                animatedSprite.Play("fly");
            }
            else if (animatedSprite.SpriteFrames.HasAnimation("default"))
            {
                animatedSprite.Play("default");
            }
        }

        CallDeferred(nameof(FindPlayer));

        orbitAngle = (float)(GD.Randf() * Mathf.Tau);

        GD.Print("[DRONE] 🚀 Attack drone oluşturuldu!");
    }

    private void FindPlayer()
    {
        var players = GetTree().GetNodesInGroup("player");

        if (players.Count > 0)
        {
            player = players[0] as Player_controller;
            GD.Print($"[DRONE] ✅ Player bulundu: {player.Name}");
        }
        else
        {
            GD.PrintErr("[DRONE] ❌ Player bulunamadı! Drone yok ediliyor...");
            QueueFree();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        lifetimeTimer += dt;

        if (lifetimeTimer >= Lifetime)
        {
            GD.Print("[DRONE] ⏱️ Yaşam süresi doldu!");
            QueueFree();
            return;
        }

        if (player == null || !IsInstanceValid(player))
        {
            QueueFree();
            return;
        }

        // ===== HOMING MİSSİLE MODU (Hedef varsa) =====
        if (hasTarget && targetEnemy != null && IsInstanceValid(targetEnemy))
        {
            isOrbiting = false;

            Vector2 direction = (targetEnemy.GlobalPosition - GlobalPosition).Normalized();
            GlobalPosition += direction * Speed * dt;

            Rotation = direction.Angle();

            if (animatedSprite != null)
            {
                animatedSprite.FlipH = direction.X < 0;
            }

            // ✅ FIX: MANUEL MESAFE KONTROLÜ (Homing modunda)
            float distance = GlobalPosition.DistanceTo(targetEnemy.GlobalPosition);

            if (distance < 40)  // HitBox menzili
            {
                // ✅ HASAR VER!
                if (targetEnemy.HasMethod("TakeDamage"))
                {
                    targetEnemy.Call("TakeDamage", Damage);
                    GD.Print($"[DRONE] 💥 {targetEnemy.Name} düşmanına {Damage} hasar verildi!");
                    QueueFree();
                }
            }
        }
        // ===== ORBİT MODU (Hedef yoksa) =====
        else
        {
            isOrbiting = true;

            orbitAngle += OrbitSpeed * dt;

            float offsetX = Mathf.Cos(orbitAngle) * OrbitRadius;
            float offsetY = Mathf.Sin(orbitAngle) * OrbitRadius;

            Vector2 targetPos = player.GlobalPosition + new Vector2(offsetX, offsetY);

            GlobalPosition = GlobalPosition.Lerp(targetPos, 10f * dt);

            Rotation = 0;

            if (animatedSprite != null)
            {
                animatedSprite.FlipH = offsetX < 0;
            }
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!IsInstanceValid(body) || !body.IsInGroup("enemy"))
            return;

        // ✅ Sadece orbit modunda hedef kilitle!
        if (isOrbiting && !hasTarget)
        {
            targetEnemy = body;
            hasTarget = true;

            float distance = GlobalPosition.DistanceTo(body.GlobalPosition);
            GD.Print($"[DRONE] 🎯 Hedef kilitlendi: {body.Name} ({distance:F0}px)");

            // ✅ DEFERRED: Detection zone'u güvenli kapat!
            if (detectionZone != null)
            {
                CallDeferred("DisableDetectionZone");
            }
        }
    }
    private void DisableDetectionZone()
    {
        if (detectionZone != null && IsInstanceValid(detectionZone))
        {
            detectionZone.Disabled = true;
            GD.Print("[DRONE] 🔒 Detection zone kapatıldı!");
        }
    }


}