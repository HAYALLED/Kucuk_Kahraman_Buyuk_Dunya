using Godot;
using System.Collections.Generic;

public partial class EventManager : Node
{
    [ExportGroup("Debug")]
    [Export] public bool ShowDebugLogs { get; set; } = true;

    [ExportGroup("Bildirim")]
    [Export] public float NotificationDuration { get; set; } = 3f;
    [Export] public float NotificationFadeTime { get; set; } = 1f;
    [Export] public int NotificationFontSize { get; set; } = 28;

    [ExportGroup("Rüzgar")]
    [Export] public bool Wind_Enable { get; set; } = true;
    [Export] public float Wind_TriggerChance { get; set; } = 0.10f;
    [Export] public float Wind_CheckInterval { get; set; } = 20f;
    [Export] public float Wind_Duration { get; set; } = 30f;
    [Export] public float Wind_DirectionRightChance { get; set; } = 0.50f;
    [Export] public float Wind_UpwardTriggerChance { get; set; } = 0.25f;

    [ExportGroup("Çöp Yağmuru")]
    [Export] public bool TrashRain_Enable { get; set; } = true;
    [Export] public float TrashRain_TriggerChance { get; set; } = 0.20f;
    [Export] public float TrashRain_CheckInterval { get; set; } = 60f;
    [Export] public float TrashRain_SpawnMultiplier { get; set; } = 1.5f;

    [ExportGroup("Çöp Toplama Kampanyası")]
    [Export] public bool Campaign_Enable { get; set; } = true;
    [Export] public float Campaign_TriggerChance { get; set; } = 0.50f;
    [Export] public float Campaign_CheckInterval { get; set; } = 100f;
    [Export] public float Campaign_Duration { get; set; } = 30f;
    [Export] public float Campaign_ScoreMultiplier { get; set; } = 2f;
    [Export] public float Campaign_CorrectMultiplier { get; set; } = 1.5f;
    [Export] public float Campaign_WrongMultiplier { get; set; } = 2f;

    [ExportGroup("Çöp Kralı")]
    [Export] public bool TrashKing_Enable { get; set; } = true;
    [Export] public float TrashKing_TriggerChance { get; set; } = 0.30f;
    [Export] public float TrashKing_CheckInterval { get; set; } = 120f;
    [Export] public float TrashKing_Duration { get; set; } = 20f;

    [ExportGroup("Kuş Göçü")]
    [Export] public bool BirdMigration_Enable { get; set; } = true;
    [Export] public float BirdMigration_TriggerChance { get; set; } = 0.50f;
    [Export] public float BirdMigration_SpawnIntervalMultiplier { get; set; } = 2f;
    [Export] public int BirdMigration_MaxBirdsMultiplier { get; set; } = 2;
    [Export] public float BirdMigration_BaseSpawnInterval { get; set; } = 3f;
    [Export] public int BirdMigration_BaseMaxBirds { get; set; } = 5;
    [Export] public float BirdMigration_BirdPathSpeed { get; set; } = 150f;
    [Export] public Node2D[] BirdMigration_MigrationSpawners;
    [Export] public Path2D[] BirdMigration_MigrationPaths;

    // ===========================
    private List<BaseEvent> events = new();
    private Player_controller player;

    private CanvasLayer notificationLayer;
    private Label notificationLabel;

    public override void _Ready()
    {
        CreateNotificationUI();

        foreach (Node child in GetChildren())
        {
            if (child is BaseEvent baseEvent)
            {
                events.Add(baseEvent);
                if (ShowDebugLogs)
                    GD.Print($"[EVENT MANAGER] ✅ Event bulundu: {baseEvent.EventName}");
            }
        }

        ApplyOverrides();
        CallDeferred(nameof(InitAfterReady));
        GD.Print($"[EVENT MANAGER] ✅ {events.Count} event yüklendi.");
    }

    private void CreateNotificationUI()
    {
        notificationLayer = new CanvasLayer();
        notificationLayer.Name = "EventNotificationLayer";
        notificationLayer.Layer = 200;
        AddChild(notificationLayer);

        notificationLabel = new Label();
        notificationLabel.Name = "EventNotificationLabel";
        notificationLabel.Text = "";
        notificationLabel.Visible = false;
        notificationLabel.HorizontalAlignment = HorizontalAlignment.Center;
        notificationLabel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        notificationLabel.OffsetTop = 80f;
        notificationLabel.OffsetBottom = 120f;
        notificationLabel.AddThemeFontSizeOverride("font_size", NotificationFontSize);
        notificationLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        notificationLabel.AddThemeColorOverride("font_shadow_color", Colors.Black);
        notificationLabel.AddThemeConstantOverride("shadow_offset_x", 2);
        notificationLabel.AddThemeConstantOverride("shadow_offset_y", 2);

        notificationLayer.AddChild(notificationLabel);
    }

    public async void ShowEventNotification(string eventName)
    {
        if (notificationLabel == null) return;

        notificationLabel.Text = $"⚠ {eventName} etkinliği tetiklendi!";
        notificationLabel.Modulate = new Color(1f, 1f, 1f, 1f);
        notificationLabel.Visible = true;

        GD.Print($"[EVENT MANAGER] 📢 {eventName} etkinliği tetiklendi!");

        await ToSignal(GetTree().CreateTimer(NotificationDuration), SceneTreeTimer.SignalName.Timeout);

        if (!IsInstanceValid(notificationLabel)) return;

        var tween = CreateTween();
        tween.TweenProperty(notificationLabel, "modulate:a", 0f, NotificationFadeTime);
        await ToSignal(tween, Tween.SignalName.Finished);

        if (IsInstanceValid(notificationLabel))
            notificationLabel.Visible = false;
    }

    private void InitAfterReady()
    {
        player = GetTree().GetFirstNodeInGroup("player") as Player_controller;

        if (player == null)
            GD.PrintErr("[EVENT MANAGER] ❌ Player bulunamadı!");
        else
            GD.Print($"[EVENT MANAGER] ✅ Player bulundu: {player.Name}");

        TriggerLevelStartEvents();
    }

    private void ApplyOverrides()
    {
        foreach (var e in events)
        {
            switch (e)
            {
                case WindEvent wind:
                    wind.EnableEvent = Wind_Enable;
                    wind.TriggerChance = Wind_TriggerChance;
                    wind.CheckInterval = Wind_CheckInterval;
                    wind.WindDuration = Wind_Duration;
                    wind.DirectionRightChance = Wind_DirectionRightChance;
                    wind.UpwardTriggerChance = Wind_UpwardTriggerChance;
                    Log($"Rüzgar → Enable:{Wind_Enable}, Şans:%{Wind_TriggerChance * 100:F0}");
                    break;

                case TrashRainEvent rain:
                    rain.EnableEvent = TrashRain_Enable;
                    rain.TriggerChance = TrashRain_TriggerChance;
                    rain.CheckInterval = TrashRain_CheckInterval;
                    rain.SpawnMultiplier = TrashRain_SpawnMultiplier;
                    Log($"Çöp Yağmuru → Enable:{TrashRain_Enable}, Şans:%{TrashRain_TriggerChance * 100:F0}");
                    break;

                case TrashCampaignEvent campaign:
                    campaign.EnableEvent = Campaign_Enable;
                    campaign.TriggerChance = Campaign_TriggerChance;
                    campaign.ScoreMultiplier = Campaign_ScoreMultiplier;
                    campaign.CorrectMultiplier = Campaign_CorrectMultiplier;
                    campaign.WrongMultiplier = Campaign_WrongMultiplier;
                    Log($"Kampanya → Enable:{Campaign_Enable}, Şans:%{Campaign_TriggerChance * 100:F0}");
                    break;

                case TrashKingEvent king:
                    king.EnableEvent = TrashKing_Enable;
                    king.TriggerChance = TrashKing_TriggerChance;
                    king.CheckInterval = TrashKing_CheckInterval;
                    king.Duration = TrashKing_Duration;
                    Log($"Çöp Kralı → Enable:{TrashKing_Enable}, Şans:%{TrashKing_TriggerChance * 100:F0}");
                    break;

                case BirdMigrationEvent bird:
                    bird.EnableEvent = BirdMigration_Enable;
                    bird.TriggerChance = BirdMigration_TriggerChance;
                    bird.SpawnIntervalMultiplier = BirdMigration_SpawnIntervalMultiplier;
                    bird.MaxBirdsMultiplier = BirdMigration_MaxBirdsMultiplier;
                    bird.BaseSpawnInterval = BirdMigration_BaseSpawnInterval;
                    bird.BaseMaxBirds = BirdMigration_BaseMaxBirds;
                    bird.BirdPathSpeed = BirdMigration_BirdPathSpeed;
                    if (BirdMigration_MigrationSpawners != null) bird.MigrationSpawners = BirdMigration_MigrationSpawners;
                    if (BirdMigration_MigrationPaths != null) bird.MigrationPaths = BirdMigration_MigrationPaths;
                    Log($"Kuş Göçü → Enable:{BirdMigration_Enable}, Şans:%{BirdMigration_TriggerChance * 100:F0}, Hız:{BirdMigration_BirdPathSpeed}");
                    break;
            }

            // ✅ Override sonrası event'e bildir — checkTimer gibi şeyler güncel değerle yeniden init edilir
            e.OnApplyOverrides();
        }
    }

    private void TriggerLevelStartEvents()
    {
        foreach (var e in events)
        {
            if (e.EnableEvent)
                e.OnLevelStart();
        }
    }

    public Player_controller GetPlayer()
    {
        if (player == null)
            player = GetTree().GetFirstNodeInGroup("player") as Player_controller;
        return player;
    }

    public void Log(string message)
    {
        if (ShowDebugLogs)
            GD.Print($"[EVENT MANAGER] {message}");
    }
}