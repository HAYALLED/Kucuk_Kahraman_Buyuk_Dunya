using Godot;
using System;

public partial class PointsBottleHolderPoints : Area2D
{
    private AnimatedSprite2D animatedSprite;

    [Export] public int MinPoints = 3;  // Minimum puan
    [Export] public int MaxPoints = 7;  // Maximum puan

    public override void _Ready()
    {
        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        animatedSprite.Play("BigPoints");
        animatedSprite.Frame = 0;
        animatedSprite.Pause();

        BodyEntered += OnBodyEntered;

    }

    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            // Random toplam puan (3-7 arası)
            int totalPoints = GD.RandRange(MinPoints, MaxPoints);

            // Kategorilere random dağıt
            int plastic = 0;
            int metal = 0;
            int glass = 0;

            for (int i = 0; i < totalPoints; i++)
            {
                int category = GD.RandRange(1, 3);  // 1, 2 veya 3

                switch (category)
                {
                    case 1:
                        plastic++;
                        break;
                    case 2:
                        metal++;
                        break;
                    case 3:
                        glass++;
                        break;
                }
            }

            // Player'a puanları ekle
            if (plastic > 0 && body.HasMethod("AddPlastic"))
            {
                body.Call("AddPlastic", plastic);
            }

            if (metal > 0 && body.HasMethod("AddMetal"))
            {
                body.Call("AddMetal", metal);
            }

            if (glass > 0 && body.HasMethod("AddGlass"))
            {
                body.Call("AddGlass", glass);
            }


            QueueFree();
        }
    }

    public override void _Process(double delta)
    {
    }
}
