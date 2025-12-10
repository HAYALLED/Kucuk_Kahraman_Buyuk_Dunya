using Godot;

public partial class RecycleBin : Control
{
    [Export] public int BinType = 0;  // 0=Cam, 1=Organik, 2=Kağıt, 3=Plastik
    [Export] public int[] AcceptedTrashTypes;  // Kabul edilen çöp türleri


    public override void _Ready()
    {

        AddToGroup("recycle_bin");
    }

    public bool AcceptsTrash(int trashType)
    {
        foreach (int accepted in AcceptedTrashTypes)
        {
            if (accepted == trashType)
                return true;
        }
        return false;
    }

    public async void FlashColor(Color color)
    {
        var originalColor = Modulate;
        Modulate = color;
        await ToSignal(GetTree().CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);
        Modulate = originalColor;
    }
}