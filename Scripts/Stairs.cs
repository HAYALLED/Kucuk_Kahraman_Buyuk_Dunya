using Godot;

public partial class Stairs : Area2D
{
    [ExportGroup("Hedef Merdiven")]
    [Export] public NodePath TargetStairs { get; set; }

    [ExportGroup("Görsel")]
    [Export] public bool ShowInteractionHint { get; set; } = true;

    private bool playerInRange = false;
    private Node2D player;
    private Stairs resolvedTarget;

    private AnimatedSprite2D animatedSprite;
    private Label interactionLabel;

    public override void _Ready()
    {
        animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        interactionLabel = GetNodeOrNull<Label>("InteractionLabel");

        AddToGroup("interactable");
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        CollisionLayer = 0;
        CollisionMask = 2;

        if (interactionLabel != null)
            interactionLabel.Visible = false;

        CallDeferred(nameof(ResolveTarget));
    }

    private void ResolveTarget()
    {
        if (TargetStairs == null || TargetStairs.IsEmpty)
        {
            GD.PrintErr($"[STAIRS] ❌ {Name}: TargetStairs boş! Inspector'dan hedef bağla.");
            return;
        }

        resolvedTarget = GetNodeOrNull<Stairs>(TargetStairs);

        if (resolvedTarget == null)
            GD.PrintErr($"[STAIRS] ❌ {Name}: Hedef bulunamadı veya Stairs değil: {TargetStairs}");
        else
            GD.Print($"[STAIRS] ✅ {Name} → {resolvedTarget.Name} bağlandı.");
    }

    public override void _Process(double delta)
    {
        if (playerInRange && Input.IsActionJustPressed("interaction"))
            TryTeleport();
    }

    private void TryTeleport()
    {
        if (resolvedTarget == null)
        {
            GD.PrintErr($"[STAIRS] ❌ {Name}: Bağlı hedef yok!");
            return;
        }

        if (player == null || !IsInstanceValid(player)) return;

        player.GlobalPosition = resolvedTarget.GlobalPosition;
        GD.Print($"[STAIRS] ✅ Oyuncu {Name} → {resolvedTarget.Name} ışınlandı.");
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        player = body;
        playerInRange = true;
        if (ShowInteractionHint && interactionLabel != null)
            interactionLabel.Visible = true;
    }

    private void OnBodyExited(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        player = null;
        playerInRange = false;
        if (interactionLabel != null)
            interactionLabel.Visible = false;
    }
}