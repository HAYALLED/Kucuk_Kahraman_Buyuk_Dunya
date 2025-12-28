using Godot;

public partial class DraggableTrash : Control
{
    [Signal] public delegate void DroppedOnBinEventHandler(int binIndex, Control trash);

    private bool isDragging = false;
    private Vector2 dragOffset;
    private Vector2 originalPosition;
    private TextureRect icon;
    private Label nameLabel;

    public int TrashType { get; private set; } = 0;  // 0=Metal, 1=Cam, 2=Plastik, 3=Yemek, 4=Kağıt

    public override void _Ready()
    {
        icon = GetNode<TextureRect>("Icon");
        nameLabel = GetNode<Label>("NameLabel");

        originalPosition = Position;

        // Mouse olayları
        GuiInput += OnGuiInput;
    }

    public void Setup(int trashType, string name, Texture2D texture = null)
    {
        TrashType = trashType;
        nameLabel.Text = name;

        if (texture != null)
            icon.Texture = texture;
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    // Tutmaya başla
                    isDragging = true;
                    dragOffset = GlobalPosition - GetGlobalMousePosition();
                    ZIndex = 100;  // En üste getir
                }
                else
                {
                    // Bırak
                    isDragging = false;
                    ZIndex = 0;
                    CheckDropZone();
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        if (isDragging)
        {
            GlobalPosition = GetGlobalMousePosition() + dragOffset;
        }
    }

    private void CheckDropZone()
    {
        // Hangi kutunun üstüne bırakıldı?
        var bins = GetTree().GetNodesInGroup("recycle_bin");

        foreach (var bin in bins)
        {
            if (bin is Control binControl)
            {
                Rect2 binRect = binControl.GetGlobalRect();

                if (binRect.HasPoint(GetGlobalMousePosition()))
                {
                    // Bu kutunun üstüne bırakıldı
                    int binIndex = binControl.GetIndex();
                    EmitSignal(SignalName.DroppedOnBin, binIndex, this);
                    return;
                }
            }
        }

        // Hiçbir kutunun üstüne bırakılmadı - geri dön
        ResetPosition();
    }

    public void ResetPosition()
    {
        Position = originalPosition;
    }

    public void SetOriginalPosition(Vector2 pos)
    {
        originalPosition = pos;
        Position = pos;
    }
}