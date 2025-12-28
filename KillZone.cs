using Godot;
using System;

public partial class KillZone : Area2D
{
    [Export] public NodePath FaderPath;
    private ScreenFader _fader;
    private bool _triggered;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;

        if (FaderPath != null && !FaderPath.IsEmpty)
        {
            _fader = GetNodeOrNull<ScreenFader>(FaderPath);
        }

        if (_fader == null)
        {
            _fader = GetTree().CurrentScene.GetNodeOrNull<ScreenFader>("ScreenFader");
        }

        if (_fader == null)
        {
            _fader = FindScreenFader(GetTree().CurrentScene);
        }

        if (_fader != null)
        {
            GD.Print("[KILLZONE] ✅ ScreenFader bulundu!");
        }
        else
        {
            GD.Print("[KILLZONE] ⚠️ ScreenFader yok, direkt restart");
        }
    }

    private ScreenFader FindScreenFader(Node node)
    {
        if (node is ScreenFader fader)
            return fader;

        foreach (Node child in node.GetChildren())
        {
            var result = FindScreenFader(child);
            if (result != null)
                return result;
        }

        return null;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player") || _triggered) return;

        _triggered = true;
        GD.Print($"[KILLZONE] 💀 {body.Name} öldü!");

        // ✅ Ölümde Meta'ları temizle
        ClearPlayerMeta();

        SetDeferred(PropertyName.Monitoring, false);

        if (_fader != null)
        {
            _fader.FadeOutThenRestart();
        }
        else
        {
            Callable.From(() =>
            {
                var path = GetTree().CurrentScene.SceneFilePath;
                GetTree().ChangeSceneToFile(path);
            }).CallDeferred();
        }
    }
    private void ClearPlayerMeta()
    {
        var root = GetTree().Root;

        // Costume & health
        if (root.HasMeta("SavedCostume"))
            root.RemoveMeta("SavedCostume");
        if (root.HasMeta("SavedHealth"))
            root.RemoveMeta("SavedHealth");
        if (root.HasMeta("SavedMaxHealth"))
            root.RemoveMeta("SavedMaxHealth");

        // Trash
        if (root.HasMeta("SavedTrash_Metal"))
            root.RemoveMeta("SavedTrash_Metal");
        if (root.HasMeta("SavedTrash_Glass"))
            root.RemoveMeta("SavedTrash_Glass");
        if (root.HasMeta("SavedTrash_Plastic"))
            root.RemoveMeta("SavedTrash_Plastic");
        if (root.HasMeta("SavedTrash_Food"))
            root.RemoveMeta("SavedTrash_Food");
        if (root.HasMeta("SavedTrash_Wood"))
            root.RemoveMeta("SavedTrash_Wood");

        // ✅ YENİ: Secret level completion flag'lerini de temizle!
        if (root.HasMeta("CurrentSecretID"))
        {
            string secretID = (string)root.GetMeta("CurrentSecretID");
            if (root.HasMeta($"SecretCompleted_{secretID}"))
            {
                root.RemoveMeta($"SecretCompleted_{secretID}");
                GD.Print($"[KILLZONE] 🔓 SecretCompleted_{secretID} temizlendi!");
            }
            root.RemoveMeta("CurrentSecretID");
        }

        // ✅ Tüm secret completion flag'lerini temizle
        var metaList = root.GetMetaList();
        foreach (string metaKey in metaList)
        {
            if (metaKey.StartsWith("SecretCompleted_"))
            {
                root.RemoveMeta(metaKey);
                GD.Print($"[KILLZONE] 🔓 {metaKey} temizlendi!");
            }
        }

        if (root.HasMeta("ReturnFromSecret"))
            root.RemoveMeta("ReturnFromSecret");

        GD.Print("[KILLZONE] 🧹 Tüm Meta'lar temizlendi (ölüm + secret flags)");
    }
}