using Godot;
using System;

public partial class PointsFood : Area2D
{
    private AnimatedSprite2D animatedSprite;
    private int pointValue = 1;  // Her Food 1 puan

    public override void _Ready()
    {
        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

        // Random frame seç (0-4 arası, 5 varyant)
        int maxFrame = animatedSprite.SpriteFrames.GetFrameCount("points") - 1;
        int randomFrame = GD.RandRange(0, maxFrame);

        animatedSprite.Play("points");
        animatedSprite.Frame = randomFrame;
        animatedSprite.Pause();  // Sabit kalsın

        // Collision sinyali
        BodyEntered += OnBodyEntered;

        AddToGroup("points");

    }

    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            // Player'a metal puanı ekle
            if (body.HasMethod("AddFood"))
            {
                body.Call("AddFood", pointValue);
            }

            // Toplama efekti (opsiyonel)
            // PlayPickupEffect();

            CallDeferred("queue_free");
        }
    }

    public void Collect(Player_controller player)
    {
        if (player.HasMethod("AddFood"))
        {
            player.Call("AddFood", pointValue);
        }

        CallDeferred("queue_free");
    }


    public void CollectByDrone(Player_controller player)
    {
        Collect(player);
    }
    public override void _Process(double delta)
    {
        // Boş bırakabilirsin veya hover efekti ekleyebilirsin
    }
}
