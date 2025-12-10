using Godot;

public partial class WizardProjectile : BaseProjectile
{
    [Export] public float ArcHeight = 150f;
    [Export] public float TravelTime = 1.2f;

    private Vector2 startPos;
    private Vector2 targetPos;
    private float elapsedTime = 0;
    private bool isMoving = false;
    private bool isSetupDone = false;
    private CollisionShape2D collisionShape;

    public override void _Ready()
    {
        animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        BodyEntered += OnBodyEntered;

        // Başlangıçta collision açık ama hasar vermiyor (setup bekliyor)
        if (collisionShape != null)
            collisionShape.Disabled = false;
        Monitoring = true;

        // 7 saniye sonra yok ol
        GetTree().CreateTimer(7.0f).Timeout += () =>
        {
            if (IsInstanceValid(this))
            {
                GD.Print("[WIZARD_PROJ] ⏰ Timeout, yok ediliyor!");
                QueueFree();
            }
        };

        GD.Print($"[WIZARD_PROJ] ✨ Oluşturuldu! Pos: {GlobalPosition}");

        // Startup animasyonu varsa oynat
        if (animatedSprite != null && animatedSprite.SpriteFrames.HasAnimation("startup"))
        {
            animatedSprite.Play("startup");
            animatedSprite.AnimationFinished += OnStartupFinished;
        }
    }

    private void OnStartupFinished()
    {
        if (animatedSprite.Animation == "startup")
        {
            GD.Print("[WIZARD_PROJ] 🎬 Startup bitti!");

            // Projectile animasyonuna geç
            if (animatedSprite.SpriteFrames.HasAnimation("projectile"))
            {
                animatedSprite.Play("projectile");
            }

            // Arc hareketini başlat (eğer setup yapıldıysa)
            if (isSetupDone)
            {
                isMoving = true;
                GD.Print("[WIZARD_PROJ] 🚀 Arc hareketi başladı!");
            }
        }
    }

    public void SetupArc(Vector2 target, int damage)
    {
        targetPos = target;
        Damage = damage;
        startPos = GlobalPosition;
        elapsedTime = 0;
        isSetupDone = true;

        GD.Print($"[WIZARD_PROJ] ⚙️ SetupArc: Start={startPos}, Target={targetPos}, Damage={damage}");

        // Eğer startup yoksa veya bittiyse, hemen harekete geç
        if (animatedSprite == null ||
            !animatedSprite.SpriteFrames.HasAnimation("startup") ||
            animatedSprite.Animation != "startup")
        {
            isMoving = true;

            if (animatedSprite != null && animatedSprite.SpriteFrames.HasAnimation("projectile"))
            {
                animatedSprite.Play("projectile");
            }

            GD.Print("[WIZARD_PROJ] 🚀 Direkt arc başladı!");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!isMoving || !isSetupDone) return;

        elapsedTime += (float)delta;
        float t = Mathf.Clamp(elapsedTime / TravelTime, 0f, 1f);

        // Parabolic arc (yay hareketi)
        float x = Mathf.Lerp(startPos.X, targetPos.X, t);
        float baseY = Mathf.Lerp(startPos.Y, targetPos.Y, t);
        float arcOffset = -Mathf.Sin(t * Mathf.Pi) * ArcHeight;

        GlobalPosition = new Vector2(x, baseY + arcOffset);

        // Rotasyon (eğim)
        if (t < 0.5f)
            Rotation = Mathf.DegToRad(-30);  // Yukarı çıkarken
        else
            Rotation = Mathf.DegToRad(30);   // Aşağı inerken

        // Hedefe ulaştıysa yok ol
        if (t >= 1.0f)
        {
            GD.Print("[WIZARD_PROJ] 🎯 Hedefe ulaştı!");
            QueueFree();
        }
    }

    protected override void OnBodyEntered(Node2D body)
    {
        if (!isSetupDone)
        {
            GD.Print("[WIZARD_PROJ] ⚠️ Setup yapılmadan çarpışma!");
            return;
        }

        GD.Print($"[WIZARD_PROJ] 💥 Çarpışma: {body.Name} (Groups: {string.Join(", ", body.GetGroups())})");

        if (body.IsInGroup("player") || body.IsInGroup("Player"))
        {
            GD.Print($"[WIZARD_PROJ] 🎯 Player'a çarptı! Hasar: {Damage}");

            if (body.HasMethod("TakeDamage"))
            {
                body.Call("TakeDamage", Damage);
            }

            QueueFree();
        }
        else if (body.IsInGroup("ground") || body.IsInGroup("Ground") || body is TileMap || body is StaticBody2D)
        {
            GD.Print("[WIZARD_PROJ] 🌍 Zemine çarptı!");
            QueueFree();
        }
    }
}