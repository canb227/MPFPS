

using Godot;
using Limbo.Console.Sharp;
using SteamMultiplayerPeerCSharp;
using Steamworks;
using System;
using System.Linq;



/// <summary>
/// Using (MIT licensed) LimboConsole (https://github.com/limbonaut/limbo_console) to provide a simple console interface. 
/// LimboConsole config file at addons/limbo_console
/// May replace this with custom version at some point
/// </summary>
public partial class Console : Node
{
    public override void _Ready()
    {
        //Register functions as commands 

        ////////////////////////////////////// GENERAL ///////////////////////////////////////////////
        LimboConsole.RegisterCommand(new Callable(this, MethodName.STATUS_Game), "status", "Prints the current game status.");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.OpenUserDataDirectory), "OpenUserDataDirectory", "Opens a native file explorer to the user data directory.");

        ////////////////////////////////////// LOGGING ///////////////////////////////////////////////
        LimboConsole.RegisterCommand(new Callable(this, MethodName.logging_verbosity_max), "logging verbosity max", "turns on all logging verbosity");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.logging_verbosity_reset), "logging verbosity reset", "resets log verbosity to default");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.logging_silence), "logging silence", "silences a single log prefix");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.logging_unsilence), "logging unsilence", "Unsilences a single log prefix");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.logging_stats), "logging stats", "Prints logging stats to console");

        ////////////////////////////////////// NETWORKING ///////////////////////////////////////////////
        LimboConsole.RegisterCommand(new Callable(this, MethodName.net_ConnectionInfo),"net connectioninfo","DEV COMMAND - Dumps info on a Steam Connection to a peer");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.net_TrackBandwidth), "net trackbandwidth", "DEV COMMAND - enables or disables bandwidth tracking");

        ////////////////////////////////////// LOBBY ///////////////////////////////////////////////
        LimboConsole.RegisterCommand(new Callable(this, MethodName.lobby_status),"lobby status","Prints info about our current lobby, if we're in one.");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.lobby_host),"lobby host","Hosts a new lobby, leaving our current lobby if we're in one.");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.lobby_join),"lobby join","Joins a lobby hosted by the given SteamID, if there is one.");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.lobby_leave),"lobby leave","Leaves our current lobby");

        ////////////////////////////////////// INPUT ///////////////////////////////////////////////
        LimboConsole.RegisterCommand(new Callable(this, MethodName.DEV_parsekeystring));
        LimboConsole.RegisterCommand(new Callable(this, MethodName.bind));

        ////////////////////////////////////// IN GAME ///////////////////////////////////////////////
        LimboConsole.RegisterCommand(new Callable(this, MethodName.spawn),"spawn","Spawns a new instance of the given object type in front of the local player.");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.destroy),"destroy","Destroys the object with the given ID");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.impulse), "impulse", "Applies a random physics impulse to the object with the given ID");
    }


    ////////////////////////////////////// GENERAL ///////////////////////////////////////////////
    public void STATUS_Game()
    {
        LimboConsole.Info("Game Status");
        LimboConsole.Info($"  Game Version: {Global.VERSION}");
        LimboConsole.Info($"  Connected to Steam: {Global.bIsSteamConnected}");
        LimboConsole.Info($"  SteamID: {Global.steamid}");
    }

    public void OpenUserDataDirectory()
    {
        LimboConsole.Info("Asking Operating System to open file browser to user save and config directory.");
        OS.ShellOpen(OS.GetUserDataDir());
    }

    ////////////////////////////////////// DEV ///////////////////////////////////////////////

    public void DEV_SetTickRate (int rate)
    {
        LimboConsole.Info($"Setting Tick Rate to {rate}");
        Engine.PhysicsTicksPerSecond = rate;
    }

    ////////////////////////////////////// LOGGING ///////////////////////////////////////////////
    public void logging_verbosity_max()
    {
        LimboConsole.Info("Now printing ALL log messages to console.");
        Logging.UnSilenceAllPrefixes();
    }
    public void logging_verbosity_reset()
    {
        LimboConsole.Info("Now printing only standard log messages to console.");
        Logging.ResetSilencedPrefixesToDefault();
    }
    public void logging_silence(string category)
    {
        LimboConsole.Info($"Silencing prefix [{category}]");
        Logging.SilencePrefix(category);
    }
    public void logging_unsilence(string category)
    {
        LimboConsole.Info($"UnSilencing prefix [{category}]");
        Logging.UnSilencePrefix(category);
    }
    public void logging_stats()
    {
        LimboConsole.Info("Dumping logging stats to console...");
        LimboConsole.Info("Category                       | Silenced?  | # Printed  | # Silenced");
        foreach (var entry in Logging.categories)
        {
            string paddedName = entry.Key.PadRight(30);
            string silenced = entry.Value.silenced.ToString().PadRight(10);
            string timesPrinted = entry.Value.timesPrinted.ToString().PadRight(10);
            string timesSilenced = entry.Value.timesSilenced.ToString().PadRight(10);
            LimboConsole.Info($"{paddedName} | {silenced} | {timesPrinted} | {timesSilenced}");
        }

    }

    ////////////////////////////////////// NETWORKING ///////////////////////////////////////////////
    public void net_ConnectionInfo(string ids)
    {
        ulong id = ulong.Parse(ids);
        LimboConsole.Info($"Status of connection with peer: {id}");
        SteamNetworkingIdentity sid = NetworkUtils.SteamIDToIdentity(id);
        SteamNetworkingMessages.GetSessionConnectionInfo(ref sid, out SteamNetConnectionInfo_t info, out SteamNetConnectionRealTimeStatus_t status);
        LimboConsole.Info($"id:{info.m_identityRemote.GetSteamID64()} state:{info.m_eState} ping:{status.m_nPing} Endreason:{info.m_eEndReason} string:{info.m_szEndDebug} sentunacked:{status.m_cbSentUnackedReliable} pending:{status.m_cbPendingReliable}");
    }

    public void net_TrackBandwidth(bool tracking, bool trackLoopback)
    {
        if (tracking)
        {
            LimboConsole.Info("Network Bandwidth Tracker now reporting.");
            Global.network.BandwidthTrackerEnabled = true;
        }
        else
        {
            LimboConsole.Info("Network Bandwidth Tracker now disabled.");
            Global.network.BandwidthTrackerEnabled = false;
        }
        Global.network.BandwidthTrackerCountLoopbackSend = trackLoopback;
        Global.network.BandwidthTrackerCountLoopbackReceive = trackLoopback;
    }

    ////////////////////////////////////// LOBBY ///////////////////////////////////////////////
    public void lobby_status()
    {
        LimboConsole.Info("Lobby Status");
        LimboConsole.Info($"  In Lobby?: {Global.Lobby.bInLobby}");
        if (!Global.Lobby.bInLobby) return;
        LimboConsole.Info($"  Is Lobby Host?: {Global.Lobby.bIsLobbyHost}");
        LimboConsole.Info($"  Lobby Host SteamID: {Global.Lobby.LobbyHostSteamID}");
        LimboConsole.Info($"  Number of peers in lobby: {Global.Lobby.lobbyPeers.Count}");
        LimboConsole.Info($"  Peer List --------------------------------------------------------------");
        foreach (ulong peer in Global.Lobby.lobbyPeers)
        {
            LimboConsole.Info($"    Name:{SteamFriends.GetFriendPersonaName(new CSteamID(peer))} | ID:{peer}");
        }
    }

    public void lobby_host()
    {
        LimboConsole.Info("Hosting a new lobby...");
        Global.Lobby.HostNewLobby(true);
    }

    public void lobby_join(string ids)
    {

       LimboConsole.Info($"Attempting to join lobby hsoted by: {ids}");
       Global.Lobby.AttemptJoinToLobby(ulong.Parse(ids),true);
    }

    public void lobby_leave()
    {
        LimboConsole.Info($"Leaving current lobby...");
        Global.Lobby.LeaveLobby(true);
    }


    ////////////////////////////////////// INPUT ///////////////////////////////////////////////
    public void DEV_parsekeystring(string keyString)
    {
        try
        {
            Key key = (Key)Enum.Parse(typeof(Key), keyString);
            LimboConsole.Info($"You entered string '{keyString}' - we parsed that to KEY_STRING: {key.ToString()} KEY_CODE: {(int)key}");
        }
        catch (Exception e)
        {
            LimboConsole.Error($"Key parse failed: {e.ToString()}");
        }
    }

    public void bind(string keyString, string actionName)
    {
        Key key = (Key)Enum.Parse(typeof(Key), keyString);
        LimboConsole.Info($"Attempting to bind Key: {key.ToString()} to action: {actionName}");
        Global.InputMap.BindKeyString(keyString, actionName);
    }

    ////////////////////////////////////// IN GAME ///////////////////////////////////////////////
    
    public void spawn(string objectName)
    {
        IGameObject obj = GameObjectLoader.LoadObjectByTypeName(objectName, out GameObjectType type);
        Global.gameState.SpawnObjectAsAuth(obj,type);
        var playerForwardVector = -Global.gameState.GetLocalPlayerCharacter().GlobalTransform.Basis.Z.Normalized();
        var spawnPosition = Global.gameState.GetLocalPlayerCharacter().GlobalPosition + (playerForwardVector * 5);
        (obj as Node3D).GlobalPosition = spawnPosition;
    }

    public void destroy(ulong id)
    {
        Global.gameState.DestroyAsAuth(id);
    }

    public void impulse(ulong id)
    {
        if (Global.gameState.GameObjects.TryGetValue(id, out var obj))
        {
            if (obj is GOBaseRigidBody phys)
            {
                Vector3 forceVector = new Vector3(Random.Shared.Next(-10, 10), Random.Shared.Next(-10, 10), Random.Shared.Next(-10, 10));
                phys.ApplyCentralImpulse(forceVector);
            }
        }
    }
}
