using Godot;
using ImGuiNET;
using Limbo.Console.Sharp;
using Steamworks;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Our entry point after our autoloads (Global) finish loading
/// Inits and drives everything else, including determining what gets to run per frame/perTick
/// </summary>
public partial class Main : Node
{


    public Input.MouseModeEnum stashedMouseMouseMode = Input.MouseModeEnum.Max;


    //This runs after Global._Ready(), and handles most of our startup. (Logging and Steam are handled earlier in Global)
    public async override void _Ready()
    {
        
        //First up, start up the config system and register a static reference to it in Global. Despite its name, this system handles both user configuration settings and user savefiles/meta-progression tracking
        //It'll automatically load up the Config file and the Progression files from disk for the logged in user from the user:// Godot file location.
        //If they don't exist it makes them.
        Logging.Log($"Starting Config system and Loading player config and save files...", "Main");
        Global.Config = new();
        Global.Config.InitConfig();

        //TODO: Load saved graphics settings (resolution, maybe UI scaling?) and apply them before we load the UI

        //Next fire up the UI system and register a static reference to it in Global. It doesn't do anything automatically yet but should probably start showing the relevant splash screens right away.
        //The UI system inherits the Control node,and gets added to the Godot scenetree as a child of main so it can render UI correctly.
        Logging.Log("Starting UI...", "Main");
        Global.ui = new();
        Global.ui.Name = "UIManager";
        AddChild(Global.ui);

        //TODO: We can show some splash screens and random bullshit while the below runs

        //Boot up the InputMapManager, register a reference with Global, and tell it to load our input settings from disk, or make a new file if there isnt one.
        //This is basically a "in-code" version of the Godot input mapper, which lets us do dynamic keybinding and saving remapped keys.
        Logging.Log($"Starting InputMapManager Handler and loading input map file...", "Main");
        Global.InputMap = new();
        Global.InputMap.InitInputMap();

        //Start the in-game console. Using MIT licensed LimboConsole for the implementation with most extra features disabled/stripped out.
        //Doesnt get a Global reference because no one should need it.
        //Check out the Console class to see how commands are managed and added.
        //Uses a special dev key (`) to toggle, bypassing the input system (see _Input() below)
        Logging.Log($"Starting in-game console...", "Main");
        Console console = new Console();
        console.Name = "InGameConsole";
        AddChild(console);


        if(!Global.OFFLINE_MODE)
        {
            //Fires up the core networking component and registers a global reference to it. Doesn't trigger any behaviour right away, but once this is started we are able to receive packets over the Steam Relay Network.
            Logging.Log($"Starting core Networking manager...", "Main");
            Global.network = new SteamNetworkManager();
        }
        else
        {
            Logging.Log($"Starting faked debug network for local testing!", "Main");
            Logging.Warn("WARNING! OFFLINE MODE IS ON! STEAM NETWORKING FUNCTIONS WILL NOT WORK!","Main");
            Global.network = new OfflineNetworkManager();
        }
        Global.network.Name = "network";
        AddChild(Global.network);

        Global.GameState = new();
        Global.GameState.Name = "GameState";
        AddChild(Global.GameState);
    
        //TODO: Trigger preloading and shader stuff here if needed

        Global.ui.StartLoadingScreen();
        Global.ui.SetLoadingScreenDescription("Compiling Shaders...");
        
        await DoSomeLongShit();

        Global.ui.StopLoadingScreen();

        //TODO: Wait for splash screens to be done, wait for preloading and shaders and shit to be done.

        Logging.Log("Startup complete!", "Main");
        //At this point we're setup and ready to go. First, let's check and see if Steam auto-launched the game because we accepted an invite while the game was closed.
        SteamApps.GetLaunchCommandLine(out string commandLine, 1024);
        Logging.Log("Checking for Launch Arguments...", "Main");
        if (!string.IsNullOrEmpty(commandLine))
        {
            Logging.Log($"Launch Argument detected: {commandLine}", "Main");
            Logging.Log($"Interperting arg as SteamID to immediately join...", "Main");
            ulong cmdLineSteamID = 0;

            //TODO: Parse launch arg string, its in some weird format like '+connect steamid' or some shit I need to test it
            throw new NotImplementedException("Joining to another player while the game is not open is not implmented yet.");
            return;
        }
        else
        {
            Logging.Log("No Launch Command Line found - continuing normally.", "Main");
        }

        //TODO: Show main menu/intro screen/whatever
        Global.ui.SwitchFullScreenUI("UI_MainMenu");

        //If we launched the game normally, startup is now fully complete
    }

    private async Task DoSomeLongShit()
    {
        await ToSignal(GetTree().CreateTimer(.1f), SceneTreeTimer.SignalName.Timeout);
        Global.ui.UpdateLoadingScreenProgressBar(20);
        await ToSignal(GetTree().CreateTimer(.1f), SceneTreeTimer.SignalName.Timeout);
        Global.ui.UpdateLoadingScreenProgressBar(40);
        await ToSignal(GetTree().CreateTimer(.1f), SceneTreeTimer.SignalName.Timeout);
        Global.ui.UpdateLoadingScreenProgressBar(60);
        await ToSignal(GetTree().CreateTimer(.1f), SceneTreeTimer.SignalName.Timeout);
        Global.ui.UpdateLoadingScreenProgressBar(80);
        await ToSignal(GetTree().CreateTimer(.1f), SceneTreeTimer.SignalName.Timeout);
        Global.ui.UpdateLoadingScreenProgressBar(100);
    }

    //To help maintain an understanding of exactly what runs every frame, and in what order, main is the only thing that has a _Process()
    //Here we call out to everything else that needs to run once per frame.
    //Experimental approach, we'll see how it goes.
    //NOTE: Largely abandoning the above due to it doesnt help and it makes physics management in Godot a huge pain in the ass.
    public override void _Process(double delta)
    {
        SteamAPI.RunCallbacks();


    }


    public override void _PhysicsProcess(double delta)
    {


        if (Global.bConsoleOpen)
        {
            ConsolePicker();
        }

    }

    private void ConsolePicker()
    {
        if (Input.IsActionJustPressed("FIRE"))
        {
            var mpos = GetViewport().GetMousePosition();
            var from = GetViewport().GetCamera3D().ProjectRayOrigin(mpos);
            var to = from + GetViewport().GetCamera3D().ProjectRayNormal(mpos) * 1000;
            var space = Global.GameState.world.GetWorld3D().DirectSpaceState;
            var query = PhysicsRayQueryParameters3D.Create(from, to);
            query.CollideWithAreas = true;
            query.CollideWithBodies = true;
            var result = space.IntersectRay(query);
            if (result.TryGetValue("collision", out Variant col))
            {
                Node collision = (Node)col;
                if (collision.GetParent() is Node3D node)
                {
                    Logging.Log($"You just clicked on a node that has a collider child!", "Picker");
                    //DisplayServer.ClipboardSet(go.GetInstanceID().ToString());
                    //TextEdit cmdBar = GetTree().Root.GetNode<TextEdit>("LimboConsole/@PanelContainer@3/@VBoxContainer@4/@TextEdit@12");
                    //cmdBar.Text += go.GetUID();
                    //cmdBar.SetCaretColumn(cmdBar.Text.Length);
                }
                else
                {
                    Logging.Log($"You just clicked on node without a collider. howd you do that.", "Picker");
                }
            }
            else
            {
                Logging.Log($"No Pick collision detected. From {from}, to {to}", "Picker");

            }
        }
    }

    /// <summary>
    /// This bypasses any input systems we wrote and just directly looks to see if a specific keycode is pressed.
    /// Used to handle important dev inputs like the console and debug toggles
    /// </summary>
    /// <param name="event"></param>
    public override void _Input(InputEvent @event)
    {
        //right bracket and tilde are special dev keys that bypass the input handling system
        if (@event is InputEventKey k && k.Pressed)
        {
            //right bracket toggles ImGUI debug overlays
            if (k.Keycode == Key.Bracketright)
            {
                Global.DrawDebugScreens = !Global.DrawDebugScreens;
                Logging.Log($"DebugScreens Active: {Global.DrawDebugScreens}", "DebugScreens");
                GetViewport().SetInputAsHandled();
            }
            //backtick/tilde toggles the console
            else if (k.Keycode == Key.Quoteleft)
            {
                if (Global.bConsoleOpen)
                {
                    Global.bConsoleOpen = false;
                    LimboConsole.CloseConsole();
                    if (Input.MouseMode==Input.MouseModeEnum.Visible)
                    {
                        Input.MouseMode = stashedMouseMouseMode;
                    }

                }
                else
                {
                    Global.bConsoleOpen = true;
                    stashedMouseMouseMode = Input.MouseMode;
                    LimboConsole.OpenConsole();
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                }
                GetViewport().SetInputAsHandled();
            }
        }

    }

    /// <summary>
    /// Notifications are usually system interrupts of various kinds. We handle them here in main to stay organized.
    /// </summary>
    /// <param name="what"></param>
    public override void _Notification(int what)
    {
        //this notification shows up if the app gets a close request (like hitting the (X) button in windows)
        //Does not fire if process gets killed - you cant really do anything about that
        if (what == NotificationWMCloseRequest)
        {
            QuitGame();
        }
    }

    public static void QuitGame()
    {
        Global.Config.SavePlayerProgression();
        Global.Config.SavePlayerConfig();
        Global.InputMap.SavePlayerInputMap();
        Global.instance.GetTree().Quit();
    }
}
