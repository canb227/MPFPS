using GameMessages;
using Godot;
using Limbo.Console.Sharp;
using Steamworks;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Using (MIT licensed) LimboConsole (https://github.com/limbonaut/limbo_console) to provide a simple console interface. 
/// LimboConsole config file at addons/limbo_console
/// May replace this with custom version at some point
/// </summary>
public partial class Console : Node
{
    public override void _Ready()
    {

        LimboConsole.SetEvalBaseInstance(this);

        //Register functions as commands 

        ////////////////////////////////////// DEV ///////////////////////////////////////////////
        LimboConsole.RegisterCommand(new Callable(this, MethodName.DEV_SetTickRate), "DEV_SetTickRate", "Dynamically changes tick rate. Almost certainly breaks stuff.");

        ////////////////////////////////////// GENERAL ///////////////////////////////////////////////
        LimboConsole.RegisterCommand(new Callable(this, MethodName.STATUS_Game), "status", "Prints the current game status.");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.OpenUserDataDirectory), "OpenUserDataDirectory", "Opens a native file explorer to the user data directory.");

        ////////////////////////////////////// LOGGING ///////////////////////////////////////////////
        LimboConsole.RegisterCommand(new Callable(this, MethodName.SetMaxLoggingVerbosity), "LOGGING_SetMaxLoggingVerbosity", "turns on all logging verbosity");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.ResetLoggingVerbosity), "LOGGING_ResetLoggingVerbosity", "resets log verbosity to default");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.SilenceLogCategory), "LOGGING_SilenceLogCategory", "silences a single log prefix");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.UnSilenceLogCategory), "LOGGING_UnSilenceLogCategory", "Unsilences a single log prefix");

        ////////////////////////////////////// NETWORKING ///////////////////////////////////////////////
        LimboConsole.RegisterCommand(new Callable(this, MethodName.DEV_NetworkLoopbackTestString));
        LimboConsole.RegisterCommand(new Callable(this, MethodName.DEV_NetworkSendTestString));
        LimboConsole.RegisterCommand(new Callable(this, MethodName.DEV_ConnectionInfo));

        ////////////////////////////////////// LOBBY ///////////////////////////////////////////////
        LimboConsole.RegisterCommand(new Callable(this, MethodName.STATUS_Lobby));
        LimboConsole.RegisterCommand(new Callable(this, MethodName.HostNewLobby));
        LimboConsole.RegisterCommand(new Callable(this, MethodName.JoinLobby));
        LimboConsole.RegisterCommand(new Callable(this, MethodName.LeaveLobby));

        ////////////////////////////////////// SESSION ///////////////////////////////////////////////
        LimboConsole.RegisterCommand(new Callable(this, MethodName.STATUS_Session));
        LimboConsole.RegisterCommand(new Callable(this, MethodName.DEV_StartSession));
        LimboConsole.RegisterCommand(new Callable(this, MethodName.DEV_EndSession));
        LimboConsole.RegisterCommand(new Callable(this, MethodName.DEV_StartGame));

        ////////////////////////////////////// INPUT ///////////////////////////////////////////////
        LimboConsole.RegisterCommand(new Callable(this, MethodName.DEV_parsekeystring));
        LimboConsole.RegisterCommand(new Callable(this, MethodName.bind));

        ////////////////////////////////////// IN GAME ///////////////////////////////////////////////
        LimboConsole.RegisterCommand(new Callable(this, MethodName.STATUS_LocalController));
        LimboConsole.RegisterCommand(new Callable(this, MethodName.objlookup));
        LimboConsole.RegisterCommand(new Callable(this, MethodName.spawn));
        LimboConsole.RegisterCommand(new Callable(this, MethodName.spawnpossess));
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
    public void SetMaxLoggingVerbosity()
    {
        LimboConsole.Info("Now printing ALL log messages to console.");
        Logging.UnSilenceAllPrefixes();
    }
    public void ResetLoggingVerbosity()
    {
        LimboConsole.Info("Now printing only standard log messages to console.");
        Logging.ResetSilencedPrefixesToDefault();
    }
    public void SilenceLogCategory(string category)
    {
        LimboConsole.Info($"Silencing prefix [{category}]");
        Logging.SilencePrefix(category);
    }
    public void UnSilenceLogCategory(string category)
    {
        LimboConsole.Info($"UnSilencing prefix [{category}]");
        Logging.UnSilencePrefix(category);
    }


    ////////////////////////////////////// NETWORKING ///////////////////////////////////////////////
    public void DEV_ConnectionInfo(string ids)
    {
        ulong id = ulong.Parse(ids);
        LimboConsole.Info($"Status of connection with peer: {id}");
        SteamNetworkingIdentity sid = NetworkUtils.SteamIDToIdentity(id);
        SteamNetworkingMessages.GetSessionConnectionInfo(ref sid, out SteamNetConnectionInfo_t info, out SteamNetConnectionRealTimeStatus_t status);
        LimboConsole.Info($"id:{info.m_identityRemote.GetSteamID64()} state:{info.m_eState} ping:{status.m_nPing} Endreason:{info.m_eEndReason} string:{info.m_szEndDebug} sentunacked:{status.m_cbSentUnackedReliable} pending:{status.m_cbPendingReliable}");
    }

    public void DEV_NetworkLoopbackTestString(string message)
    {
        LimboConsole.Info($"Sending string \"{message}\" on loopback...");
        SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
        identity.SetSteamID64(Global.steamid);
        Global.network.SendData(Encoding.UTF8.GetBytes(message), NetType.DEBUG_UTF8, identity);
    }

    public void DEV_NetworkSendTestString(string id, string message)
    {
        LimboConsole.Info($"Sending string \"{message}\" to {id}...");
        SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
        identity.SetSteamID64(ulong.Parse(id));
        Global.network.SendData(Encoding.UTF8.GetBytes(message), NetType.DEBUG_UTF8, identity);
    }

    public void NET_EnableBandwidthTracker()
    {
        LimboConsole.Info("Network Bandwidth Tracker now reporting.");
        Global.network.BandwidthTrackerEnabled = true;
    }

    public void NET_DisableBandwidthTracker()
    {
        LimboConsole.Info("Network Bandwidth Tracker disabled.");
        Global.network.BandwidthTrackerEnabled = false;
    }

    ////////////////////////////////////// SESSSION ///////////////////////////////////////////////
    public void STATUS_Session()
    {
        LimboConsole.Info("Session Status");
        LimboConsole.Info($"  In Session?: {Global.GameSession != null}");
        if (Global.GameSession == null) return;
        LimboConsole.Info($"  Number of players in session: {Global.GameSession.playerData.Count}");
        LimboConsole.Info($"  Session Player List --------------------------------------------------------------");
        foreach (var player in Global.GameSession.playerData)
        {
            if (player.Key == Global.steamid)
            {
                LimboConsole.Info($"ME  Name:{SteamFriends.GetFriendPersonaName(new CSteamID(player.Key))} | ID:{player.Key} | State: {player.Value.state} | DblCheckID: {player.Value.steamID}");
            }
            else
            {
                LimboConsole.Info($"    Name:{SteamFriends.GetFriendPersonaName(new CSteamID(player.Key))} | ID:{player.Key} | State: {player.Value.state} | DblCheckID: {player.Value.steamID}");
            }
        }
    }

    public void DEV_StartSession()
    {
        LimboConsole.Info("Direct starting new session...");
        Global.GameSession = new(Global.Lobby.lobbyPeers.ToList(), Global.Lobby.LobbyHostSteamID);
    }

    public void DEV_EndSession()
    {
        LimboConsole.Info("Direct ending session...");
        Global.GameSession.EndSession();
    }

    public void DEV_StartGame()
    {
        if (Global.GameSession.sessionAuthority == Global.steamid)
        {
            LimboConsole.Info("Direct starting game...");
            Global.GameSession.BroadcastSessionMessage([0], SessionMessageType.COMMAND_STARTGAME);
        }
        else
        {
            LimboConsole.Error("Only the host can start the game.");
        }

    }
    ////////////////////////////////////// LOBBY ///////////////////////////////////////////////
    public void STATUS_Lobby()
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

    public void HostNewLobby()
    {
        LimboConsole.Info("Hosting a new lobby...");
        Global.Lobby.HostNewLobby(true);
    }

    public void JoinLobby(string ids)
    {

        LimboConsole.Info($"Attempting to join lobby hsoted by: {ids}");
        Global.Lobby.AttemptJoinToLobby(ulong.Parse(ids),true);
    }

    public void LeaveLobby()
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
        LimboConsole.Info("P");
        Global.InputMap.BindKeyString(keyString, actionName);
    }

    //////////////////////////////////////// In - Game ////////////////////////////////////////////////////////////////////////////
    
    public void STATUS_LocalController()
    {
        PlayerController pc = Global.GameSession.playerData[Global.steamid].playerController;
        LimboConsole.Info("Local Player Controller Status");
        LimboConsole.Info($"  UID: {pc.GetUniqueID()}");
        LimboConsole.Info($"  Possessing: {(pc.possessedCharacter!=null? pc.possessedCharacter.GetUID() : "null" )}");

    }

    public void objlookup(ulong UID)
    {
        GameObject obj = Global.world.entities[UID];
        LimboConsole.Info($"Found data for obj {UID}");

    }

    public void spawn(string objName)
    {
        LimboConsole.Info($"Attempting to spawn object with type: {objName}");
        SpawnGameObjectData msg = new SpawnGameObjectData();
        msg.Authority = Global.world.defaultAuthority;
        msg.ObjName = objName;
        msg.Sender = Global.steamid;
        msg.SpawnX = 0;
        msg.SpawnY = 10;
        msg.SpawnZ = 0;
        SpawnRequestor.SpawnGameObject(msg);
    }

    public void spawnpossess(string objName)
    {
        LimboConsole.Info($"Attempting to spawn object with type: {objName}");
        SpawnGameObjectData msg = new SpawnGameObjectData();
        msg.Authority = Global.world.defaultAuthority;
        msg.ObjName = objName;
        msg.Sender = Global.steamid;
        msg.RequestorAutoPossess = Global.steamid;
        msg.SpawnX = 0;
        msg.SpawnY = 10;
        msg.SpawnZ = 0;
        SpawnRequestor.SpawnGameObject(msg);
    }
}
