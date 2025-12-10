using Godot;
using System;

public partial class TrashProjectile : BaseProjectile
{
    public override void _Ready()
    {
        base._Ready();

        GetTree().CreateTimer(7.0f).Timeout += QueueFree;


        if (animatedSprite != null && animatedSprite.SpriteFrames.HasAnimation("default"))
        {
            animatedSprite.Play("default");
        }
    }
    protected override void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            // Player'a hasar ver
            if (body.HasMethod("TakeDamage"))
            {
                body.Call("TakeDamage", Damage);
            }
            QueueFree();
        }
        else if (body.IsInGroup("Ground"))
        {
            QueueFree();
        }
    }


}
