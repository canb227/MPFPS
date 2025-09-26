using Godot;
using Steamworks;
using System;



/// <summary>
/// Singleton that autoloads right after Godot engine init. Can be statically referenced using "Global." anywhere.
/// This class is for storing universally useful references to objects.
/// </summary>
public partial class Global : Node
{
    /// <summary>
    /// semver (https://semver.org/)
    /// </summary>
    public const string VERSION = "0.0.1";

    /// <summary>
    /// Steam app ID. Set to 480 (SpaceWar) for development.
    /// Unused while app_id.txt is present in root folder. Remove that file when deploying to Steam.
    /// </summary>
    public const int APP_ID = 480;

    /// <summary>
    /// If true, fake steam and network connections. Allows for local testing, not for production.
    /// </summary>
    public const bool OFFLINE_MODE = false;

    /// <summary>
    /// The SteamID of the user that launched the game. Is set to 0 if Steam Init fails.
    /// </summary>
	public static ulong steamid = 0;

    /// <summary>
    /// true if the game has succesfully connected to Steam's API - don't call Steamworks functions until after this is true
    /// </summary>
    public static bool bIsSteamConnected = false;

    /// <summary>
    /// If true, draw ImGUI debug screens (if any are active)
    /// </summary>
    public static bool DrawDebugScreens = false;

    public static bool bConsoleOpen = false;

    public static Node instance;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Node Derived Singletons (On scene tree, under main)

    public static GameState gameState;

    /// <summary>
    /// Holds a reference to the currently active UI system
    /// </summary>
    public static UI ui;


    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Non-Node Singletons

    /// <summary>
    /// Holds a reference to the currently active networking system
    /// </summary>
    public static SteamNetwork network = new();

    /// <summary>
    /// holds a reference to the lobby system that is always running in the background
    /// </summary>
    public static Lobby Lobby;

    /// <summary>
    /// Holds a reference to the InputMapManager, useful for rebinding keys
    /// </summary>
    public static InputMapManager InputMap;

    /// <summary>
    /// Handles saving and loading to/from disk player settings and meta-progression
    /// </summary>
    public static Config Config;



    //This is the first of our code that runs when starting the game, right after plugins load but before any of our nodes.
    public override void _Ready()
    {
        instance = this;



        SteamInit(); //We have to do Steam here in the Global autoload, doing it in a normal scene is too late for the SteamAPI hooks to work.

        Logging.Start(); //Also start logging here instead of Main so its ready super early to log stuff

        //This makes sure the file structure in the godot user data folder is setup correctly
        Logging.Log($" mkdir user://saves                    | {DirAccess.MakeDirAbsolute("user://saves").ToString()}", "FirstTimeSetup");
        Logging.Log($" mkdir user://config                  | {DirAccess.MakeDirAbsolute("user://config").ToString()}", "FirstTimeSetup");
        Logging.Log($" mkdir user://logs                      | {DirAccess.MakeDirAbsolute("user://logs").ToString()}", "FirstTimeSetup");
        Logging.Log($" mkdir user://saves/{Global.steamid}   | {DirAccess.MakeDirAbsolute("user://saves/" + Global.steamid).ToString()}", "FirstTimeSetup");
        Logging.Log($" mkdir user://config/{Global.steamid} | {DirAccess.MakeDirAbsolute("user://config/" + Global.steamid).ToString()}", "FirstTimeSetup");
        Logging.Log($" mkdir user://logs/{Global.steamid}     | {DirAccess.MakeDirAbsolute("user://logs/" + Global.steamid).ToString()}", "FirstTimeSetup");

        if (Logging.bSaveLogsToFile)
        {
            Logging.StartLoggingToFile();
        }

        Logging.Log("Connection to Steam successful.", "SteamAPI");
        Logging.Log($"Steam ID: {steamid}", "SteamAPI");


        //OverrideGodotMultiplayerInterface();
        //From here next code ran is in Main.cs's _Ready()
    }

    private void OverrideGodotMultiplayerInterface()
    {
        MPFPSMultiplayerAPI mpapi = new();
        mpapi.MultiplayerPeer = null;
        GetTree().SetMultiplayer(mpapi);
    }

    /// <summary>
    /// Starts up the SteamAPI using SteamWorks.Net. After this the user will show as playing the game and Steamworks functions will be functional.
    /// </summary>
    public void SteamInit()
    {
        Logging.Log("Initializing Steam API...", "SteamAPI");

        try
        {
            //SteamAPI call that checks if steam is running in the background. If it is not, it starts steam then starts the game once steam is started.
            //We close the game so we don't end up with two instances open.

            if (SteamAPI.RestartAppIfNecessary((AppId_t)APP_ID)) //ALWAYS RETURNS FALSE IF app_id.txt IS PRESENT IN ROOT FOLDER
            {
                GD.PushError("Steam is not running. Starting Steam then relaunching game", "SteamAPI");
                GetTree().Quit();
            }
        }
        catch (System.DllNotFoundException)
        {
            //nothing works if the steam dll for our OS isn't present. We only support Windows at the moment.
            GD.PushError("steam_api64.dll not found. steam_api64.dll is expected in the game root folder.", "SteamAPI");
            throw;
        }

        if (SteamAPI.Init())
        {
            //Not technically needed, but gets our steam relay connection started up as soon as possible - there can be up to a three second delay from first networking API call
            SteamNetworkingUtils.InitRelayNetworkAccess(); 

            steamid = SteamUser.GetSteamID().m_SteamID;
            bIsSteamConnected = true;
        }
        else
        {
            GD.PushError("Steam not initialized", "SteamAPI");
        }
    }
}
