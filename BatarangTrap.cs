using Godot;

public partial class BatarangTrap : Area2D
{
    [Export] public int Damage = 1;
    [Export] public float StunDuration = 3.0f;
    [Export] public float Lifetime = 30.0f;  // Ne kadar süre aktif kalır

    private AnimatedSprite2D animatedSprite;
    private bool isTriggered = false;

    public override void _Ready()
    {
        animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

        BodyEntered += OnBodyEntered;

        // Lifetime sonunda yok ol
        GetTree().CreateTimer(Lifetime).Timeout += () =>
        {
            if (!isTriggered)
            {
                QueueFree();
            }
        };

        // Idle animasyonu
        if (animatedSprite != null && animatedSprite.SpriteFrames.HasAnimation("idle"))
        {
            animatedSprite.Play("idle");
        }

        GD.Print("[TRAP] Tuzak kuruldu!");
    }

    public void Setup(int dmg, float stunDur)
    {
        Damage = dmg;
        StunDuration = stunDur;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (isTriggered) return;

        if (body.IsInGroup("enemy"))
        {
            Explode(body);
        }
    }

    public void Explode(Node2D target = null)
    {
        isTriggered = true;

        GD.Print("[TRAP] Tuzak patladı!");

        // Hedef varsa hasar ve stun ver
        if (target != null)
        {
            if (target.HasMethod("TakeDamage") && Damage > 0)
            {
                target.Call("TakeDamage", Damage);
            }

            if (target.HasMethod("ApplyStun"))
            {
                target.Call("ApplyStun", StunDuration);
                GD.Print($"[TRAP] Düşman {StunDuration}sn stunlandı!");
            }
        }

        // Patlama animasyonu
        if (animatedSprite != null && animatedSprite.SpriteFrames.HasAnimation("explode"))
        {
            animatedSprite.Play("explode");
            animatedSprite.AnimationFinished += () => QueueFree();
        }
        else
        {
            QueueFree();
        }
    }
}