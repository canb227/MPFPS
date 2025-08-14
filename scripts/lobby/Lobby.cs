using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;

public enum LobbyMessageType
{
    None = 0,
    JoinRequest = 1,
    JoinAccepted = 2,
    PeerListRequest = 3,
    PeerListResponse = 4,



    ERROR_AlreadyPeer = 200,
    ERROR_JoinRejected = 201,
}

public class Lobby
{
    public HashSet<ulong> LobbyPeers = new();
    public ulong LobbyHostSteamID;

    public bool bInLobby = false;
    public bool bIsLobbyHost = false;

    Callback<GameRichPresenceJoinRequested_t> m_GameRichPresenceJoinRequested;

    public delegate void InviteAccepted(ulong inviterSteamID);
    public static event InviteAccepted InviteAcceptedEvent;

    public delegate void JoinedToLobby(ulong hostSteamID);
    public static event JoinedToLobby JoinedToLobbyEvent;

    public delegate void NewLobbyPeerAdded(ulong newPlayerSteamID);
    public static event NewLobbyPeerAdded NewLobbyPeerAddedEvent;

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
        LobbyHostSteamID = Global.steamid;
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
        Logging.Log($"Sending Lobby Message with type {type.ToString()} to {toSteamID} | payload length:{data.Length}","Lobby");
        byte[] newData = new byte[data.Length + 1];
        newData[0] = (byte)type;
        data.CopyTo(newData, 1);
        SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
        identity.SetSteamID64(toSteamID);
        Global.network.SendData(newData, NetType.BYTES_LOBBY, identity);
    }

    public void HandleLobbyBytes(byte[] payload, ulong fromSteamID)
    {
        Logging.Log($"Lobby Message from {fromSteamID} has type {((LobbyMessageType)payload[0]).ToString()}","Lobby");
        LobbyMessageType type = (LobbyMessageType)payload[0];
        byte[] data = payload.Skip(1).ToArray();
        switch (type)
        {
            case LobbyMessageType.JoinRequest:
                if (!bInLobby)
                {
                    Logging.Warn("Ignoring unexpected Join Request! We are not in a lobby!", "Lobby");
                    break;
                }
                if (bIsLobbyHost)
                {
                    Logging.Log($"Accepting Join Request from {fromSteamID} as Host","Lobby");
                    if (LobbyPeers.Add(fromSteamID))
                    {
                        NewLobbyPeerAddedEvent?.Invoke(fromSteamID);
                        SendLobbyMessage([1], LobbyMessageType.JoinAccepted, fromSteamID);
                    }
                    else
                    {
                        SendLobbyMessage([1], LobbyMessageType.ERROR_AlreadyPeer, fromSteamID);
                    }
                }
                else
                {
                    Logging.Log($"Accepting Join Request from {fromSteamID} as non-host", "Lobby");
                    if (LobbyPeers.Add(fromSteamID))
                    {
                        NewLobbyPeerAddedEvent?.Invoke(fromSteamID);
                        SendLobbyMessage([0], LobbyMessageType.JoinAccepted, fromSteamID);
                    }
                    else
                    {
                        SendLobbyMessage([0], LobbyMessageType.ERROR_AlreadyPeer, fromSteamID);
                    }

                }                
                break;
            case LobbyMessageType.JoinAccepted:
                if (data[0]==1) // we just joined the host look alive
                {
                    Logging.Log($"Successfully joined to host {fromSteamID}. Sending request for other peers.", "Lobby");
                    LobbyPeers.Add(fromSteamID);
                    LobbyHostSteamID = fromSteamID;
                    SteamFriends.SetRichPresence("connect", fromSteamID.ToString());
                    JoinedToLobbyEvent?.Invoke(fromSteamID);
                    SendLobbyMessage([0], LobbyMessageType.PeerListRequest, fromSteamID);
                    break;
                }
                else if (data[0]==0) // established a peer connection to a non host
                {
                    Logging.Log($"Successfully joined to non-host {fromSteamID}. Sending request for other peers.", "Lobby");
                    LobbyPeers.Add(fromSteamID);
                    NewLobbyPeerAddedEvent?.Invoke(fromSteamID);
                    SendLobbyMessage([0], LobbyMessageType.PeerListRequest, fromSteamID);
                    break;
                }
                else
                {
                    throw new ArgumentException($"Malformed JoinAccepted Message | First Byte: {((int)data[0])}");
                }

            case LobbyMessageType.PeerListRequest:
                Logging.Log($"Request for peers from {fromSteamID}. Sending peer data.", "Lobby");
                foreach (ulong peerID in LobbyPeers)
                {
                    if (peerID == fromSteamID) continue;
                    SendLobbyMessage(BitConverter.GetBytes(peerID),LobbyMessageType.PeerListResponse,fromSteamID);
                }
                break;
            case LobbyMessageType.PeerListResponse:
                ulong newPeerID = BitConverter.ToUInt64(data, 0);
                if (LobbyPeers.Add(newPeerID))
                {
                    Logging.Log($"Received data on new shared peer {newPeerID}, requesting to join it", "Lobby");
                    SendLobbyMessage([0], LobbyMessageType.JoinRequest, newPeerID);
                }
                else
                {
                    Logging.Log($"Received data on duplicate peer {newPeerID}, ignoring.", "Lobby");
                }
                break;
            case LobbyMessageType.ERROR_AlreadyPeer:
                Logging.Log($"We tried to joined a peer who thinks we are already joined to. Locally adding them as peer and Requesting updated peer list.", "Lobby");
                LobbyPeers.Add(fromSteamID);
                SendLobbyMessage([0], LobbyMessageType.PeerListRequest, fromSteamID);
                break;
            case LobbyMessageType.ERROR_JoinRejected:
                Logging.Log($"Join Request rejected for unknown reasons.", "Lobby");
                break;
            default:
                throw new ArgumentException($"Malformed Lobby Message | First Byte: {((int)payload[0])} Cast As LobbyMessageType:{((LobbyMessageType)payload[0]).ToString()}");
                break;
        }
    }
}

