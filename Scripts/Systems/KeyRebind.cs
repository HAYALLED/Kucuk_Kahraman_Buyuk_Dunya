using Godot;
using System.Collections.Generic;

public partial class KeyRebind : Control
{
    private const string SettingsScene = "res://Resources/Settings.tscn";

    private VBoxContainer rowsBox;
    private Label statusLabel;
    private Button resetButton;
    private TextureButton backButton;

    private readonly Dictionary<string, Button[]> slotButtons = new Dictionary<string, Button[]>();

    private string waitingAction;
    private int waitingSlot = -1;

    // Çakışma onayı bekleyen atama
    private ConfirmationDialog conflictDialog;
    private string pendingAction;
    private int pendingSlot = -1;
    private InputEvent pendingEvent;

    public override void _Ready()
    {
        KeyBindings.Initialize();

        rowsBox = GetNode<VBoxContainer>("MarginContainer/VBoxContainer/Scroll/Rows");
        statusLabel = GetNode<Label>("MarginContainer/VBoxContainer/StatusLabel");
        resetButton = GetNode<Button>("MarginContainer/VBoxContainer/ResetButton");
        backButton = GetNode<TextureButton>("BackButton");

        BuildRows();
        CreateConflictDialog();
        RefreshAll();
        SetStatus(Tr("KEYREBIND_HINT"));

        resetButton.Pressed += OnResetPressed;
        backButton.Pressed += OnBackPressed;
    }

    private void BuildRows()
    {
        foreach (KeyBindings.ActionInfo info in KeyBindings.Actions)
        {
            if (!InputMap.HasAction(info.Action))
                continue;

            string action = info.Action;

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);

            var label = new Label
            {
                Text = Tr(info.LabelKey),
                CustomMinimumSize = new Vector2(190, 40),
                VerticalAlignment = VerticalAlignment.Center
            };
            label.AddThemeFontSizeOverride("font_size", 20);
            row.AddChild(label);

            var buttons = new Button[KeyBindings.SlotCount];
            for (int i = 0; i < KeyBindings.SlotCount; i++)
            {
                int slot = i;

                var slotButton = new Button
                {
                    CustomMinimumSize = new Vector2(140, 40),
                    ClipText = true
                };
                slotButton.AddThemeFontSizeOverride("font_size", 18);
                slotButton.Pressed += () => StartListening(action, slot);
                row.AddChild(slotButton);
                buttons[slot] = slotButton;

                var clearButton = new Button
                {
                    Text = "X",
                    CustomMinimumSize = new Vector2(36, 40)
                };
                clearButton.Pressed += () => OnClearPressed(action, slot);
                row.AddChild(clearButton);
            }

            slotButtons[action] = buttons;
            rowsBox.AddChild(row);
        }
    }

    private void RefreshAll()
    {
        foreach (KeyValuePair<string, Button[]> pair in slotButtons)
        {
            for (int i = 0; i < pair.Value.Length; i++)
            {
                bool isWaiting = pair.Key == waitingAction && i == waitingSlot;
                pair.Value[i].Text = isWaiting
                    ? Tr("KEYREBIND_WAITING")
                    : KeyBindings.GetDisplayText(KeyBindings.GetSlot(pair.Key, i));
            }
        }
    }

    private void SetStatus(string text)
    {
        statusLabel.Text = text;
    }

    // ===== YAKALAMA =====
    private void StartListening(string action, int slot)
    {
        waitingAction = action;
        waitingSlot = slot;
        RefreshAll();
        SetStatus(Tr("KEYREBIND_HINT"));
    }

    private void CancelListening()
    {
        if (waitingSlot < 0)
            return;

        waitingAction = null;
        waitingSlot = -1;
        RefreshAll();
    }

    public override void _Input(InputEvent @event)
    {
        if (waitingSlot < 0)
            return;

        if (@event is InputEventKey key)
        {
            if (!key.Pressed || key.Echo)
                return;

            // Odaktaki butonun bu tuşu (Space/Enter/ok) yutmasını engelle
            GetViewport().SetInputAsHandled();

            Key code = key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;

            if (code == Key.Escape || key.Keycode == Key.Escape)
            {
                CancelListening();
                return;
            }

            if (IsModifierKey(code))
                return;

            CommitBinding(new InputEventKey { PhysicalKeycode = code });
        }
        else if (@event is InputEventMouseButton mouse && mouse.Pressed)
        {
            bool isBindable = mouse.ButtonIndex == MouseButton.Left
                || mouse.ButtonIndex == MouseButton.Right
                || mouse.ButtonIndex == MouseButton.Middle;

            if (!isBindable)
                return;

            if (KeyBindings.AllowsMouse(waitingAction))
            {
                GetViewport().SetInputAsHandled();
                CommitBinding(new InputEventMouseButton { ButtonIndex = mouse.ButtonIndex, Pressed = true });
            }
            else
            {
                // Mouse atanamayan aksiyonda başka yere tıklamak beklemeyi iptal eder
                CancelListening();
            }
        }
    }

    private static bool IsModifierKey(Key code)
    {
        return code == Key.Shift || code == Key.Ctrl || code == Key.Alt || code == Key.Meta;
    }

    private void CommitBinding(InputEvent ev)
    {
        string action = waitingAction;
        int slot = waitingSlot;

        waitingAction = null;
        waitingSlot = -1;

        List<string> conflicts = KeyBindings.FindConflicts(action, ev);
        if (conflicts.Count > 0)
        {
            pendingAction = action;
            pendingSlot = slot;
            pendingEvent = ev;
            RefreshAll(); // slot eski tuşunu göstermeye devam eder

            conflictDialog.DialogText = string.Format(
                Tr("KEYREBIND_CONFLICT_TEXT"),
                KeyBindings.GetDisplayText(ev),
                JoinActionNames(conflicts));
            conflictDialog.PopupCentered(new Vector2I(560, 190));
            return;
        }

        KeyBindings.SetSlot(action, slot, ev);
        RefreshAll();
        SetStatus(Tr("KEYREBIND_SAVED"));
    }

    // ===== ÇAKIŞMA DİYALOĞU =====
    private void CreateConflictDialog()
    {
        conflictDialog = new ConfirmationDialog
        {
            Title = Tr("KEYREBIND_CONFLICT_TITLE"),
            OkButtonText = Tr("KEYREBIND_CONFLICT_BOTH"),
            CancelButtonText = Tr("KEYREBIND_CONFLICT_CANCEL"),
            Exclusive = true
        };
        conflictDialog.AddButton(Tr("KEYREBIND_CONFLICT_ONLY_HERE"), false, "only_here");

        conflictDialog.Confirmed += OnConflictBoth;
        conflictDialog.Canceled += OnConflictCancel;
        conflictDialog.CustomAction += OnConflictCustomAction;

        AddChild(conflictDialog);
    }

    private string JoinActionNames(List<string> actions)
    {
        var names = new List<string>();
        foreach (string a in actions)
            names.Add(Tr(KeyBindings.GetLabelKey(a)));
        return string.Join(", ", names);
    }

    private void OnConflictBoth()
    {
        ApplyPending(false);
    }

    private void OnConflictCustomAction(StringName action)
    {
        conflictDialog.Hide();
        if (action.ToString() == "only_here")
            ApplyPending(true);
    }

    private void OnConflictCancel()
    {
        ClearPending();
        RefreshAll();
        SetStatus(Tr("KEYREBIND_CONFLICT_CANCELLED"));
    }

    private void ApplyPending(bool exclusive)
    {
        if (pendingEvent == null)
            return;

        List<string> emptied = null;
        if (exclusive)
            emptied = KeyBindings.SetSlotExclusive(pendingAction, pendingSlot, pendingEvent);
        else
            KeyBindings.SetSlot(pendingAction, pendingSlot, pendingEvent);

        ClearPending();
        RefreshAll();

        if (emptied != null && emptied.Count > 0)
            SetStatus(string.Format(Tr("KEYREBIND_UNASSIGNED_FORMAT"), JoinActionNames(emptied)));
        else
            SetStatus(Tr("KEYREBIND_SAVED"));
    }

    private void ClearPending()
    {
        pendingAction = null;
        pendingSlot = -1;
        pendingEvent = null;
    }

    // ===== BUTONLAR =====
    private void OnClearPressed(string action, int slot)
    {
        CancelListening();

        if (!KeyBindings.ClearSlot(action, slot))
        {
            SetStatus(Tr("KEYREBIND_NEED_ONE"));
            return;
        }

        RefreshAll();
        SetStatus(Tr("KEYREBIND_SAVED"));
    }

    private void OnResetPressed()
    {
        waitingAction = null;
        waitingSlot = -1;

        KeyBindings.ResetAll();
        RefreshAll();
        SetStatus(Tr("KEYREBIND_RESET_DONE"));
    }

    private void OnBackPressed()
    {
        waitingAction = null;
        waitingSlot = -1;
        GetTree().ChangeSceneToFile(SettingsScene);
    }
}
