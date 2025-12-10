using Godot;

public partial class BatarangProjectile : BaseProjectile
{
    [Export] public bool ReturnToPlayer = false;  // Geri dönüş özelliği
    private bool isReturning = false;
    private Node2D playerRef;

    public override void _Ready()
    {
        base._Ready();

        // Player referansı al
        playerRef = GetTree().GetFirstNodeInGroup("player") as Node2D;

        if (animatedSprite != null && animatedSprite.SpriteFrames.HasAnimation("default"))
        {
            animatedSprite.Play("default");
        }
    }

    protected override void Move(float delta)
    {
        if (isReturning && playerRef != null)
        {
            // Player'a doğru dön
            Vector2 dirToPlayer = (playerRef.GlobalPosition - GlobalPosition).Normalized();
            GlobalPosition += dirToPlayer * Speed * delta;

            // Player'a yaklaştıysa yok ol
            if (GlobalPosition.DistanceTo(playerRef.GlobalPosition) < 30)
            {
                QueueFree();
            }
        }
        else
        {
            // Normal hareket
            base.Move(delta);
        }
    }

    protected override void HitWall()
    {
        if (ReturnToPlayer && !isReturning)
        {
            isReturning = true;
            GD.Print("[BATARANG] Geri dönüyor!");
        }
        else
        {
            base.HitWall();
        }
    }
}