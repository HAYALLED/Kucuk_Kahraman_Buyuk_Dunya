using Godot;
using System.Collections.Generic;

public partial class WebProjectile : BaseProjectile
{
    // Stun sistemi - aynı düşmana 10 saniyede 3 vuruş = stun
    private static Dictionary<ulong, StunTracker> enemyHitTrackers = new Dictionary<ulong, StunTracker>();

    private class StunTracker
    {
        public int hitCount = 0;
        public float timer = 0;
    }

    [Export] public float SlowPercent = 0.2f;  // Stun sonrası yavaşlama
    [Export] public float SlowDuration = 3.0f;

    public override void _Ready()
    {
        base._Ready();

        // Ağ animasyonu
        if (animatedSprite != null && animatedSprite.SpriteFrames.HasAnimation("default"))
        {
            animatedSprite.Play("default");
        }
    }

    protected override void HitEnemy(Node2D enemy)
    {
        ulong enemyId = enemy.GetInstanceId();

        // Tracker yoksa oluştur
        if (!enemyHitTrackers.ContainsKey(enemyId))
        {
            enemyHitTrackers[enemyId] = new StunTracker();
        }

        var tracker = enemyHitTrackers[enemyId];

        // Timer sıfırlanmış mı kontrol et (10 saniye geçtiyse)
        tracker.hitCount++;
        tracker.timer = stunTimeWindow;

        GD.Print($"[WEB] Düşmana vuruş! ({tracker.hitCount}/{stunHitCount})");

        // Hasar ver (0 olabilir)
        if (Damage > 0 && enemy.HasMethod("TakeDamage"))
        {
            enemy.Call("TakeDamage", Damage);
        }

        // 3 vuruşa ulaştı mı?
        if (tracker.hitCount >= stunHitCount)
        {
            // Stun ver
            if (enemy.HasMethod("ApplyStun"))
            {
                enemy.Call("ApplyStun", stunDuration);
                GD.Print($"[WEB] Düşman {stunDuration}sn stunlandı!");
            }

            // Stun sonrası yavaşlatma
            if (enemy.HasMethod("ApplySlow"))
            {
                enemy.Call("ApplySlow", SlowPercent, SlowDuration);
                GD.Print($"[WEB] Düşman {SlowDuration}sn yavaşlatıldı!");
            }

            // Tracker sıfırla
            tracker.hitCount = 0;
        }

        QueueFree();
    }

    // Static timer güncelleme (her frame çağrılmalı - opsiyonel)
    public static void UpdateTrackers(float delta)
    {
        var keysToRemove = new List<ulong>();

        foreach (var kvp in enemyHitTrackers)
        {
            kvp.Value.timer -= delta;
            if (kvp.Value.timer <= 0)
            {
                kvp.Value.hitCount = 0;
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            enemyHitTrackers.Remove(key);
        }
    }
}