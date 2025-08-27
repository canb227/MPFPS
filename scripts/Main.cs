using Godot;
using Limbo.Console.Sharp;
using Steamworks;
using System;

/// <summary>
/// Our entry point after our autoloads (Global) finish loading
/// Inits and drives everything else, including determining what gets to run per frame/perTick
/// </summary>
public partial class Main : Node
{

    public override void _Ready()
    {

        Logging.Log($"PreGameLoading player config and save files...","Main");
        Global.Config = new();
        Global.Config.InitConfig();
        //TODO: Load graphics settings

        Logging.Log("Starting UI...", "Main");
        Global.ui = new();
        Global.ui.Name = "UIManager";
        AddChild(Global.ui);
        //TODO: We can show some splash screens and random bullshit while the below runs

        Logging.Log($"Starting InputMapManager Handler and loading input map file...", "Main");
        Global.InputMap = new();
        Global.InputMap.InitInputMap();

        Logging.Log($"Starting in-game console...", "Main");
        Console console = new Console();
        console.Name = "InGameConsole";
        AddChild(console);//Console gets to be on the scene tree for "reasons" (expression evaluation (not using this feature atm but :shrug:))

        Logging.Log($"Starting core Networking manager...", "Main");
        Global.network = new SteamNetwork();

        Logging.Log($"Starting world/level/3DNode manager...", "Main");
        Global.world = new World();
        Global.world.Name = "World";
        AddChild(Global.world);

        //TODO:any other startup stuff goes here - maybe preloading or smth idk

        //Do the Lobby stuff last so no one tries to join us before we're ready
        Logging.Log($"Starting Lobby system and auto-hosting lobby to make us joinable thru steam", "Main");
        Global.Lobby = new();
        Global.Lobby.HostNewLobby();


        //TODO: make sure the splash screens and random bullshit are done before continuing

        //At this point we're setup and ready to go. First, let's check and see if Steam auto-launched the game because we accepted an invite while the game was closed.
        Logging.Log("Startup complete!", "Main");
        SteamApps.GetLaunchCommandLine(out string commandLine, 1024);
        Logging.Log("Checking for Launch Arguments...","Main");
        if (!string.IsNullOrEmpty(commandLine))
        {
            Logging.Log($"Launch Argument detected: {commandLine}","Main");
            Logging.Log($"Interperting arg as SteamID to immediately join...","Main");
            ulong cmdLineSteamID = 0;

            //TODO: Parse launch arg string, its in some weird format like '+connect steamid' or some shit I need to test it
            throw new NotImplementedException("Joining to another player while the game is not open is not implmented yet.");

            Global.Lobby.AttemptJoinToLobby(cmdLineSteamID);
        }
        else
        {
            Logging.Log("No Launch Command Line found - continuing normally.","Main");
        }

        //TODO: start main menu UI here

    }

    //Run all the stuff below once per frame.
    public override void _Process(double delta)
    {
        SteamAPI.RunCallbacks();
        Global.network.PerFrame(delta);
        Global.world.PerFrame(delta);
        if (Global.GameSession != null && Global.GameSession.playerData!=null)
        {
            foreach (var player in Global.GameSession.playerData)
            {
                player.Value.playerController.PerFrame(delta);
            }
        }

    }

    //Run all the stuff below once per tick (60ticks/second)
    public override void _PhysicsProcess(double delta)
    {
        Global.network.Tick(delta);
        Global.world.Tick(delta);
    }

    public override void _Input(InputEvent @event)
    {
        //right bracket and tilde are special dev keys that bypass the input handling system
        if (@event is InputEventKey k && k.Pressed)
        {
            //right bracket toggles ImGUI debug overlays
            if (k.Keycode==Key.Bracketright)
            {
                Global.DrawDebugScreens = !Global.DrawDebugScreens;
                GetViewport().SetInputAsHandled();
            }
            //backtick/tilde toggles the console
            else if (k.Keycode==Key.Quoteleft)
            {
                LimboConsole.ToggleConsole();
                GetViewport().SetInputAsHandled();
            }
        }    
    }
    public override void _Notification(int what)
    {
        //app gets a close request (like hitting the (X) button in windows)
        //Does not fire if process gets killed - you cant really do anything about that
        if (what == NotificationWMCloseRequest)
        {
            //lobal.network.NetworkCleanup();
            GetTree().Quit(); // default behavior
        }
    }
}
