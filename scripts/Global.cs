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

    public static GameState GameState;

    /// <summary>
    /// Holds a reference to the currently active UI system
    /// </summary>
    public static UI ui;


    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Non-Node Singletons

    /// <summary>
    /// Holds a reference to the currently active networking system
    /// </summary>
    public static NetworkManager network;



    /// <summary>
    /// Holds a reference to the InputMapManager, useful for rebinding keys
    /// </summary>
    public static InputMapManager InputMap;

    /// <summary>
    /// Handles saving and loading to/from disk player settings and meta-progression
    /// </summary>
    public static Config Config;




    //This is the first of our code that runs when starting the game, right after engine init but before any other nodes (before main)
    public override void _Ready()
    {
        instance = this;



        SteamInit(); //We have to do Steam here in the Global autoload, doing it in a normal scene is too late for the SteamAPI hooks to work.

        Logging.Start(); //Also start logging here instead of Main so its ready super early to log stuff

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
            if (SteamAPI.RestartAppIfNecessary((AppId_t)APP_ID)) //ALWAYS RETURNS FALSE IF app_id.txt IS PRESENT IN ROOT FOLDER
            {
                GD.PushError("Steam is not running. Starting Steam then relaunching game", "SteamAPI");
                GetTree().Quit();
            }
        }
        catch (System.DllNotFoundException)
        {
            GD.PushError("steam_api64.dll not found. steam_api64.dll is expected in the game root folder.", "SteamAPI");
            throw;
        }

        if (SteamAPI.Init())
        {
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
