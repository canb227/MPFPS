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
    /// Semantic version compliant (https://semver.org/) string
    /// </summary>
    public const string VERSION = "0.0.1";

    /// <summary>
    /// Steam app ID. Set to 480 (SpaceWar) for development.
    /// Unused while app_id.txt is present in root folder. Remove that file when deploying to Steam.
    /// </summary>
    public const int APP_ID = 480;

    /// <summary>
    /// The SteamID of the user that launched the game. Is set to 0 if Steam Init fails.
    /// </summary>
	public static ulong steamid = 0;

    public static bool bIsSteamConnected = false;

    public static NetworkManager network;
    public static World world;
    public static EntityLoader EntityLoader = new();
    public static SceneLoader SceneLoader = new();
    public static SimpleNetworking snetwork;

    //This is the first of our code that runs when starting the game, right after engine init but before any other nodes (before main)
    public override void _Ready()
    { 

        SteamInit(); //We have to do Steam here in the Global autoload, doing it in a normal scene is too late for the SteamAPI hooks to work.

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
                Logging.Error("Steam is not running. Starting Steam then relaunching game", "SteamAPI");
                GetTree().Quit();
            }
        }
        catch (System.DllNotFoundException e)
        {
            Logging.Error("Steam DLLs not found. steam_api.dll, steam_api.lib, steam_api64.dll, steam_api64.lib are all expected in the game root folder.", "SteamAPI");
            GetTree().Quit();
        }

        if (SteamAPI.Init())
        {
            SteamNetworkingUtils.InitRelayNetworkAccess();
            Logging.Log("Connection to Steam successful.", "SteamAPI");
            steamid = SteamUser.GetSteamID().m_SteamID;
            bIsSteamConnected = true;
            Logging.Log($"Steam ID: {steamid}", "SteamAPI");
        }
        else
        {
            Logging.Error("Steam not initialized", "SteamAPI");
        }
    }

    public override void _Process(double delta)
    {
        SteamAPI.RunCallbacks();
    }


}
