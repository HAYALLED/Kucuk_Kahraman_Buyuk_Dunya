using Godot;
using System;

public partial class Mr_AdGuy : CanvasLayer
{
    // ========================================
    // NODE REFERANSLARI
    // ========================================
    private AnimatedSprite2D animatedSprite;
    private AnimatedSprite2D posterSprite;
    private TextureButton closeButton;
    private Timer showTimer;
    private Timer respawnTimer;

    // ========================================
    // AYARLAR
    // ========================================
    [ExportGroup("Zamanlama")]
    [Export] public float MinShowTime = 8.0f;
    [Export] public float MaxShowTime = 15.0f;
    [Export] public float MinRespawnTime = 45.0f;
    [Export] public float MaxRespawnTime = 150.0f;
    [Export] public float FirstSpawnDelay = 10.0f;

    // ========================================
    // DURUM
    // ========================================
    private bool isShowing = false;
    private bool firstSpawn = true;

    private Godot.RandomNumberGenerator rng = new Godot.RandomNumberGenerator();

    public override void _Ready()
    {
        // ✅ KRİTİK: CanvasLayer ayarları
        Layer = 90;                          // Yüksek layer — UI'ın üstünde
        FollowViewportEnabled = false;       // ✅ FIX: Viewport'u TAKİP ETME → ekran koordinatlarında kal

        GD.Print("[AD GUY] ========================================");
        GD.Print("[AD GUY] 📺 Mr_AdGuy _Ready()");
        GD.Print($"[AD GUY] Parent: {GetParent()?.Name ?? "YOK"}");
        GD.Print($"[AD GUY] FollowViewportEnabled: {FollowViewportEnabled}");

        LoadNodes();
        ConnectSignals();

        rng.Randomize();
        HideAll();
        StartRespawnTimer();

        GD.Print($"[AD GUY] ✅ Hazır! İlk çıkış ~{FirstSpawnDelay:F0}sn sonra");
        GD.Print("[AD GUY] ========================================");
    }

    private void LoadNodes()
    {
        // ✅ Her node'u AYRI AYRI yükle (try-catch hepsini bozmasın)
        animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        GD.Print($"[AD GUY] AnimatedSprite2D: {(animatedSprite != null ? "✅" : "❌")}");

        posterSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D/PosterSprite");
        GD.Print($"[AD GUY] PosterSprite: {(posterSprite != null ? "✅" : "⚠️ opsiyonel")}");

        closeButton = GetNodeOrNull<TextureButton>("CloseButton");
        GD.Print($"[AD GUY] CloseButton: {(closeButton != null ? "✅" : "❌")}");

        // Timer'lar — path dene, yoksa koddan oluştur
        showTimer = GetNodeOrNull<Timer>("Timers/ShowTimer");
        if (showTimer == null) showTimer = GetNodeOrNull<Timer>("ShowTimer");
        if (showTimer == null)
        {
            showTimer = new Timer { Name = "ShowTimer_Auto", OneShot = true };
            AddChild(showTimer);
            GD.Print("[AD GUY] ShowTimer: ⚠️ koddan oluşturuldu");
        }
        else GD.Print("[AD GUY] ShowTimer: ✅");

        respawnTimer = GetNodeOrNull<Timer>("Timers/RespawnTimer");
        if (respawnTimer == null) respawnTimer = GetNodeOrNull<Timer>("RespawnTimer");
        if (respawnTimer == null)
        {
            respawnTimer = new Timer { Name = "RespawnTimer_Auto", OneShot = true };
            AddChild(respawnTimer);
            GD.Print("[AD GUY] RespawnTimer: ⚠️ koddan oluşturuldu");
        }
        else GD.Print("[AD GUY] RespawnTimer: ✅");

    }

    private void ConnectSignals()
    {
        if (closeButton != null)
            closeButton.Pressed += OnCloseButtonPressed;
        if (showTimer != null)
            showTimer.Timeout += OnShowTimerTimeout;
        if (respawnTimer != null)
            respawnTimer.Timeout += OnRespawnTimerTimeout;
        if (animatedSprite != null)
            animatedSprite.AnimationFinished += OnAnimationFinished;
    }

    private void ShowAll()
    {
        Visible = true;
        if (animatedSprite != null) animatedSprite.Visible = true;
        if (posterSprite != null) posterSprite.Visible = true;
        if (closeButton != null) closeButton.Visible = true;
    }

    private void HideAll()
    {
        Visible = false;
        if (animatedSprite != null) animatedSprite.Visible = false;
        if (posterSprite != null) posterSprite.Visible = false;
        if (closeButton != null) closeButton.Visible = false;
    }

    // ========================================
    // SHOW
    // ========================================
    private void ShowAdGuy()
    {
        if (isShowing) return;

        if (animatedSprite == null)
        {
            StartRespawnTimer();
            return;
        }

        isShowing = true;

        SetRandomPosition();
        SelectRandomPoster();
        ShowAll();

        // Animasyon fallback: opening → loop → default
        if (animatedSprite.SpriteFrames != null)
        {
            string[] tryAnims = { "opening", "loop", "default" };
            foreach (string anim in tryAnims)
            {
                if (animatedSprite.SpriteFrames.HasAnimation(anim))
                {
                    animatedSprite.Play(anim);
                    break;
                }
            }
        }

        float showTime = rng.RandfRange(MinShowTime, MaxShowTime);
        showTimer.WaitTime = showTime;
        showTimer.Start();

    }

    // ========================================
    // HIDE
    // ========================================
    private void HideAdGuy(bool closedByPlayer = false)
    {
        if (!isShowing) return;
        isShowing = false;
        showTimer.Stop();

        if (animatedSprite != null &&
            animatedSprite.SpriteFrames != null &&
            animatedSprite.SpriteFrames.HasAnimation("ending"))
        {
            animatedSprite.Play("ending");
        }
        else
        {
            HideAll();
            StartRespawnTimer();
        }
    }

    // ========================================
    // POSTER & POZİSYON
    // ========================================
    private void SelectRandomPoster()
    {
        if (posterSprite?.SpriteFrames == null) return;
        var animNames = posterSprite.SpriteFrames.GetAnimationNames();
        if (animNames.Length == 0) return;

        string animName = animNames[0].ToString();
        if (!posterSprite.SpriteFrames.HasAnimation(animName)) return;

        int frameCount = posterSprite.SpriteFrames.GetFrameCount(animName);
        if (frameCount == 0) return;

        int randomFrame = rng.RandiRange(0, frameCount - 1);
        posterSprite.Play(animName);
        posterSprite.Frame = randomFrame;
        posterSprite.Stop();
    }

    private void SetRandomPosition()
    {
        // ✅ Viewport boyutunu al (kamera ne gösteriyorsa o)
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;

        float margin = 100f;
        float randomX = rng.RandfRange(margin, viewportSize.X - margin);
        float randomY = rng.RandfRange(margin, viewportSize.Y - margin);

        if (animatedSprite != null)
            animatedSprite.Position = new Vector2(randomX, randomY);

        if (closeButton != null)
            closeButton.Position = new Vector2(randomX - 45, randomY - 65);
    }

    private void StartRespawnTimer()
    {
        if (respawnTimer == null) return;

        float waitTime;
        if (firstSpawn)
        {
            waitTime = FirstSpawnDelay;
            firstSpawn = false;
        }
        else
        {
            waitTime = rng.RandfRange(MinRespawnTime, MaxRespawnTime);
        }

        respawnTimer.WaitTime = waitTime;
        respawnTimer.Start();
    }

    private void OnCloseButtonPressed()
    {
        HideAdGuy(closedByPlayer: true);
    }

    private void OnShowTimerTimeout()
    {
        HideAdGuy(closedByPlayer: false);
    }

    private void OnRespawnTimerTimeout()
    {
        ShowAdGuy();
    }

    private void OnAnimationFinished()
    {
        if (animatedSprite == null) return;
        string current = animatedSprite.Animation;

        if (current == "opening")
        {
            if (animatedSprite.SpriteFrames.HasAnimation("loop"))
                animatedSprite.Play("loop");
        }
        else if (current == "ending")
        {
            HideAll();
            StartRespawnTimer();
        }
    }

    // ========================================
    // PUBLIC
    // ========================================
    public void ForceShow()
    {
        respawnTimer?.Stop();
        ShowAdGuy();
    }

    public void Disable()
    {
        showTimer?.Stop();
        respawnTimer?.Stop();
        HideAll();
        isShowing = false;
    }

    public void Enable()
    {
        StartRespawnTimer();
    }
}