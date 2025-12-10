using Godot;
using System;

public partial class Background : Sprite2D
{
    private Camera2D _cam;

    public override void _Ready()
    {
        _cam = GetViewport().GetCamera2D();

        Centered = true;

        if (Texture is Texture2D tex)
        {
            Vector2 vp = GetViewportRect().Size;
            Vector2 texSize = tex.GetSize();
            if (texSize.X > 0 && texSize.Y > 0)
                Scale = vp / texSize;
        }
    }

    public override void _Process(double delta)
    {
        if (_cam == null)
            return;

        GlobalPosition = _cam.GlobalPosition.Round();

    }
}