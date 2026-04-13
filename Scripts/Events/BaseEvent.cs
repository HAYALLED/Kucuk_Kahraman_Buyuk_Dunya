using Godot;

public abstract partial class BaseEvent : Node
{
    [ExportGroup("Event Ayarları")]
    [Export] public bool EnableEvent { get; set; } = true;
    [Export] public string EventName { get; set; } = "Event";

    protected bool isActive = false;
    protected EventManager manager;

    public override void _Ready()
    {
        manager = GetParentOrNull<EventManager>();
        if (manager == null)
            GD.PrintErr($"[{EventName}] ❌ EventManager parent bulunamadı!");

        OnSetup();
    }

    // Alt sınıflar override eder
    protected virtual void OnSetup() { }
    public virtual void OnEventStart() { manager?.ShowEventNotification(EventName); }
    public virtual void OnEventEnd() { }
    public virtual void OnLevelStart() { } // Kuş göçü gibi level başında tetiklenenler için

    public bool IsActive => isActive;

    public virtual void OnApplyOverrides() { }

    public void SetManager(EventManager mgr) { manager = mgr; }
}