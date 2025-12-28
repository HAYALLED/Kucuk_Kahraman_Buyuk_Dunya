using Godot;

public partial class RocketProjectile : BaseProjectile
{
    [Export] public float TurnSpeed = 5.0f;  // Dönüş hızı
    [Export] public float DetectionRange = 300.0f;  // Hedef arama mesafesi

    private Node2D target;

    public override void _Ready()
    {
        base._Ready();

        // En yakın düşmanı bul
        FindTarget();

        if (animatedSprite != null && animatedSprite.SpriteFrames.HasAnimation("default"))
        {
            animatedSprite.Play("default");
        }
    }

    private void FindTarget()
    {
        var enemies = GetTree().GetNodesInGroup("enemy");
        float closestDistance = DetectionRange;

        foreach (var enemy in enemies)
        {
            if (enemy is Node2D enemyNode)
            {
                float distance = GlobalPosition.DistanceTo(enemyNode.GlobalPosition);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    target = enemyNode;
                }
            }
        }

        if (target != null)
        {
            GD.Print($"[ROCKET] Hedef bulundu: {target.Name}");
        }
    }

    protected override void Move(float delta)
    {
        if (target != null && IsInstanceValid(target))
        {
            // Hedefe doğru yönlen
            Vector2 dirToTarget = (target.GlobalPosition - GlobalPosition).Normalized();
            Vector2 currentDir = new Vector2(direction, 0);

            // Yumuşak dönüş
            Vector2 newDir = currentDir.Lerp(dirToTarget, TurnSpeed * delta).Normalized();

            GlobalPosition += newDir * Speed * delta;

            // Sprite rotasyonu
            Rotation = newDir.Angle();
        }
        else
        {
            // Hedef yoksa düz git
            base.Move(delta);
        }
    }
}
