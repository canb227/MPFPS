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
        LimboConsole.RegisterCommand(new Callable(this, MethodName.TestConsole1), "TestConsole1", "Runs a simple function to test the console.");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.TestConsole2), "TestConsole2", "Runs a test with optional (string) arg");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.TestConsole3), "TestConsole3", "Runs a test with one mandatory (int) args");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.TestConsole4), "TestConsole4", "Runs a test with two mandatory (string) args");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.status), "status", "Prints the current game status.");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.Chat), "chat", "Sends a chat message to all peers");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.Chat), "say", "Sends a chat message to all peers");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.conninfo), "conninfo", "shoot me");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.SetMaxLoggingVerbosity), "LOGGING_SetMaxLoggingVerbosity", "turns on all logging verbosity");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.ResetLoggingVerbosity), "LOGGING_ResetLoggingVerbosity", "resets log verbosity to default");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.SilenceLogCategory), "LOGGING_SilenceLogCategory", "silences a single log prefix");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.UnSilenceLogCategory), "LOGGING_UnSilenceLogCategory", "Unsilences a single log prefix");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.loopbacktest), "loopbacktest", "snetwork");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.send), "send", "snetwork");
        LimboConsole.RegisterCommand(new Callable(this, MethodName.ping), "ping", "snetwork");
    }
    public void status()
    {
        LimboConsole.Info("Game Status");
        LimboConsole.Info($"  Game Version: {Global.VERSION}");
        LimboConsole.Info($"  Connected to Steam: {Global.bIsSteamConnected}");
        LimboConsole.Info($"  Status of connection to Steam Relay Network: {Global.network.GetSteamRelayNetworkStatus()}");
        LimboConsole.Info($"  SteamID: {Global.steamid}");
    }

    public void conninfo(string ids)
    {
        ulong id = ulong.Parse(ids);
        LimboConsole.Info($"Status of connection with peer: {id}");
        SteamNetworkingIdentity sid = NetworkUtils.SteamIDToIdentity(id);
        SteamNetworkingMessages.GetSessionConnectionInfo(ref sid,out SteamNetConnectionInfo_t info, out SteamNetConnectionRealTimeStatus_t status);
        LimboConsole.Info($"id:{info.m_identityRemote.GetSteamID64()} state:{info.m_eState} ping:{status.m_nPing} realtimeState:{status.m_eState} sentunacked:{status.m_cbSentUnackedReliable} pending:{status.m_cbPendingReliable}");
    }

    public void loopbacktest(string message)
    {
        LimboConsole.Info($"Sending test on loopback...");
        SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
        identity.SetSteamID64(Global.steamid);
        Global.snetwork.SendData(Encoding.UTF8.GetBytes(message), NetType.DEBUG_UTF8, identity);
    }

    public void ping(string id)
    {
        LimboConsole.Info($"Sending dummy test on snetwork...");
        SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
        identity.SetSteamID64(ulong.Parse(id));
        Global.snetwork.SendDummyMessage(identity);
    }

    public void send(string id, string message)
    {
        LimboConsole.Info($"Sending test on snetwork...");
        SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
        identity.SetSteamID64(ulong.Parse(id));
        Global.snetwork.SendData(Encoding.UTF8.GetBytes(message), NetType.DEBUG_UTF8, identity);
    }

    public void Chat(string message)
    {
        ChatManager.Chat(message);
    }
    public void SetMaxLoggingVerbosity()
    {
        LimboConsole.Info("Now printing ALL log messages to console.");
        Logging.SilencedPrefixes = new();
    }
    public void ResetLoggingVerbosity()
    {
        LimboConsole.Info("Now printing only standard log messages to console.");
        Logging.SilencedPrefixes = Logging.SilencedPrefixesDefault;
    }
    public void SilenceLogCategory(string category)
    {
        LimboConsole.Info($"Silencing prefix [{category}]");
        Logging.SilencedPrefixes.Add(category);
    }
    public void UnSilenceLogCategory(string category)
    {
        LimboConsole.Info($"UnSilencing prefix [{category}]");
        Logging.SilencedPrefixes.Remove(category);
    }
    public void TestConsole1()
    {
        LimboConsole.Info("Info is a standard line.");
        LimboConsole.PrintBoxed("boxed is fun");
        LimboConsole.PrintLine("This prints to Godot output too.", true);
        LimboConsole.PrintLine("This is the same as Info.", false);
        LimboConsole.Error("This is Error.");
        LimboConsole.Warn("This is Warn");
    }

    public void TestConsole2(string optionalArgument)
    {
        LimboConsole.Info("Singular nullable arguments are considered optional.");
        LimboConsole.Info($"Optional Argument: {optionalArgument}");
    }


    public void TestConsole3(int mandatoryArgument)
    {
        LimboConsole.Info("non-nullable (int) parameters are considered mandatory");
        LimboConsole.Info($"Mandatory Argument: {mandatoryArgument}");
    }

    public void TestConsole4(string optionalArgument1, string optionalArgument2)
    {
        LimboConsole.Info("multiple of any argument makes them all required");
        LimboConsole.Info($"Optional Argument: {optionalArgument1} and {optionalArgument2}");

    }

}
