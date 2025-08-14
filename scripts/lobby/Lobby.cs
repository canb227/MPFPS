using Steamworks;
using System;
using System.Collections.Generic;
using System.Security.Principal;

public enum LobbyMessageType
{
    None = 0,
    JoinRequest = 1,
    JoinAccepted = 2,
    PeerListRequest = 3,
    PeerListResponse = 4,
}

public class Lobby
{
    public HashSet<ulong> LobbyPeers = new();
    public ulong LobbyHost;

    public bool bInLobby = false;
    public bool bIsLobbyHost = false;

    Callback<GameRichPresenceJoinRequested_t> m_GameRichPresenceJoinRequested;

    public delegate void InviteAccepted(ulong inviterSteamID);
    public static event InviteAccepted InviteAcceptedEvent;

    public delegate void JoinedToLobby(ulong hostSteamID);
    public static event JoinedToLobby JoinedToLobbyEvent;

    public Lobby()
    {
        m_GameRichPresenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnGameRichPresenceJoinRequested);
    }

    public void HostNewLobby()
    {
        Logging.Log($"Hosting a new lobby and enabling steam rich presence.", "Lobby");
        SteamFriends.SetRichPresence("connect", Global.steamid.ToString());
        bInLobby = true;
        bIsLobbyHost = true;
        JoinedToLobbyEvent?.Invoke(Global.steamid);
    }

    private void OnGameRichPresenceJoinRequested(GameRichPresenceJoinRequested_t param)
    {
        Logging.Log($"Invite Accepted From: {ulong.Parse(param.m_rgchConnect)}", "Lobby");
        Logging.Log(ulong.Parse(param.m_rgchConnect).ToString(), "Lobby");
        InviteAcceptedEvent?.Invoke(ulong.Parse(param.m_rgchConnect));
        SendLobbyMessage([0],LobbyMessageType.JoinRequest, ulong.Parse(param.m_rgchConnect));
    }

    public void AttemptJoinToLobby(ulong steamID)
    {
        Logging.Log($"Attempting to join lobby hosted by: {steamID}", "Lobby");
        SendLobbyMessage([0], LobbyMessageType.JoinRequest, steamID);
    }

    public void SendLobbyMessage(byte[] data, LobbyMessageType type, ulong toSteamID)
    {
        Logging.Log($"Sending Lobby Message with type {type.ToString()} to {toSteamID} | data length:{data.Length}","Lobby");
        byte[] newData = new byte[data.Length + 1];
        newData[0] = (byte)type;
        data.CopyTo(newData, 1);
        SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
        identity.SetSteamID64(toSteamID);
        Global.network.SendData(newData, NetType.LOBBY, identity);
    }

    public void HandleLobbyMessageData(byte[] data, ulong fromSteamID)
    {
        Logging.Log($"Lobby Message from {fromSteamID} has type {((LobbyMessageType)data[0]).ToString()}","Lobby");
        switch ((LobbyMessageType)data[0])
        {
            case LobbyMessageType.JoinRequest:
                if (bInLobby && bIsLobbyHost)
                {
                    Logging.Log($"Accepting Join Request from {fromSteamID}","Lobby");
                    LobbyPeers.Add(fromSteamID);
                    SendLobbyMessage([0], LobbyMessageType.JoinAccepted, fromSteamID);
                }
                else
                {
                    Logging.Warn("Ignoring unexpected Join Request! We are not hosting anything.","Lobby");
                }                
                break;
            case LobbyMessageType.JoinAccepted:
                Logging.Log($"Successfully joined to {fromSteamID}", "Lobby");
                LobbyPeers.Add(fromSteamID);
                LobbyHost = fromSteamID;
                JoinedToLobbyEvent?.Invoke(fromSteamID);
                break;
            case LobbyMessageType.PeerListRequest:
                foreach(ulong peerID in LobbyPeers)
                {
                    SendLobbyMessage(BitConverter.GetBytes(peerID),LobbyMessageType.PeerListResponse,fromSteamID);
                }
                break;
            case LobbyMessageType.PeerListResponse:
                ulong newPeerID = BitConverter.ToUInt64(data, 1);
                if (LobbyPeers.Contains(newPeerID))
                {
                    Logging.Log($"Ignoring shared peer {newPeerID}, they are already our peer.", "Lobby");
                }
                else
                {
                    Logging.Log($"Attempting to join to shared peer {newPeerID}", "Lobby");
                    SendLobbyMessage([0], LobbyMessageType.JoinRequest, newPeerID);
                }
                break;
            default:
                throw new ArgumentException($"Malformed Lobby Message | First Byte: {((int)data[0]).ToString()}");
                break;
        }
    }

}

