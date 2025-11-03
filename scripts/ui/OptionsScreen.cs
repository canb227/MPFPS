using Godot;
using System;

public partial class OptionsScreen : Control
{

    private TextEdit WindowWidth;
    private TextEdit WindowHeight;
    private CheckBox Fullscreen;

    private TextEdit MouseSensX;
    private TextEdit MouseSensY;
    private PlayerConfig conf;

    private GridContainer keymapGrid;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        conf = Global.Config.loadedPlayerConfig;
        GetNode<Button>("BUTTON_cancel").Pressed += OnCancelPressed;
        GetNode<Button>("BUTTON_apply").Pressed += OnApplyPressed;

        WindowWidth = GetNode<TextEdit>("WindowWidthEdit");
        WindowWidth.Text = conf.window_width.ToString();

        WindowHeight = GetNode<TextEdit>("WindowHeightEdit");
        WindowHeight.Text = conf.window_height.ToString();

        Fullscreen = GetNode<CheckBox>("FullscreenCheck");
        Fullscreen.ButtonPressed = conf.fullscreen;

        MouseSensX = GetNode<TextEdit>("MouseSensXEdit");
        MouseSensX.Text = conf.mouseSensX.ToString();

        MouseSensY = GetNode<TextEdit>("MouseSensYEdit");
        MouseSensY.Text = conf.mouseSensY.ToString();

        keymapGrid = GetNode<GridContainer>("keymap/keymapGrid");
        foreach(var action in Enum.GetValues(typeof(ActionFlags)))
        {
            Label actionLabel = new Label();
            actionLabel.Text = action.ToString();
            keymapGrid.AddChild(actionLabel);
            
            Button button = new Button();
            button.Text = "KEYNAME";
            keymapGrid.AddChild(button);
        }

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

        conf.mouseSensX = 5;
        conf.mouseSensY = 5;
        Global.Config.SavePlayerConfig();
        Global.ui.SwitchFullScreenUI("UI_MainMenu");
    }

    private void OnCancelPressed()
    {
        Global.ui.SwitchFullScreenUI("UI_MainMenu");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}
}
