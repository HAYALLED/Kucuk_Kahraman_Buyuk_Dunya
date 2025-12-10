using Godot;
using System;

public partial class PointsGoldTrash : Area2D
{
    private AnimatedSprite2D animatedSprite;

    [Export] public int MinPoints = 10;
    [Export] public int MaxPoints = 25;

    private Node2D secretLevel = null;
    private bool isCollected = false;

    public override void _Ready()
    {
        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        animatedSprite.Play("BigPoints");
        animatedSprite.Frame = 0;
        animatedSprite.Pause();

        BodyEntered += OnBodyEntered;
        AddToGroup("points");

        GD.Print($"[GOLD TRASH] ✅ {Name} hazır");
    }

    public void SetSecretLevel(Node2D level)
    {
        secretLevel = level;
        GD.Print($"[GOLD TRASH] Secret level referansı alındı");
    }

    private void OnBodyEntered(Node2D body)
    {
        if (isCollected || !body.IsInGroup("player")) return;

        isCollected = true;

        // Random puan dağıt
        int totalPoints = GD.RandRange(MinPoints, MaxPoints);
        int plastic = 0, metal = 0, glass = 0, food = 0, wood = 0;

        for (int i = 0; i < totalPoints; i++)
        {
            int category = GD.RandRange(1, 5);
            switch (category)
            {
                case 1: plastic++; break;
                case 2: metal++; break;
                case 3: glass++; break;
                case 4: food++; break;
                case 5: wood++; break;
            }
        }

        // Player'a ekle
        if (plastic > 0 && body.HasMethod("AddPlastic"))
            body.Call("AddPlastic", plastic);
        if (metal > 0 && body.HasMethod("AddMetal"))
            body.Call("AddMetal", metal);
        if (glass > 0 && body.HasMethod("AddGlass"))
            body.Call("AddGlass", glass);
        if (food > 0 && body.HasMethod("AddFood"))
            body.Call("AddFood", food);
        if (wood > 0 && body.HasMethod("AddWood"))
            body.Call("AddWood", wood);

        GD.Print($"[GOLD TRASH] 🗑️ Toplandı! P:{plastic} M:{metal} G:{glass} F:{food} W:{wood}");

        // ✅ Secret level'e bildir
        if (secretLevel != null && secretLevel.HasMethod("OnPointCollected"))
        {
            secretLevel.Call("OnPointCollected");
        }

        // ✅ CRITICAL FIX: CallDeferred ile yok et
        CallDeferred("queue_free");
    }
}