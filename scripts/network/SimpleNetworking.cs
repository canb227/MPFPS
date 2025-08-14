using Godot;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using static Godot.HttpRequest;
using static System.Runtime.InteropServices.JavaScript.JSType;

public enum NetType
{
    ERROR = 0,
    
    //1
    LOBBY = 1,
        //Byte Types

    //100
    
        //IMessage Types

    //200

        //Other Types

    DEBUG_UTF8 = 254,
    EMPTY = 255
}

public partial class SimpleNetworking : Node
{
    Callback<SteamNetworkingMessagesSessionRequest_t> SessionRequest;
    Callback<SteamNetworkingMessagesSessionFailed_t> SessionFailed;

    Callback<SteamRelayNetworkStatus_t> RelayNetworkStatusChanged;
    Callback<SteamNetConnectionStatusChangedCallback_t> ConnectionStatusChanged;

    private bool bNetworkSimulation = false;
    private double dNetworkSimulationDelay = 0.05f;
    private double dNetworkSimulationDelayVariance = 0.1f;

    public override void _Ready()
    {
        SessionFailed = Callback<SteamNetworkingMessagesSessionFailed_t>.Create(OnSessionFailed);
        SessionRequest = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(OnSessionRequest);
        RelayNetworkStatusChanged = Callback<SteamRelayNetworkStatus_t>.Create(OnRelayNetworkStatusChanged);
        SteamNetworkHealthManager();
    }

    public async void SteamNetworkHealthManager()
    {
        while (true)
        {
            await ToSignal(GetTree().CreateTimer(1), "timeout");
            
            SteamNetworkingUtils.GetRelayNetworkStatus(out SteamRelayNetworkStatus_t details);
            if (details.m_eAvail!=ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Current)
            {
                Logging.Log("Force retrying relay network access...","NetworkRelay");
                SteamNetworkingUtils.InitRelayNetworkAccess();
            }
        }
    }

    private void OnRelayNetworkStatusChanged(SteamRelayNetworkStatus_t param)
    {
        Logging.Log($"Connection Status to Steam Relay Network has changed: {param.m_eAvail}","NetworkRelay");
    }

    private void OnSessionFailed(SteamNetworkingMessagesSessionFailed_t param)
    {
        param.m_info.m_identityRemote.ToString(out string idstring);
        Logging.Log($"Session Failed with: {idstring} Reason: {((ESteamNetConnectionEnd)(param.m_info.m_eEndReason)).ToString()} DEBUG:{param.m_info.m_szEndDebug}", "NetworkSession");
    }



    void OnSessionRequest(SteamNetworkingMessagesSessionRequest_t param)
    {
        bool sessionEstablished = SteamNetworkingMessages.AcceptSessionWithUser(ref param.m_identityRemote);
        if (sessionEstablished)
        {
            param.m_identityRemote.ToString(out string idstring);
            Logging.Log($"New Session Established with {idstring}","NetworkSession");
        }
        else
        {
            param.m_identityRemote.ToString(out string idstring);
            Logging.Log($"Failed to Establish New Session With {idstring}", "NetworkSession");
        }
    }

    public EResult SendData(byte[] data, NetType type, SteamNetworkingIdentity remoteIdentity)
    {
        byte[] payload = new byte[data.Length + 1];
        payload[0] = (byte)type;
        data.CopyTo(payload, 1);
        nint ptr = Marshal.AllocHGlobal(payload.Length);
        Marshal.Copy(payload, 0, ptr, payload.Length);
        remoteIdentity.ToString(out string idstring);
        EResult result = SteamNetworkingMessages.SendMessageToUser(ref remoteIdentity, ptr, (uint)payload.Length, NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle, 0);
        Logging.Log($" MSGSND | TO: {SteamFriends.GetFriendPersonaName(remoteIdentity.GetSteamID())}({idstring}) | TYPE: {type.ToString()} | SIZE: {data.Length} | RESULT: {result.ToString()}", "NetworkWire");
        return result;
    }

    public override void _Process(double delta)
    {
        nint[] messages = new nint[100];
        for (int i = 0; i < SteamNetworkingMessages.ReceiveMessagesOnChannel(0, messages, 100); i++)
        {
            SteamNetworkingMessage_t steamMessage = SteamNetworkingMessage_t.FromIntPtr(messages[i]);
            byte[] payload = new byte[steamMessage.m_cbSize];
            Marshal.Copy(steamMessage.m_pData, payload, 0, payload.Length);
            NetType type = (NetType)payload[0];
            byte[] data = payload.Skip(1).ToArray();
            steamMessage.m_identityPeer.ToString(out string idstring);
            Logging.Log($" MSGRCV | FROM: {idstring} | TYPE: {type.ToString()} | SIZE: {data.Length}", "NetworkWire");
            ProcessData(data, type, steamMessage.m_identityPeer.GetSteamID64());
            SteamNetworkingMessage_t.Release(messages[i]);
        }
    }

    private void ProcessData(byte[] data, NetType type, ulong fromSteamID)
    {
        switch (type)
            {
            case NetType.LOBBY:
                Global.Lobby.HandleLobbyMessageData(data, fromSteamID);
                break;
            case NetType.DEBUG_UTF8:
                Logging.Log($"Reencoded (UTF8) Message: {Encoding.UTF8.GetString(data)}","NetworkDEBUG");
                break;
            default:
                throw new NotImplementedException($" TYPE ERROR | FROM: {fromSteamID} | TYPE: {type} | SIZE: {data.Length}");
            }
    }

    
    private async void Loopback(byte[] data, NetType type, ulong toSteamID)
    {
        if (bNetworkSimulation)
        {
            await ToSignal(GetTree().CreateTimer(dNetworkSimulationDelay + (new Random().NextDouble() * dNetworkSimulationDelayVariance)), "timeout");
        }
        Logging.Log($" MSGRCV | FROM: LOOPBACK | TYPE: {type.ToString()} | SIZE: {data.Length}", "NetworkWire");
        ProcessData(data, type, toSteamID);
    }
}

