using Godot;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static Godot.HttpRequest;

public enum NetType
{
    ERROR = 0,
    DEBUG_UTF8 = 254,
    EMPTY = 255
}

public partial class SimpleNetworking : Node
{
    List<SteamNetworkingIdentity> peers = new();

    Callback<SteamNetworkingMessagesSessionRequest_t> SessionRequest;
    Callback<SteamNetworkingMessagesSessionFailed_t> SessionFailed;
    Callback<GameRichPresenceJoinRequested_t> m_GameRichPresenceJoinRequested;

    public override void _Ready()
    {
        SessionFailed = Callback<SteamNetworkingMessagesSessionFailed_t>.Create(OnSessionFailed);
        SessionRequest = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(OnSessionRequest);
        m_GameRichPresenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnGameRichPresenceJoinRequested);

        SteamFriends.SetRichPresence("connect", Global.steamid.ToString());
    }

    private void OnSessionFailed(SteamNetworkingMessagesSessionFailed_t param)
    {
        Logging.Log($"Session Failed with: {param.m_info.m_identityRemote} Reason: {((ESteamNetConnectionEnd)(param.m_info.m_eEndReason)).ToString()}", "Network");
    }

    private void OnGameRichPresenceJoinRequested(GameRichPresenceJoinRequested_t param)
    {
        Logging.Log($"Invite Accepted From: {ulong.Parse(param.m_rgchConnect)}", "Network");
        Logging.Log(ulong.Parse(param.m_rgchConnect).ToString(),"Network");
    }

    void OnSessionRequest(SteamNetworkingMessagesSessionRequest_t param)
    {
        SteamNetworkingIdentity id = param.m_identityRemote;
        peers.Add(id);
        if (!SteamNetworkingMessages.AcceptSessionWithUser(ref id))
        {
            Logging.Log("Session Error!","Network");
        }
        Logging.Log("Session Established.", "Network");
    }

    public EResult SendData(byte[] data, NetType type, ulong toSteamID)
    {
        byte[] payload = new byte[data.Length + 1];
        payload[0] = (byte)type;
        data.CopyTo(payload, 1);
        nint ptr = Marshal.AllocHGlobal(payload.Length);
        Marshal.Copy(payload, 0, ptr, payload.Length);
        CSteamID cSteamID = new CSteamID(toSteamID);
        SteamNetworkingIdentity remoteIdentity = new();
        remoteIdentity.SetSteamID(cSteamID);
        remoteIdentity.m_eType = ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_SteamID;
        EResult result = SteamNetworkingMessages.SendMessageToUser(ref remoteIdentity, ptr, (uint)payload.Length, NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle, 0);
        Logging.Log($" MSGSND | TO: {SteamFriends.GetFriendPersonaName(cSteamID)}({toSteamID}) | TYPE: {type.ToString()} | SIZE: {data.Length} | RESULT: {result.ToString()}", "NetworkWire");
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
            Logging.Log($" MSGRCV | FROM: {steamMessage.m_identityPeer.GetSteamID64()} | TYPE: {type.ToString()} | SIZE: {data.Length}", "NetworkWire");
            ProcessData(data, type, steamMessage.m_identityPeer.GetSteamID64());
            SteamNetworkingMessage_t.Release(messages[i]);
        }
    }

    private void ProcessData(byte[] data, NetType type, ulong fromSteamID)
    {
        switch (type)
            {
                case NetType.DEBUG_UTF8:
                    string msg = Encoding.UTF8.GetString(data);
                    Logging.Log($" HANDLER: DEBUG_UTF8 | PAYLOAD: {msg}", "NetworkWire");
                    break;
                case NetType.EMPTY:
                    Logging.Log($" HANDLER: EMPTY | NO PAYLOAD ", "NetworkWire");
                    break;
                default:
                    throw new NotImplementedException($" TYPE ERROR | FROM: {fromSteamID} | TYPE: {type} | SIZE: {data.Length}");
            }
    }

    /*
    private async void Loopback(byte[] data, NetType type, ulong toSteamID)
    {
        if (bNetworkSimulation)
        {
            await ToSignal(GetTree().CreateTimer(dNetworkSimulationDelay + (new Random().NextDouble() * dNetworkSimulationDelayVariance)), "timeout");
        }
        Logging.Log($" MSGRCV | FROM: LOOPBACK | TYPE: {type.ToString()} | SIZE: {data.Length}", "NetworkWire");
        ProcessData(data, type, toSteamID);
    }*/
}

