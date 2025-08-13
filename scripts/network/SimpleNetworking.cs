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
    NETREQUEST = 1,
    NETACCEPT = 2,



    DEBUG_UTF8 = 254,
    EMPTY = 255
}

public partial class SimpleNetworking : Node
{

    List<ulong> peers = new();

    public bool bLoopback = false;
    public bool bNetworkSimulation = false;
    public double dNetworkSimulationDelay = 0.1; 
    public double dNetworkSimulationDelayVariance = 0.1; 

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
        Logging.Log($"Session Failed with: {param.m_info.m_identityRemote} Reason: {param.m_info.m_eEndReason.ToString()}", "Network");
    }

    private void OnGameRichPresenceJoinRequested(GameRichPresenceJoinRequested_t param)
    {

        EResult result = SendData([0], NetType.NETREQUEST, ulong.Parse(param.m_rgchConnect));
        Logging.Log($"Session Request Sent To: {ulong.Parse(param.m_rgchConnect)} Result: {result}", "Network");
    }

    void OnSessionRequest(SteamNetworkingMessagesSessionRequest_t param)
    {
        if (peers.Contains(param.m_identityRemote.GetSteamID64()))
        {
            Logging.Log($"Session REJECTED with: {param.m_identityRemote.GetSteamID64()} Reason: Session Already Exists", "Network");
            return;
        }
        bool success = SteamNetworkingMessages.AcceptSessionWithUser(ref param.m_identityRemote);
        if (success)
        {
            peers.Add(param.m_identityRemote.GetSteamID64());
            EResult result = SendData([0], NetType.NETACCEPT, param.m_identityRemote.GetSteamID64());
            Logging.Log($"Session Established with: {param.m_identityRemote.GetSteamID64()}", "Network");
        }
        else
        {
            Logging.Log($"Session REJECTED with: {param.m_identityRemote.GetSteamID64()} Reason: UNKNOWN", "Network");
        }

    }

    public EResult SendData(byte[] data, NetType type, ulong toSteamID)
    {
        EResult result = EResult.k_EResultNone;
        if (bLoopback && toSteamID == Global.steamid)
        {
            Loopback(data, type, toSteamID);
            result = EResult.k_EResultOK;
        }
        else
        {
            byte[] payload = new byte[data.Length + 1];
            payload[0] = (byte)type;
            data.CopyTo(payload, 1);
            nint ptr = Marshal.AllocHGlobal(payload.Length);
            Marshal.Copy(payload, 0, ptr, payload.Length);
            SteamNetworkingIdentity remoteIdentity = new();
            remoteIdentity.SetSteamID64(toSteamID);
            result = SteamNetworkingMessages.SendMessageToUser(ref remoteIdentity, ptr, (uint)payload.Length, NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle, 0);
        }
        Logging.Log($" MSGSND | TO: {toSteamID} | TYPE: {type.ToString()} | SIZE: {data.Length} | RESULT: {result.ToString()}", "NetworkWire");
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
            ProcessData(data, type, steamMessage.m_identityPeer.GetSteamID64());
            SteamNetworkingMessage_t.Release(messages[i]);
        }
    }

    private void ProcessData(byte[] data, NetType type, ulong fromSteamID)
    {
        if (fromSteamID==Global.steamid) Logging.Log($" MSGRCV | FROM: LOOPBACK | TYPE: {type.ToString()} | SIZE: {data.Length}", "NetworkWire");
        else Logging.Log($" MSGRCV | FROM: {fromSteamID} | TYPE: {type.ToString()} | SIZE: {data.Length}", "NetworkWire");
        switch (type)
            {
                case NetType.NETREQUEST:
                    Logging.Log($"Session Request from: {fromSteamID}", "Network");
                    break;
                case NetType.NETACCEPT:
                    Logging.Log($"Session Established with: {fromSteamID}", "Network");
                    break;
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

    private async void Loopback(byte[] data, NetType type, ulong toSteamID)
    {
        if (bNetworkSimulation)
        {
            await ToSignal(GetTree().CreateTimer(dNetworkSimulationDelay + (new Random().NextDouble() * dNetworkSimulationDelayVariance)), "timeout");
        }
        ProcessData(data, type, toSteamID);
    }
}

