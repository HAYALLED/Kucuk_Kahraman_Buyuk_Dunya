using Godot;
using System;

public partial class ScreenFader : CanvasLayer
{
    [Export] public AnimationPlayer AnimPlayer;
    [Export] public ColorRect Overlay;

    public override void _Ready()
    {
        AnimPlayer ??= GetNode<AnimationPlayer>("AnimationPlayer");
        Overlay ??= GetNode<ColorRect>("Overlay");
    }

    public void FadeOutThenRestart()
    {
        void Finished(StringName _)
        {
            AnimPlayer.AnimationFinished -= Finished;

            var path = GetTree().CurrentScene.SceneFilePath;
            Callable.From(() => GetTree().ChangeSceneToFile(path)).CallDeferred();
        }

        AnimPlayer.AnimationFinished += Finished;
        AnimPlayer.Play("fade_out");

  
    }
}
