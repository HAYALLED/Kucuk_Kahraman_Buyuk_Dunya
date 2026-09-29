using Godot;
using System;

public partial class CameraController : Camera2D
{
    // ========================================
    // LOOK-AHEAD (İleri Bakış)
    // Oyuncu sağa gidiyorsa kamera sağa kaydırılır
    // ========================================
    [ExportGroup("İleri Bakış (Look-Ahead)")]
    [Export] public float LookAheadDistance = 80.0f;    // İleri bakış mesafesi (piksel)
    [Export] public float LookAheadSpeed = 3.0f;        // İleri bakış geçiş hızı
    [Export] public float LookAheadThreshold = 10.0f;   // Minimum hareket hızı (bu altında look-ahead yok)

    // ========================================
    // YUKARIYA/AŞAĞIYA BAKMA (Peek)
    // Ok tuşlarını basılı tutunca kamera yukarı/aşağı kayar
    // ========================================
    [ExportGroup("Yukarı/Aşağı Bakma")]
    [Export] public float PeekDistance = 120.0f;         // Bakma mesafesi
    [Export] public float PeekSpeed = 2.5f;              // Bakma geçiş hızı
    [Export] public float PeekHoldTime = 0.3f;           // Bu kadar saniye basılı tutunca peek başlar

    // ========================================
    // SMOOTH FOLLOW
    // ========================================
    [ExportGroup("Smooth Kamera")]
    [Export] public float SmoothSpeedX = 5.0f;           // Yatay takip hızı
    [Export] public float SmoothSpeedY = 8.0f;           // Dikey takip hızı (daha hızlı — düşerken geri kalmasın)

    // ========================================
    // DURUM
    // ========================================
    private float currentLookAheadX = 0;
    private float currentPeekY = 0;
    private float peekHoldTimer = 0;
    private int peekDirection = 0;  // -1=yukarı, 0=yok, 1=aşağı
    private Vector2 targetOffset = Vector2.Zero;
    private CharacterBody2D player;

    public override void _Ready()
    {
        // ✅ Godot'un built-in smoothing'ini KAPAT — kendi smooth sistemimizi kullanacağız
        PositionSmoothingEnabled = false;

        // ✅ Parent'ı player olarak al
        player = GetParent<CharacterBody2D>();

        if (player == null)
        {
            GD.PrintErr("[CAMERA] ❌ Parent CharacterBody2D değil!");
            return;
        }

        // ✅ TopLevel = true → Kamera player'ın child'ı ama bağımsız hareket eder
        TopLevel = true;
        GlobalPosition = player.GlobalPosition;

        GD.Print("[CAMERA] ✅ Smooth kamera aktif!");
        GD.Print($"[CAMERA] LookAhead: {LookAheadDistance}px, Peek: {PeekDistance}px");
    }

    public override void _Process(double delta)
    {
        if (player == null) return;

        float dt = (float)delta;

        // ========================================
        // 1. LOOK-AHEAD (İleri Bakış)
        // ========================================
        float playerVelocityX = player.Velocity.X;
        float targetLookAhead = 0;

        if (Mathf.Abs(playerVelocityX) > LookAheadThreshold)
        {
            // Oyuncu hareket ediyorsa → ileri bak
            targetLookAhead = Mathf.Sign(playerVelocityX) * LookAheadDistance;
        }

        // Smooth geçiş
        currentLookAheadX = Mathf.Lerp(currentLookAheadX, targetLookAhead, LookAheadSpeed * dt);

        // ========================================
        // 2. YUKARIYA/AŞAĞIYA BAKMA (Peek)
        // ========================================
        float targetPeek = 0;

        if (Input.IsActionPressed("camera_up"))
        {
            if (peekDirection != -1)
            {
                peekDirection = -1;
                peekHoldTimer = 0;
            }
            peekHoldTimer += dt;

            if (peekHoldTimer >= PeekHoldTime)
            {
                targetPeek = -PeekDistance;  // Yukarı bak
            }
        }
        else if (Input.IsActionPressed("camera_down"))
        {
            if (peekDirection != 1)
            {
                peekDirection = 1;
                peekHoldTimer = 0;
            }
            peekHoldTimer += dt;

            if (peekHoldTimer >= PeekHoldTime)
            {
                targetPeek = PeekDistance;  // Aşağı bak
            }
        }
        else
        {
            peekDirection = 0;
            peekHoldTimer = 0;
            targetPeek = 0;
        }

        // Smooth geçiş
        currentPeekY = Mathf.Lerp(currentPeekY, targetPeek, PeekSpeed * dt);

        // ========================================
        // 3. SMOOTH FOLLOW
        // ========================================
        // Hedef: player pozisyonu + look-ahead + peek
        Vector2 targetPosition = new Vector2(
            player.GlobalPosition.X + currentLookAheadX,
            player.GlobalPosition.Y + currentPeekY
        );

        // Smooth takip — X ve Y farklı hızlarda
        float newX = Mathf.Lerp(GlobalPosition.X, targetPosition.X, SmoothSpeedX * dt);
        float newY = Mathf.Lerp(GlobalPosition.Y, targetPosition.Y, SmoothSpeedY * dt);

        GlobalPosition = new Vector2(newX, newY);
    }
}