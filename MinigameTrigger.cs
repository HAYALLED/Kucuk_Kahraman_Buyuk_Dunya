using Godot;

public partial class MinigameTrigger : Area2D
{
    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            var level = GetTree().CurrentScene;
            if (level.HasMethod("StartMinigame"))
            {
                level.Call("StartMinigame");
            }
        }
    }
}