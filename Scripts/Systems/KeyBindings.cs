using Godot;
using System.Collections.Generic;

/// <summary>
/// Tuş atama sistemi. Her aksiyon için 2 slot tutar, InputMap'e uygular ve
/// user://keybindings.json dosyasına kaydeder. Varsayılan tuşlar project.godot'taki
/// mevcut InputMap'ten okunur; sadece değiştirilen aksiyonlar kaydedilir.
/// </summary>
public static class KeyBindings
{
    public const int SlotCount = 2;
    private const string SavePath = "user://keybindings.json";

    public readonly struct ActionInfo
    {
        public readonly string Action;
        public readonly string LabelKey;

        public ActionInfo(string action, string labelKey)
        {
            Action = action;
            LabelKey = labelKey;
        }
    }

    public static readonly ActionInfo[] Actions =
    {
        new ActionInfo("move_left", "KEYREBIND_ACTION_MOVE_LEFT"),
        new ActionInfo("move_right", "KEYREBIND_ACTION_MOVE_RIGHT"),
        new ActionInfo("jump", "KEYREBIND_ACTION_JUMP"),
        new ActionInfo("climb", "KEYREBIND_ACTION_CLIMB"),
        new ActionInfo("attack", "KEYREBIND_ACTION_ATTACK"),
        new ActionInfo("throw_projectile", "KEYREBIND_ACTION_THROW"),
        new ActionInfo("special_ability", "KEYREBIND_ACTION_SPECIAL"),
        new ActionInfo("interaction", "KEYREBIND_ACTION_INTERACT"),
        new ActionInfo("teleport", "KEYREBIND_ACTION_TELEPORT"),
        new ActionInfo("plant", "KEYREBIND_ACTION_PLANT"),
        new ActionInfo("costume_1", "KEYREBIND_ACTION_COSTUME_1"),
        new ActionInfo("costume_2", "KEYREBIND_ACTION_COSTUME_2"),
        new ActionInfo("costume_3", "KEYREBIND_ACTION_COSTUME_3"),
        new ActionInfo("camera_up", "KEYREBIND_ACTION_CAMERA_UP"),
        new ActionInfo("camera_down", "KEYREBIND_ACTION_CAMERA_DOWN"),
        new ActionInfo("ui_pause", "KEYREBIND_ACTION_PAUSE"),
    };

    // project.godot'taki orijinal olaylar (reset için birebir geri yüklenir)
    private static readonly Dictionary<string, List<InputEvent>> originalEvents = new Dictionary<string, List<InputEvent>>();
    // Klavye/mouse dışı olaylar (gamepad vb.) - tuş atamasında korunur
    private static readonly Dictionary<string, List<InputEvent>> extraEvents = new Dictionary<string, List<InputEvent>>();
    private static readonly Dictionary<string, InputEvent[]> defaultSlots = new Dictionary<string, InputEvent[]>();
    private static readonly Dictionary<string, InputEvent[]> slots = new Dictionary<string, InputEvent[]>();
    private static readonly HashSet<string> mouseActions = new HashSet<string>();

    private static bool initialized;

    // ========================================
    // BAŞLATMA (güvenle birden çok kez çağrılabilir)
    // ========================================
    public static void Initialize()
    {
        if (initialized)
            return;
        initialized = true;

        foreach (ActionInfo info in Actions)
        {
            string action = info.Action;
            if (!InputMap.HasAction(action))
            {
                GD.PrintErr($"[KEYBIND] InputMap'te aksiyon yok: {action}");
                continue;
            }

            var original = new List<InputEvent>();
            var extras = new List<InputEvent>();
            var binds = new List<InputEvent>();
            var seen = new HashSet<string>();

            foreach (InputEvent e in InputMap.ActionGetEvents(action))
            {
                original.Add((InputEvent)e.Duplicate());

                if (e is InputEventKey || e is InputEventMouseButton)
                {
                    if (e is InputEventMouseButton)
                        mouseActions.Add(action);

                    if (binds.Count < SlotCount && seen.Add(Signature(e)))
                        binds.Add((InputEvent)e.Duplicate());
                }
                else
                {
                    extras.Add((InputEvent)e.Duplicate());
                }
            }

            var defaults = new InputEvent[SlotCount];
            for (int i = 0; i < SlotCount && i < binds.Count; i++)
                defaults[i] = binds[i];

            originalEvents[action] = original;
            extraEvents[action] = extras;
            defaultSlots[action] = defaults;
            slots[action] = CloneSlots(defaults);
        }

        Load();
    }

    // ========================================
    // ERİŞİM
    // ========================================
    public static InputEvent GetSlot(string action, int slot)
    {
        if (slots.TryGetValue(action, out InputEvent[] arr) && slot >= 0 && slot < SlotCount)
            return arr[slot];
        return null;
    }

    public static string GetLabelKey(string action)
    {
        foreach (ActionInfo info in Actions)
        {
            if (info.Action == action)
                return info.LabelKey;
        }
        return action;
    }

    /// <summary>Bu olayı şu an başka aksiyonlar kullanıyor mu? Kullananların listesini döner.</summary>
    public static List<string> FindConflicts(string action, InputEvent ev)
    {
        var result = new List<string>();
        if (ev == null)
            return result;

        string sig = Signature(ev);
        foreach (ActionInfo info in Actions)
        {
            if (info.Action == action || !slots.TryGetValue(info.Action, out InputEvent[] arr))
                continue;

            foreach (InputEvent e in arr)
            {
                if (e != null && Signature(e) == sig)
                {
                    result.Add(info.Action);
                    break;
                }
            }
        }
        return result;
    }

    public static bool AllowsMouse(string action)
    {
        return mouseActions.Contains(action);
    }

    public static bool SetSlot(string action, int slot, InputEvent ev)
    {
        if (ev == null || slot < 0 || slot >= SlotCount || !slots.TryGetValue(action, out InputEvent[] arr))
            return false;

        // Aynı aksiyonda aynı tuş iki slotta olmasın
        string sig = Signature(ev);
        for (int i = 0; i < SlotCount; i++)
        {
            if (i != slot && arr[i] != null && Signature(arr[i]) == sig)
                arr[i] = null;
        }

        arr[slot] = ev;
        Commit(action);
        return true;
    }

    /// <summary>
    /// Olayı slota atar ve aynı olayı diğer aksiyonlardan siler.
    /// Tamamen atamasız kalan aksiyonların listesini döner.
    /// </summary>
    public static List<string> SetSlotExclusive(string action, int slot, InputEvent ev)
    {
        var emptied = new List<string>();
        if (ev == null || !slots.ContainsKey(action))
            return emptied;

        string sig = Signature(ev);
        foreach (ActionInfo info in Actions)
        {
            if (info.Action == action || !slots.TryGetValue(info.Action, out InputEvent[] arr))
                continue;

            bool changed = false;
            for (int i = 0; i < SlotCount; i++)
            {
                if (arr[i] != null && Signature(arr[i]) == sig)
                {
                    arr[i] = null;
                    changed = true;
                }
            }

            if (!changed)
                continue;

            ApplyToInputMap(info.Action);
            if (arr[0] == null && arr[1] == null)
                emptied.Add(info.Action);
        }

        SetSlot(action, slot, ev); // kaydetme de burada yapılır
        return emptied;
    }

    /// <summary>Slotu boşaltır. Aksiyonda başka atama kalmayacaksa false döner.</summary>
    public static bool ClearSlot(string action, int slot)
    {
        if (slot < 0 || slot >= SlotCount || !slots.TryGetValue(action, out InputEvent[] arr))
            return false;

        if (arr[slot] == null)
            return true;

        bool otherExists = false;
        for (int i = 0; i < SlotCount; i++)
        {
            if (i != slot && arr[i] != null)
                otherExists = true;
        }

        if (!otherExists)
            return false;

        arr[slot] = null;
        Commit(action);
        return true;
    }

    public static void ResetAll()
    {
        foreach (ActionInfo info in Actions)
        {
            string action = info.Action;
            if (!originalEvents.TryGetValue(action, out List<InputEvent> original))
                continue;

            InputMap.ActionEraseEvents(action);
            foreach (InputEvent e in original)
                InputMap.ActionAddEvent(action, (InputEvent)e.Duplicate());

            slots[action] = CloneSlots(defaultSlots[action]);
        }

        Save();
    }

    // ========================================
    // GÖRÜNTÜ METNİ
    // ========================================
    public static string GetDisplayText(InputEvent e)
    {
        if (e == null)
            return Translate("KEYREBIND_EMPTY");

        string prefix = string.Empty;
        if (e is InputEventWithModifiers mods)
        {
            if (mods.CtrlPressed) prefix += "Ctrl+";
            if (mods.AltPressed) prefix += "Alt+";
            if (mods.ShiftPressed) prefix += "Shift+";
            if (mods.MetaPressed) prefix += "Meta+";
        }

        if (e is InputEventKey key)
        {
            Key code = GetKeyCode(key);
            // Fiziksel tuşu klavye düzenindeki harfine çevir (Q/AZERTY/TR vb. için doğru etiket)
            Key logical = DisplayServer.KeyboardGetKeycodeFromPhysical(code);
            return prefix + OS.GetKeycodeString(logical != Key.None ? logical : code);
        }

        if (e is InputEventMouseButton mouse)
        {
            switch (mouse.ButtonIndex)
            {
                case MouseButton.Left: return prefix + Translate("KEYREBIND_MOUSE_LEFT");
                case MouseButton.Right: return prefix + Translate("KEYREBIND_MOUSE_RIGHT");
                case MouseButton.Middle: return prefix + Translate("KEYREBIND_MOUSE_MIDDLE");
                default: return prefix + $"Mouse {(int)mouse.ButtonIndex}";
            }
        }

        return Translate("KEYREBIND_EMPTY");
    }

    private static string Translate(string key)
    {
        return TranslationServer.Translate(key).ToString();
    }

    // ========================================
    // İÇ YARDIMCILAR
    // ========================================
    private static Key GetKeyCode(InputEventKey k)
    {
        return k.PhysicalKeycode != Key.None ? k.PhysicalKeycode : k.Keycode;
    }

    private static string Mods(InputEventWithModifiers e)
    {
        return $"{(e.AltPressed ? 1 : 0)}{(e.CtrlPressed ? 1 : 0)}{(e.ShiftPressed ? 1 : 0)}{(e.MetaPressed ? 1 : 0)}";
    }

    private static string Signature(InputEvent e)
    {
        if (e is InputEventKey k)
            return $"key:{(long)GetKeyCode(k)}:{Mods(k)}";
        if (e is InputEventMouseButton m)
            return $"mouse:{(long)m.ButtonIndex}:{Mods(m)}";
        return string.Empty;
    }

    private static InputEvent[] CloneSlots(InputEvent[] source)
    {
        var copy = new InputEvent[SlotCount];
        for (int i = 0; i < SlotCount; i++)
            copy[i] = source[i] == null ? null : (InputEvent)source[i].Duplicate();
        return copy;
    }

    private static bool IsModified(string action)
    {
        InputEvent[] current = slots[action];
        InputEvent[] defaults = defaultSlots[action];

        for (int i = 0; i < SlotCount; i++)
        {
            string a = current[i] == null ? string.Empty : Signature(current[i]);
            string b = defaults[i] == null ? string.Empty : Signature(defaults[i]);
            if (a != b)
                return true;
        }
        return false;
    }

    private static void ApplyToInputMap(string action)
    {
        InputMap.ActionEraseEvents(action);

        foreach (InputEvent e in slots[action])
        {
            if (e != null)
                InputMap.ActionAddEvent(action, (InputEvent)e.Duplicate());
        }

        foreach (InputEvent e in extraEvents[action])
            InputMap.ActionAddEvent(action, (InputEvent)e.Duplicate());
    }

    private static void Commit(string action)
    {
        ApplyToInputMap(action);
        Save();
    }

    // ========================================
    // KAYIT / YÜKLEME
    // ========================================
    private static Godot.Collections.Dictionary Serialize(InputEvent e)
    {
        var d = new Godot.Collections.Dictionary();

        if (e is InputEventKey k)
        {
            d["t"] = "key";
            d["c"] = (long)GetKeyCode(k);
        }
        else if (e is InputEventMouseButton m)
        {
            d["t"] = "mouse";
            d["b"] = (long)m.ButtonIndex;
        }
        else
        {
            return d;
        }

        var mods = (InputEventWithModifiers)e;
        d["alt"] = mods.AltPressed;
        d["ctrl"] = mods.CtrlPressed;
        d["shift"] = mods.ShiftPressed;
        d["meta"] = mods.MetaPressed;
        return d;
    }

    private static bool ReadBool(Godot.Collections.Dictionary d, string key)
    {
        return d.ContainsKey(key) && d[key].AsBool();
    }

    private static InputEvent Deserialize(Godot.Collections.Dictionary d)
    {
        if (!d.ContainsKey("t"))
            return null;

        string type = d["t"].AsString();

        if (type == "key" && d.ContainsKey("c"))
        {
            long code = d["c"].AsInt64();
            if (code <= 0)
                return null;

            return new InputEventKey
            {
                PhysicalKeycode = (Key)code,
                AltPressed = ReadBool(d, "alt"),
                CtrlPressed = ReadBool(d, "ctrl"),
                ShiftPressed = ReadBool(d, "shift"),
                MetaPressed = ReadBool(d, "meta"),
            };
        }

        if (type == "mouse" && d.ContainsKey("b"))
        {
            long button = d["b"].AsInt64();
            if (button <= 0)
                return null;

            return new InputEventMouseButton
            {
                ButtonIndex = (MouseButton)button,
                Pressed = true,
                AltPressed = ReadBool(d, "alt"),
                CtrlPressed = ReadBool(d, "ctrl"),
                ShiftPressed = ReadBool(d, "shift"),
                MetaPressed = ReadBool(d, "meta"),
            };
        }

        return null;
    }

    private static void Save()
    {
        var bindings = new Godot.Collections.Dictionary();

        foreach (ActionInfo info in Actions)
        {
            if (!slots.ContainsKey(info.Action) || !IsModified(info.Action))
                continue;

            var arr = new Godot.Collections.Array();
            foreach (InputEvent e in slots[info.Action])
                arr.Add(e == null ? new Godot.Collections.Dictionary() : Serialize(e));

            bindings[info.Action] = arr;
        }

        var root = new Godot.Collections.Dictionary
        {
            { "version", 1 },
            { "bindings", bindings }
        };

        using FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr($"[KEYBIND] Kayıt açılamadı: {FileAccess.GetOpenError()}");
            return;
        }

        file.StoreString(Json.Stringify(root));
        GD.Print("[KEYBIND] Tuş atamaları kaydedildi.");
    }

    private static void Load()
    {
        if (!FileAccess.FileExists(SavePath))
            return;

        using FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (file == null)
            return;

        Variant parsed = Json.ParseString(file.GetAsText());
        if (parsed.VariantType != Variant.Type.Dictionary)
        {
            GD.PrintErr("[KEYBIND] keybindings.json okunamadı, varsayılanlar kullanılıyor.");
            return;
        }

        Godot.Collections.Dictionary root = parsed.AsGodotDictionary();
        if (!root.ContainsKey("bindings") || root["bindings"].VariantType != Variant.Type.Dictionary)
            return;

        Godot.Collections.Dictionary bindings = root["bindings"].AsGodotDictionary();

        foreach (ActionInfo info in Actions)
        {
            string action = info.Action;
            if (!slots.ContainsKey(action) || !bindings.ContainsKey(action))
                continue;
            if (bindings[action].VariantType != Variant.Type.Array)
                continue;

            Godot.Collections.Array arr = bindings[action].AsGodotArray();
            var loaded = new InputEvent[SlotCount];

            bool corrupt = arr.Count == 0;
            for (int i = 0; i < SlotCount && i < arr.Count; i++)
            {
                if (arr[i].VariantType != Variant.Type.Dictionary)
                {
                    corrupt = true;
                    break;
                }

                Godot.Collections.Dictionary slotData = arr[i].AsGodotDictionary();
                if (slotData.Count == 0)
                    continue; // bilinçli boş slot

                loaded[i] = Deserialize(slotData);
                if (loaded[i] == null)
                    corrupt = true;
            }

            // Bozuk kayıt varsayılanı bozmasın
            if (corrupt)
                continue;

            slots[action] = loaded;
            ApplyToInputMap(action);
        }

        GD.Print("[KEYBIND] Kayıtlı tuş atamaları yüklendi.");
    }
}
