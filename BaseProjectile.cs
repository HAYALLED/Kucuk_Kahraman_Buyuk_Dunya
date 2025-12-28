using Godot;

public partial class BaseProjectile : Area2D
{
    [Export] public float Speed = 400.0f;
    [Export] public int Damage = 1;
    [Export] public float Lifetime = 3.0f;  // Kaç saniye sonra yok olur

    protected int direction = 1;  // 1 = sağ, -1 = sol
    protected bool canStun = false;
    protected float stunDuration = 2.0f;
    protected int stunHitCount = 3;
    protected float stunTimeWindow = 10.0f;

    protected AnimatedSprite2D animatedSprite;

    public override void _Ready()
    {
        animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

        // Collision sinyali
        BodyEntered += OnBodyEntered;

        // Lifetime sonunda yok ol
        GetTree().CreateTimer(Lifetime).Timeout += () => QueueFree();

        // Yöne göre sprite çevir
        if (animatedSprite != null)
        {
            animatedSprite.FlipH = direction < 0;
        }
    }
    public void SetStunHitCount(int count)
    {
        stunHitCount = count;
        GD.Print($"[PROJECTILE] StunHitCount: {stunHitCount}");
    }
    public virtual void Setup(int dir, int dmg, bool stun = false, float stunDur = 2.0f)
    {
        direction = dir;
        Damage = dmg;
        canStun = stun;
        stunDuration = stunDur;

        // Sprite yönünü güncelle
        if (animatedSprite != null)
        {
            animatedSprite.FlipH = direction < 0;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Hareket - override edilebilir
        Move((float)delta);
    }

    protected virtual void Move(float delta)
    {
        Position += new Vector2(direction * Speed * delta, 0);
    }

    protected virtual void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("enemy"))
        {
            HitEnemy(body);
        }
        else if (body.IsInGroup("ground") || body.IsInGroup("wall"))
        {
            HitWall();
        }
    }

    protected virtual void HitEnemy(Node2D enemy)
    {
        // Hasar ver
        if (enemy.HasMethod("TakeDamage"))
        {
            enemy.Call("TakeDamage", Damage);
            GD.Print($"[PROJECTILE] Düşmana {Damage} hasar verildi!");
        }

        // Stun kontrolü
        if (canStun && enemy.HasMethod("ApplyStun"))
        {
            enemy.Call("ApplyStun", stunDuration);
            GD.Print($"[PROJECTILE] Düşman {stunDuration}sn stunlandı!");
        }

        // Yok ol
        QueueFree();
    }

    protected virtual void HitWall()
    {
        // Duvara çarpınca yok ol
        QueueFree();
    }
}