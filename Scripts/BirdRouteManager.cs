using Godot;

// Path2D node'una bu scripti ekle.
// Tek rota yeterliyse sade Path2D kullan — spawnerlar ikisiyle de çalışır.
public partial class BirdRouteManager : Path2D
{
    [ExportGroup("Bağlı Rotalar")]
    [Export] public Path2D[] ConnectedPaths;
    [Export] public float BirdSpeed = 80f;
    [Export] public bool Forward = true;
    [Export] public float EndThreshold = 10f;

    [ExportGroup("Giriş Tetikleyicileri")]
    [Export] public Node[] OnEnterTriggers;
    [Export] public string OnEnterMethod = "Activate";
    [Export] public bool OnEnterParam = true;

    [ExportGroup("Çıkış Tetikleyicileri")]
    [Export] public Node[] OnExitTriggers;
    [Export] public string OnExitMethod = "Activate";
    [Export] public bool OnExitParam = false;

    [Signal] public delegate void BirdEnteredPathEventHandler();
    [Signal] public delegate void BirdExitedPathEventHandler();

    private int activeBirdCount = 0;
    public int ActiveBirdCount => activeBirdCount;

    public void FireEnter()
    {
        activeBirdCount++;
        EmitSignal(SignalName.BirdEnteredPath);
        CallTriggers(OnEnterTriggers, OnEnterMethod, OnEnterParam);
    }

    public void FireExit()
    {
        activeBirdCount = Mathf.Max(0, activeBirdCount - 1);
        EmitSignal(SignalName.BirdExitedPath);
        CallTriggers(OnExitTriggers, OnExitMethod, OnExitParam);
    }

    public Path2D GetNextPath(Path2D exclude = null)
    {
        if (ConnectedPaths == null || ConnectedPaths.Length == 0) return null;

        var available = new Godot.Collections.Array<Path2D>();
        foreach (var p in ConnectedPaths)
            if (p != null && IsInstanceValid(p) && p != exclude)
                available.Add(p);

        if (available.Count == 0)
            foreach (var p in ConnectedPaths)
                if (p != null && IsInstanceValid(p)) available.Add(p);

        if (available.Count == 0) return null;
        return available[GD.RandRange(0, available.Count - 1)];
    }

    private void CallTriggers(Node[] targets, string method, bool param)
    {
        if (targets == null) return;
        foreach (var node in targets)
        {
            if (node == null || !IsInstanceValid(node)) continue;
            if (node.HasMethod(method))
                node.Call(method, param);
            else
                GD.PrintErr($"[BirdRouteManager] ❌ {node.Name} node'unda '{method}' metodu yok!");
        }
    }
}