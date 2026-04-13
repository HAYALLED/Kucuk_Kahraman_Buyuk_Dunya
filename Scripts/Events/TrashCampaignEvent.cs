using Godot;

public partial class TrashCampaignEvent : BaseEvent
{
    [ExportGroup("Kampanya Ayarları")]
    [Export] public float TriggerChance { get; set; } = 0.50f;
    [Export] public float ScoreMultiplier { get; set; } = 2f;       // Minigame final skoru çarpanı
    [Export] public float CorrectMultiplier { get; set; } = 1.5f;   // Doğru cevap puan çarpanı
    [Export] public float WrongMultiplier { get; set; } = 2f;       // Yanlış cevap ceza çarpanı

    // Diğer scriptlerin okuması için static
    public static float ActiveScoreMultiplier { get; private set; } = 1f;
    public static float ActiveCorrectMultiplier { get; private set; } = 1f;
    public static float ActiveWrongMultiplier { get; private set; } = 1f;
    public static bool IsCampaignActive { get; private set; } = false;

    protected override void OnSetup()
    {
        EventName = "Çöp Toplama Kampanyası";
    }

    // Level başında bir kez tetiklenir — tüm level boyunca aktif kalır
    public override void OnLevelStart()
    {
        if (!EnableEvent) return;
        if (GD.Randf() < TriggerChance)
            StartCampaign();
        else
            GD.Print("[KAMPANYA] Bu levelde kampanya yok.");
    }

    private void StartCampaign()
    {
        isActive = true;
        IsCampaignActive = true;
        ActiveScoreMultiplier = ScoreMultiplier;
        ActiveCorrectMultiplier = CorrectMultiplier;
        ActiveWrongMultiplier = WrongMultiplier;
        GD.Print($"[KAMPANYA] ⭐ Başladı! Final x{ScoreMultiplier} | Doğru x{CorrectMultiplier} | Yanlış x{WrongMultiplier}");
        OnEventStart();
    }

    // Level bitince veya manuel reset
    public void EndCampaign()
    {
        isActive = false;
        IsCampaignActive = false;
        ActiveScoreMultiplier = 1f;
        ActiveCorrectMultiplier = 1f;
        ActiveWrongMultiplier = 1f;
        GD.Print("[KAMPANYA] Bitti.");
        OnEventEnd();
    }
}