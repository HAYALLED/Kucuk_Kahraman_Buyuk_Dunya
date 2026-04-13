using Godot;

public partial class TrashKingEvent : BaseEvent
{
    [ExportGroup("Çöp Kralı Ayarları")]
    [Export] public float CheckInterval { get; set; } = 120f;
    [Export] public float TriggerChance { get; set; } = 0.30f; // 30%
    [Export] public float Duration { get; set; } = 20f;
    [Export] public int BonusDamage { get; set; } = 1;         // +1 hasar

    private float durationTimer = 0f;
    private float checkTimer = 0f;

    protected override void OnSetup()
    {
        EventName = "Çöp Kralından Gelen Emir";
        checkTimer = CheckInterval;
    }

    public override void OnApplyOverrides()
    {
        checkTimer = CheckInterval;
    }

    public override void _Process(double delta)
    {
        if (!EnableEvent) return;
        float dt = (float)delta;

        if (isActive)
        {
            durationTimer -= dt;
            if (durationTimer <= 0)
                EndEdict();
        }
        else
        {
            checkTimer -= dt;
            if (checkTimer <= 0)
            {
                checkTimer = CheckInterval;
                TryTrigger();
            }
        }
    }

    private void TryTrigger()
    {
        if (GD.Randf() < TriggerChance)
            StartEdict();
    }

    private void StartEdict()
    {
        isActive = true;
        durationTimer = Duration;

        // Tüm düşmanlara bonus hasar ver
        var enemies = GetTree().GetNodesInGroup("enemy");
        foreach (var enemy in enemies)
        {
            if (enemy.HasMethod("AddBonusDamage"))
                enemy.Call("AddBonusDamage", BonusDamage);
        }

        GD.Print($"[ÇÖP KRALI] 👑 Emir geldi! {enemies.Count} düşmana +{BonusDamage} hasar, {Duration}sn");
        OnEventStart();
    }

    private void EndEdict()
    {
        isActive = false;

        // Bonus hasarı geri al
        var enemies = GetTree().GetNodesInGroup("enemy");
        foreach (var enemy in enemies)
        {
            if (enemy.HasMethod("AddBonusDamage"))
                enemy.Call("AddBonusDamage", -BonusDamage);
        }

        GD.Print("[ÇÖP KRALI] Emir bitti, düşman hasarı normale döndü.");
        OnEventEnd();
    }
}