using Godot;

public partial class Trambolin : Area2D
{
    [Export] public float BounceForce = 800.0f;  // Zıplatma gücü (her trambolin için farklı ayarlanabilir)
    [Export] public float MinBounceForce = 400.0f;  // Minimum zıplatma
    [Export] public float MaxBounceForce = 1500.0f; // Maximum zıplatma

    private AnimatedSprite2D animatedSprite;

    public override void _Ready()
    {
        animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

        // Collision sinyalini bağla
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        // Sadece player'ı zıplat
        if (body.IsInGroup("player") && body is CharacterBody2D player)
        {
            // Oyuncuyu yukarı fırlat
            Vector2 velocity = player.Velocity;
            velocity.Y = -BounceForce;  // Negatif = yukarı
            player.Velocity = velocity;

            GD.Print($"[TRAMBOLIN] Zıplatma! Güç: {BounceForce}");

            // Animasyon varsa oynat
            if (animatedSprite != null && animatedSprite.SpriteFrames.HasAnimation("bounce"))
            {
                animatedSprite.Play("bounce");

                // Animasyon bitince default'a dön
                animatedSprite.AnimationFinished += () =>
                {
                    if (animatedSprite.SpriteFrames.HasAnimation("default"))
                        animatedSprite.Play("default");
                };
            }
        }
    }
}
