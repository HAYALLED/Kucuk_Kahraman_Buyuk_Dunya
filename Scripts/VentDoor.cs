using Godot;

public partial class VentDoor : Area2D
{
    private AnimatedSprite2D anim;
    private bool isOpen = false;

    public override void _Ready()
    {
        anim = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        anim?.Play("default_close");
    }

    // BirdRouteManager'ın OnEnterMethod / OnExitMethod buraya bağlanır
    public void Activate(bool open)
    {
        if (open == isOpen) return;
        isOpen = open;

        if (anim == null) return;

        if (open)
        {
            anim.Play("opening");
            anim.AnimationFinished += OnOpeningFinished;
        }
        else
        {
            anim.Play("closing");
            anim.AnimationFinished += OnClosingFinished;
        }
    }

    private void OnOpeningFinished()
    {
        anim.AnimationFinished -= OnOpeningFinished;
        if (isOpen) anim.Play("default_open");
    }

    private void OnClosingFinished()
    {
        anim.AnimationFinished -= OnClosingFinished;
        if (!isOpen) anim.Play("default_close");
    }
}