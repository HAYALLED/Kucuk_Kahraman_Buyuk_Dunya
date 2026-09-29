using Godot;

public partial class RandomCostume : Area2D
{
    private AnimatedSprite2D animatedSprite;
    private bool _alreadyCollected = false;

    [ExportGroup("Kostüm Ayarları")]
    [Export] public CostumeResource[] AvailableCostumes;
    [Export] public SpriteFrames Sprites;

    public override void _Ready()
    {
        animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

        if (animatedSprite != null)
            animatedSprite.Play();

        if (AvailableCostumes == null || AvailableCostumes.Length == 0)
            GD.PrintErr("[RandomCostume] ❌ AvailableCostumes BOŞ! Inspector'da ayarla!");

        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_alreadyCollected) return;

        if (body.IsInGroup("player") && body is Player_controller player)
        {
            _alreadyCollected = true;
            CallDeferred(nameof(ProcessCostumePickup), player);
        }
    }

    private void ProcessCostumePickup(Player_controller player)
    {
        if (player == null || !IsInstanceValid(player))
        {
            GD.PrintErr("[RandomCostume] ❌ Player geçersiz!");
            QueueFree();
            return;
        }

        GiveCostumeToPlayer(player);
        QueueFree();
    }

    private void GiveCostumeToPlayer(Player_controller player)
    {
        if (AvailableCostumes == null || AvailableCostumes.Length == 0)
        {
            GD.PrintErr("[RandomCostume] ❌ AvailableCostumes boş!");
            return;
        }

        int randomIndex = GD.RandRange(0, AvailableCostumes.Length - 1);
        CostumeResource newCostume = AvailableCostumes[randomIndex];

        if (newCostume == null)
        {
            GD.PrintErr($"[RandomCostume] ❌ Index {randomIndex}'deki kostüm null!");
            return;
        }

        int existingSlotIndex = -1;
        for (int i = 0; i < player.CostumeSlots.Length; i++)
        {
            if (player.CostumeSlots[i] != null &&
                player.CostumeSlots[i].CostumeName == newCostume.CostumeName)
            {
                existingSlotIndex = i;
                break;
            }
        }

        if (existingSlotIndex >= 0)
            player.HealCostumeSlot(existingSlotIndex);
        else
            AddOrSwapCostume(player, newCostume);
    }

    private void AddOrSwapCostume(Player_controller player, CostumeResource newCostume)
    {
        for (int i = 0; i < player.CostumeSlots.Length; i++)
        {
            if (player.CostumeSlots[i] == null)
            {
                player.SetCostumeAndEquip(i, newCostume);
                return;
            }
        }

        // Tüm slotlar dolu — aktif kostümle değiştir
        int activeSlotIndex = player.GetCurrentCostumeIndex();
        if (activeSlotIndex < 0) activeSlotIndex = 0;
        player.SetCostumeAndEquip(activeSlotIndex, newCostume);
    }
}