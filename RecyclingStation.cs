using Godot;

public partial class RecyclingStation : Area2D
{
    [Export] public PackedScene MinigameScene;  // Minigame UI scene'i

    private bool playerInRange = false;
    private Node2D player;
    private Label interactionLabel;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        // "E tuşuna bas" yazısı (opsiyonel)
        interactionLabel = GetNodeOrNull<Label>("InteractionLabel");
        if (interactionLabel != null)
            interactionLabel.Visible = false;
    }

    public override void _Process(double delta)
    {
        // E tuşuna basılınca minigame başlat
        if (playerInRange && Input.IsActionJustPressed("interaction"))
        {
            StartMinigame();
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInRange = true;
            player = body;

            if (interactionLabel != null)
                interactionLabel.Visible = true;

            GD.Print("[RECYCLE] Player yaklaştı - E'ye bas!");
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body.IsInGroup("player"))
        {
            playerInRange = false;
            player = null;

            if (interactionLabel != null)
                interactionLabel.Visible = false;
        }
    }

    private void StartMinigame()
    {
        if (MinigameScene == null)
        {
            GD.PrintErr("[RECYCLE] MinigameScene atanmamış!");
            return;
        }

        // Player'dan toplanan çöpleri al
        int[] collectedTrash = new int[5];
        if (player != null && player.HasMethod("GetAllPoints"))
        {
            collectedTrash = (int[])player.Call("GetAllPoints");
        }

        // Minigame UI'ı oluştur
        var minigame = MinigameScene.Instantiate<CanvasLayer>();
        GetTree().CurrentScene.AddChild(minigame);

        // Minigame'e çöp verilerini gönder
        if (minigame.HasMethod("Setup"))
        {
            minigame.Call("Setup", collectedTrash, player);
        }

        // Oyunu duraklat (opsiyonel)
        GetTree().Paused = true;
        minigame.ProcessMode = ProcessModeEnum.Always;

        GD.Print("[RECYCLE] Minigame başladı!");
    }
}