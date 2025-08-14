using Godot;
using Limbo.Console.Sharp;
using NetworkMessages;
using Steamworks;
using System.Collections.Generic;
using System.Security.Principal;
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

        LimboConsole.RegisterCommand(new Callable(this, MethodName.status), "status", "Prints the current game status.");

        LimboConsole.RegisterCommand(new Callable(this, MethodName.conninfo), "conninfo", "shoot me");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.SetMaxLoggingVerbosity), "LOGGING_SetMaxLoggingVerbosity", "turns on all logging verbosity");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.ResetLoggingVerbosity), "LOGGING_ResetLoggingVerbosity", "resets log verbosity to default");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.SilenceLogCategory), "LOGGING_SilenceLogCategory", "silences a single log prefix");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.UnSilenceLogCategory), "LOGGING_UnSilenceLogCategory", "Unsilences a single log prefix");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.loopbacktest), "loopbacktest", "network");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.send), "send", "network");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.lobbystatus), "lobbystatus", "lobbystatus");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.hostlobby), "hostlobby", "hostlobby");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.joinlobby), "joinlobby", "joinlobby");

    }
    public void status()
    {
        LimboConsole.Info("Game Status");
        LimboConsole.Info($"  Game Version: {Global.VERSION}");
        LimboConsole.Info($"  Connected to Steam: {Global.bIsSteamConnected}");
        LimboConsole.Info($"  SteamID: {Global.steamid}");
    }

    public void lobbystatus()
    {
        LimboConsole.Info("Lobby Status");
        LimboConsole.Info($"  In Lobby?: {Global.Lobby.bInLobby}");
        if (!Global.Lobby.bInLobby) return;
        LimboConsole.Info($"  Is Lobby Host?: {Global.Lobby.bIsLobbyHost}");
        LimboConsole.Info($"  Lobby Host SteamID: {Global.Lobby.LobbyHostSteamID}");
        LimboConsole.Info($"  Number of players in lobby: {Global.Lobby.LobbyPeers.Count+1}");
        LimboConsole.Info($"  Peer List --------------------------------------------------------------");
        foreach(ulong peer in Global.Lobby.LobbyPeers)
        {
            LimboConsole.Info($"    Name:{SteamFriends.GetFriendPersonaName(new CSteamID(peer))} | ID:{peer}");
        }
    }

    public void hostlobby()
    {
        LimboConsole.Info("Hosting a new lobby...");
        Global.Lobby.HostNewLobby();
    }

    public void joinlobby(string ids)
    {

        LimboConsole.Info($"Attempting to join lobby hsoted by: {ids}");
        Global.Lobby.AttemptJoinToLobby(ulong.Parse(ids));
    }

    public void conninfo(string ids)
    {
        ulong id = ulong.Parse(ids);
        LimboConsole.Info($"Status of connection with peer: {id}");
        SteamNetworkingIdentity sid = NetworkUtils.SteamIDToIdentity(id);
        SteamNetworkingMessages.GetSessionConnectionInfo(ref sid,out SteamNetConnectionInfo_t info, out SteamNetConnectionRealTimeStatus_t status);
        LimboConsole.Info($"id:{info.m_identityRemote.GetSteamID64()} state:{info.m_eState} ping:{status.m_nPing} Endreason:{info.m_eEndReason} string:{info.m_szEndDebug} sentunacked:{status.m_cbSentUnackedReliable} pending:{status.m_cbPendingReliable}");
    }

    public void loopbacktest(string message)
    {
        LimboConsole.Info($"Sending test on loopback...");
        SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
        identity.SetSteamID64(Global.steamid);
        Global.network.SendData(Encoding.UTF8.GetBytes(message), NetType.DEBUG_UTF8, identity);
    }

 

    public void send(string id, string message)
    {
        LimboConsole.Info($"Sending test on network...");
        SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
        identity.SetSteamID64(ulong.Parse(id));
        Global.network.SendData(Encoding.UTF8.GetBytes(message), NetType.DEBUG_UTF8, identity);
    }


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
}
