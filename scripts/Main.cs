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

        //Start the in-game console. Using MIT licensed LimboConsole for the implementation with most extra features disabled/stripped out.
        //Doesnt get a Global reference because no one should need it.
        //Check out the Console class to see how commands are managed and added.
        //Uses a special dev key (`) to toggle, bypassing the input system (see _Input() below)
        Logging.Log($"Starting in-game console...", "Main");
        Console console = new Console();
        console.Name = "InGameConsole";
        AddChild(console);

        //Now we can show the loading screen and do all the heavier stuff
        Global.ui.StartLoadingScreen();
        Global.ui.SetLoadingScreenDescription("Compiling Shaders...");

        //Boot up the InputMapManager, register a reference with Global, and tell it to load our input settings from disk, or make a new file if there isnt one.
        //This is basically a "in-code" version of the Godot input mapper, which lets us do dynamic keybinding and saving remapped keys.
        Logging.Log($"Starting InputMapManager Handler and loading input map file...", "Main");
        Global.InputMap = new();
        Global.InputMap.InitInputMap();

        //TODO: Add additonal start up items here.
        await DoSomeLongShit();

        //Core game state manager - handles world state, network sync, etc.
        Global.gameState = new GameState();
        Global.gameState.Name = "GameState";
        AddChild(Global.gameState);

        Global.ui.StopLoadingScreen();

        //Create the Lobby system, register a reference to it with Global, and "host" a new lobby right away.
        //Hosting a lobby is what allows us to be joinable in Steam, adding the "join to" button for Friends, and adding the "invite to play" button on friends for us.
        //We do this last so that no one tries to join us until after core systems are ready.
        Logging.Log($"Starting Lobby system and auto-hosting lobby to make us joinable thru steam", "Main", true,true);
        Global.Lobby = new();
        Global.Lobby.HostNewLobby();

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

    //Runs once per frame
    public override void _Process(double delta)
    {
        SteamAPI.RunCallbacks();
        Global.network.PerFrame(delta);

    }

    //_PhysicsProcess gets called 60 times a second (by default). We are using it to establish a tickrate that is disconnected from the framerate.
    //We can adjust the tick rate by changing the engine's physics rate.
    public override void _PhysicsProcess(double delta)
    {
        Global.network.Tick(delta);

        if (Global.bConsoleOpen)
        {
            ConsolePicker();
        }

    }

    private void ConsolePicker()
    {
        if (Global.bConsoleOpen && Global.gameState.gameStarted && Input.IsActionJustPressed("FIRE"))
        {
            var mpos = GetViewport().GetMousePosition();
            GOBasePlayerCharacter pc = Global.gameState.GetLocalPlayerCharacter();
            if (pc != null)
            {
                var from = pc.GetCamera().ProjectRayOrigin(mpos);
                var to = from + pc.GetCamera().ProjectRayNormal(mpos) * 1000;
                var space = Global.gameState.GetWorld3D().DirectSpaceState;
                var query = PhysicsRayQueryParameters3D.Create(from, to);
                query.CollideWithAreas = true;
                query.CollideWithBodies = true;
                query.Exclude = [pc.GetRid()];
                var result = space.IntersectRay(query);
                if (result.TryGetValue("collider", out Variant col))
                {
                    Node collision = (Node)col;
                    if (collision is IGameObject go)
                    {
                        Logging.Log($"You just clicked on a gameobject with type {go.type}, id {go.id}. ID saved to clipboard!", "Picker");
                        DisplayServer.ClipboardSet(go.id.ToString());
                        Global.gameState.SetDebugTarget(go);
                    }
                    else
                    {
                        Logging.Log($"You just clicked on non game object {collision} (Name={collision.Name}  at pos {result["position"]} (ID: {result["collider_id"]})", "Picker");
                    }
                }
                else
                {
                    Logging.Log($"No pick ray collision detected. From {from}, to {to}", "Picker");
                }

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
            Global.Lobby.LeaveLobby();
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
