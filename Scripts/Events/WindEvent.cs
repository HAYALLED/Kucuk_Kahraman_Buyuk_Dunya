using Godot;

public partial class WindEvent : BaseEvent
{
    [ExportGroup("Rüzgar Ayarları")]
    [Export] public float CheckInterval { get; set; } = 20f;
    [Export] public float TriggerChance { get; set; } = 0.10f;   // 10%
    [Export] public float WindDuration { get; set; } = 30f;
    [Export] public float SpeedReduction { get; set; } = 0.50f;  // Karşı yönde yavaşlama
    [Export] public float SpeedBoost { get; set; } = 0.30f;      // Rüzgar yönünde hızlanma
    [Export] public float PushForce { get; set; } = 60f;         // Duruyorken itme

    [ExportGroup("Yukarı Rüzgar")]
    [Export] public float DirectionRightChance { get; set; } = 0.50f; // %50 sağa, %50 sola
    [Export] public float UpwardTriggerChance { get; set; } = 0.25f;   // %25 yukarı bileşen
    [Export] public float UpwardGravityMultiplier { get; set; } = 0.5f; // Düşüş yavaşlar
    [Export] public float UpwardJumpBoost { get; set; } = 1.3f;         // Zıplama boost

    private float checkTimer = 0f;
    private float durationTimer = 0f;
    private int windDirection = 1; // 1=sağ, -1=sol
    private bool hasUpward = false;

    protected override void OnSetup()
    {
        EventName = "Rüzgar";
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
                EndWind();
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
        {
            windDirection = GD.Randf() < DirectionRightChance ? 1 : -1;
            hasUpward = GD.Randf() < UpwardTriggerChance;          // %25 yukarı bileşen
            StartWind();
        }
    }

    private void StartWind()
    {
        isActive = true;
        durationTimer = WindDuration;

        var player = manager?.GetPlayer();
        if (player != null)
        {
            player.ApplyWind(windDirection, SpeedReduction, PushForce, SpeedBoost);
            if (hasUpward)
                player.SetUpwardWind(UpwardGravityMultiplier, UpwardJumpBoost);
        }

        string dir = windDirection > 0 ? "→ Sağa" : "← Sola";
        string upStr = hasUpward ? " + ⬆ Yukarı" : "";
        GD.Print($"[RÜZGAR] 💨 Rüzgar başladı! Yön: {dir}{upStr}, Süre: {WindDuration}sn");
        OnEventStart();
    }

    private void EndWind()
    {
        isActive = false;

        var player = manager?.GetPlayer();
        if (player != null)
            player.RemoveWind();

        hasUpward = false;
        GD.Print("[RÜZGAR] Rüzgar durdu.");
        OnEventEnd();
    }
}