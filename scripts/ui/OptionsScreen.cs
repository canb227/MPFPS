using Godot;
using System;
using System.Diagnostics;
using System.Linq;

public partial class OptionsScreen : Control
{

    private LineEdit WindowWidth;
    private LineEdit WindowHeight;
    private CheckBox Fullscreen;

    private LineEdit MouseSensX;
    private LineEdit MouseSensY;
    private PlayerConfig conf;

    private LineEdit FOV;

    private GridContainer keymapGrid;
    private bool waitingForInput;
    private ActionFlags waitingForInputAction;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        conf = Global.Config.loadedPlayerConfig;
        GetNode<Button>("BUTTON_cancel").Pressed += OnCancelPressed;
        GetNode<Button>("BUTTON_apply").Pressed += OnApplyPressed;

        WindowWidth = GetNode<LineEdit>("WindowWidthEdit");
        WindowWidth.Text = conf.window_width.ToString();

        WindowHeight = GetNode<LineEdit>("WindowHeightEdit");
        WindowHeight.Text = conf.window_height.ToString();

        Fullscreen = GetNode<CheckBox>("FullscreenCheck");
        Fullscreen.ButtonPressed = conf.fullscreen;

        MouseSensX = GetNode<LineEdit>("MouseSensXEdit");
        MouseSensX.Text = conf.mouseSensX.ToString();

        MouseSensY = GetNode<LineEdit>("MouseSensYEdit");
        MouseSensY.Text = conf.mouseSensY.ToString();

        FOV = GetNode<LineEdit>("FOVEdit");
        FOV.Text = conf.fov.ToString();

        keymapGrid = GetNode<GridContainer>("keymap/keymapGrid");
        foreach(ActionFlags action in Enum.GetValues(typeof(ActionFlags)))
        {
            if (action == ActionFlags.None)
            {
                continue;
            }
            Label actionLabel = new Label();
            actionLabel.Text = action.ToString();
            keymapGrid.AddChild(actionLabel);
            
            Button button = new Button();
            button.Name = action.ToString();
            var key = InputMapManager.loadedPlayerInputMap.KeyboardKeyCodeToActionMap.FirstOrDefault(x => x.Value == action).Key;

            button.Text = key.ToString();

            button.Pressed += () => OnKeyMapButtonPressed(action);

            keymapGrid.AddChild(button);
        }

    }

    void OnKeyMapButtonPressed(ActionFlags action)
    {
        Logging.Log($"pressed {action} remap key", "Remapper");
        GetNode<Control>("bg2").Show();
        GetNode<Control>("bg2").GetNode<Label>("lbl").Text = $"Press Key for `{action.ToString()}`";
        waitingForInput = true;
        waitingForInputAction = action;
    }

    private void OnApplyPressed()
    {
        if (Fullscreen.ButtonPressed)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        }
        else
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            conf.window_width = int.Parse(WindowWidth.Text);
            conf.window_height = int.Parse(WindowHeight.Text);
            GetWindow().Size = new Vector2I(conf.window_width, conf.window_height);
        }
        Logging.Log($"SETTING FOV TO: {int.Parse(FOV.Text)}", "FOV");
        conf.fov = int.Parse(FOV.Text);
        conf.mouseSensX = int.Parse(MouseSensX.Text);
        conf.mouseSensY = int.Parse(MouseSensY.Text);
        Global.Config.loadedPlayerConfig = conf;
        Global.Config.SavePlayerConfig();
        Global.ui.SwitchFullScreenUI("UI_MainMenu");

    }

    private void OnCancelPressed()
    {
        //InputMapManager.LoadPlayerInputMap();
        Global.ui.SwitchFullScreenUI("UI_MainMenu");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}

    public override void _Input(InputEvent @event)
    {
        if (!waitingForInput) { return; }
        if (@event is InputEventKey k && k.Pressed && k.Keycode!=Key.Quoteleft)
        {
            if (InputMapManager.loadedPlayerInputMap.KeyboardKeyCodeToActionMap.TryGetValue(k.Keycode,out var val) && val == waitingForInputAction)
            {
                waitingForInputAction = ActionFlags.None;
                waitingForInput = false;
                GetNode<Control>("bg2").Hide();
                return;
            }
            if (InputMapManager.loadedPlayerInputMap.KeyboardKeyCodeToActionMap.TryGetValue(k.Keycode, out var val2) && val2 != ActionFlags.None)
            {
                Logging.Log($"That key ({k.Keycode}) is already bound to {InputMapManager.loadedPlayerInputMap.KeyboardKeyCodeToActionMap[k.Keycode]},unbinding that key!", "Keymapper");
                keymapGrid.GetNode<Button>(Enum.GetName(InputMapManager.loadedPlayerInputMap.KeyboardKeyCodeToActionMap[k.Keycode])).Text = Key.None.ToString();
                InputMapManager.UnbindKeyboardKey(k.Keycode);

            }

            InputMapManager.BindKeyboardKey(k.Keycode, waitingForInputAction, false);
            waitingForInput = false;
            GetNode<Control>("bg2").Hide();
            keymapGrid.GetNode<Button>(Enum.GetName(waitingForInputAction)).Text = k.Keycode.ToString();
            waitingForInputAction = ActionFlags.None;


        }
        else if (@event is InputEventMouseButton m && m.Pressed)
        {
            if (InputMapManager.loadedPlayerInputMap.MouseButtonToActionMap.TryGetValue(m.ButtonIndex, out var val2) && val2 != ActionFlags.None)
            {
                Logging.Log($"That mousebutton ({m.ButtonIndex}) is already bound to {InputMapManager.loadedPlayerInputMap.MouseButtonToActionMap[m.ButtonIndex]},unbinding that key!", "Keymapper");
                keymapGrid.GetNode<Button>(Enum.GetName(InputMapManager.loadedPlayerInputMap.MouseButtonToActionMap[m.ButtonIndex])).Text = Key.None.ToString();
                InputMapManager.UnbindMouseButton(m.ButtonIndex);

            }
            InputMapManager.BindMouseButton(m.ButtonIndex, waitingForInputAction, false);
            waitingForInput = false;
            GetNode<Control>("bg2").Hide();
            keymapGrid.GetNode<Button>(Enum.GetName(waitingForInputAction)).Text = m.ButtonIndex.ToString();
            waitingForInputAction = ActionFlags.None;
        }


    }
}
