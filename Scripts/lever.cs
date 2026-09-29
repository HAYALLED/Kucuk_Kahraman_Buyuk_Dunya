using Godot;
using System.Collections.Generic;

public partial class lever : Area2D
{
    [ExportGroup("Hedef Kapılar")]
    [Export] public Godot.Collections.Array<NodePath> TargetDoors { get; set; } = new();
    [Export] public string TargetMethod { get; set; } = "Activate";

    [ExportGroup("Lever Davranışı")]
    [Export] public bool IsToggle { get; set; } = true;
    [Export] public bool StartsActivated { get; set; } = false;
    [Export] public float CooldownTime { get; set; } = 0.5f;

    [ExportGroup("Görsel")]
    [Export] public bool ShowInteractionHint { get; set; } = true;

    private bool isActivated = false;
    private bool playerInRange = false;
    private bool isOnCooldown = false;
    private bool isAnimating = false;
    private bool hasSprite = false;

    private List<Node> resolvedDoors = new();
    private AnimatedSprite2D animatedSprite;
    private Label interactionLabel;

    public override void _Ready()
    {
        animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        interactionLabel = GetNodeOrNull<Label>("InteractionLabel");
        hasSprite = animatedSprite != null;

        AddToGroup("interactable");
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        CollisionLayer = 0;
        CollisionMask = 2;

        isActivated = StartsActivated;

        if (hasSprite)
        {
            PlayAnim(isActivated ? "default_open" : "default_close");
            animatedSprite.AnimationFinished += OnAnimationFinished;
        }

        if (interactionLabel != null)
            interactionLabel.Visible = false;

        CallDeferred(nameof(ResolveDoors));
    }

    private void ResolveDoors()
    {
        resolvedDoors.Clear();

        if (TargetDoors == null || TargetDoors.Count == 0)
        {
            GD.PrintErr("[LEVER] ❌ TargetDoors boş! Inspector'dan kapı bağla.");
            return;
        }

        foreach (var path in TargetDoors)
        {
            if (path == null || path.IsEmpty) continue;

            var node = GetNodeOrNull<Node>(path);

            if (node == null)
            {
                GD.PrintErr($"[LEVER] ❌ Path çözülemedi: {path}");
                continue;
            }

            if (!node.HasMethod(TargetMethod))
            {
                GD.PrintErr($"[LEVER] ❌ '{node.Name}' üzerinde '{TargetMethod}' metodu yok!");
                continue;
            }

            resolvedDoors.Add(node);
            GD.Print($"[LEVER] ✅ Kapı bağlandı: {node.Name}");
        }

        GD.Print($"[LEVER] 📋 {resolvedDoors.Count}/{TargetDoors.Count} kapı hazır.");
    }

    public override void _Process(double delta)
    {
        if (playerInRange && !isAnimating && Input.IsActionJustPressed("interaction"))
            TryActivate();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        playerInRange = true;
        if (ShowInteractionHint && interactionLabel != null)
            interactionLabel.Visible = true;
    }

    private void OnBodyExited(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        playerInRange = false;
        if (interactionLabel != null)
            interactionLabel.Visible = false;
    }

    public void Interact(Node2D interactor) => TryActivate();
    public void OnInteract(Node2D interactor) => TryActivate();

    private void TryActivate()
    {
        if (isOnCooldown || isAnimating) return;
        if (!IsToggle && isActivated) return;

        isActivated = IsToggle ? !isActivated : true;

        if (hasSprite)
        {
            isAnimating = true;
            PlayAnim(isActivated ? "opening" : "closing");
        }

        TriggerDoors();

        isOnCooldown = true;
        GetTree().CreateTimer(CooldownTime).Timeout += () => isOnCooldown = false;
    }

    private void TriggerDoors()
    {
        if (resolvedDoors.Count == 0)
        {
            GD.PrintErr("[LEVER] ❌ Bağlı kapı yok!");
            return;
        }

        foreach (var door in resolvedDoors)
        {
            if (!IsInstanceValid(door)) continue;
            door.Call(TargetMethod, isActivated);
            GD.Print($"[LEVER] ✅ {door.Name}.{TargetMethod}({isActivated})");
        }
    }

    private void OnAnimationFinished()
    {
        if (!hasSprite) return;
        string anim = animatedSprite.Animation;
        if (anim == "opening") { PlayAnim("default_open"); isAnimating = false; }
        else if (anim == "closing") { PlayAnim("default_close"); isAnimating = false; }
    }

    private void PlayAnim(string animName)
    {
        if (animatedSprite?.SpriteFrames?.HasAnimation(animName) == true)
            animatedSprite.Play(animName);
    }

    public bool IsLeverActivated() => isActivated;

    public void ForceActivate(bool state)
    {
        isActivated = state;
        if (hasSprite)
        {
            isAnimating = true;
            PlayAnim(isActivated ? "opening" : "closing");
        }
        TriggerDoors();
    }
}