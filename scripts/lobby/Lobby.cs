using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The first byte of the data we get in LOBBY network messages is an additional routing flag.
/// </summary>
public enum LobbyMessageType : byte
{
    None = 0,
    JoinRequest = 1,
    JoinAccepted = 2,
    PeerListRequest = 3,
    PeerListResponse = 4,
    Leave_Quit = 5,
    Leave_Error = 6,

    ERROR_AlreadyPeer = 200,
    ERROR_JoinRejectedNotInLobby = 201,
}

/// <summary>
/// Most lobby messages don't have any actual payloads, so we just shove a single byte into the payload array indicating
/// if they are host or not to help the receiver process our message quickly.
/// </summary>
public enum LobbyHostFlag : byte
{
    FromNonHost = 0,
    FromHost = 1,
}

/// <summary>
/// The lobby provides a promise that you and every single player in your game are all connected to eachother, and can send messages either to the lobby host or to any other peer. 
/// It handles the connection graph needed to facilitate that, and pumps out events when notable events take place. Any other code can hook into these events for various purposes.
/// Lobby is the second layer of the multiplayer abstraction system, after SteamNetwork. SteamNetwork handles actual connections and data with no understanding of friends or lobbies or hosts.
/// Lobby should not be handling gameplay related tasks - its job starts and ends at keeping LobbyPeers perfectly maintained.
/// </summary>
public class Lobby
{
    /// <summary>
    /// The most useful output of the Lobby system is the LobbyPeers list. It is a list of peers that we are connected to, and thus represents the list of users we are able to send messages to, and the list of users in our game.
    /// One of the peers in LobbyPeers is the Lobby Host, who has a few extra jobs in keeping everyone in line. The lobby host does not have any inherent networking authority (they are not the server), its just an arbitrary designation
    /// given to the first person to start the lobby.
    /// </summary>
    public HashSet<ulong> lobbyPeers = new();

    /// <summary>
    /// One of the peers in LobbyPeers is the Lobby Host, who has a few extra jobs in keeping everyone in line. The lobby host does not have any inherent networking authority, under
    /// the hood they are just another peer. We assign special authority to this host just to help keep things organized and sane.
    /// </summary>
    public ulong LobbyHostSteamID;

    /// <summary>
    /// True if we're in a lobby. This should be true most of the time, as the Lobby system is running in the background at all times, even in singleplayer.
    /// </summary>
    public bool bInLobby = false;

    /// <summary>
    /// True if we're the lobby host. See <see cref="LobbyHostSteamID"/>
    /// </summary>
    public bool bIsLobbyHost = false;

    /// <summary>
    /// An automagical Steam callback that fires whenever the local user does any of the following:
    /// 1. Clicks "accept" on a steam invite, 2. clicks "join game" in the friends menu. 3. Probably other stuff idk
    /// <para>Regardless, the parameter contains the steamID of the friend in question, and the value stored in that friend's rich presence dict under the "connect" key. </para>
    /// </summary>
    Callback<GameRichPresenceJoinRequested_t> m_GameRichPresenceJoinRequested;

    public delegate void HostedNewLobby();
    /// <summary>
    /// Fires when we host a new lobby. We try to keep a lobby open at all times to allow friends to join us, so this fires semi-frequently as the user leaves/switches lobbies.
    /// This event is lowkey not that helpful I think. Might be good to trigger some preloads or smth.
    /// </summary>
    public static event HostedNewLobby HostedNewLobbyEvent;

    public delegate void JoinedToLobby(ulong hostSteamID);
    /// <summary>
    /// Fires when we join a lobby, including our own after hosting. Once this fires we should be safe to query lobby state.
    /// </summary>
    public static event JoinedToLobby JoinedToLobbyEvent;

    public delegate void NewLobbyPeerAdded(ulong newPlayerSteamID);
    /// <summary>
    /// Fires when a someone new joins the lobby that we're in, including ourselves.
    /// </summary>
    public static event NewLobbyPeerAdded NewLobbyPeerAddedEvent;

    public delegate void LobbyPeerRemoved(ulong removedPlayerSteamID);
    /// <summary>
    /// Fires when a someone leaves the lobby that we're in. DOES NOT FIRE WHEN WE LEAVE A LOBBY. See <see cref="LeftLobbyEvent"/>.
    /// </summary>
    public static event LobbyPeerRemoved LobbyPeerRemovedEvent;

    public delegate void LeftLobby();
    /// <summary>
    /// Fires when we leave a lobby.
    /// </summary>
    public static event LeftLobby LeftLobbyEvent;

    /// <summary>
    /// Register the rich presence Steam Callback when we first start the Lobby system. After this is done, we can join to other players. This does NOT make us joinable, however.
    /// </summary>
    public Lobby()
    {
        m_GameRichPresenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnGameRichPresenceJoinRequested);
    }

    /// <summary>
    /// Start a new lobby with us as the host. After this we will be joinable, and be able to send invites.
    /// </summary>
    /// <param name="leaveCurrent">If true, leave the current lobby before making a new one. If false, throws an error if trying to host while in a lobby.</param>
    public void HostNewLobby(bool leaveCurrent = false)
    {
        if (bInLobby)
        {
            if (leaveCurrent)
            {
                Logging.Log($"Leaving current lobby before hosting a new one...", "Lobby");
                LeaveLobby();
            }
            else
            {
                Logging.Error($"Cannot host a new lobby while still in a lobby. Leave first!", "Lobby");
            }
        }
        Logging.Log($"Hosting a new lobby and enabling steam rich presence.", "Lobby");
        SteamFriends.SetRichPresence("connect", Global.steamid.ToString());
        bInLobby = true;
        bIsLobbyHost = true;
        LobbyHostSteamID = Global.steamid;
        lobbyPeers = new();
        lobbyPeers.Add(Global.steamid);
        HostedNewLobbyEvent?.Invoke();
        JoinedToLobbyEvent?.Invoke(Global.steamid);
        NewLobbyPeerAddedEvent?.Invoke(Global.steamid);
    }

    /// <summary>
    /// See <see cref="m_GameRichPresenceJoinRequested"/>
    /// </summary>
    /// <param name="param"></param>
    private void OnGameRichPresenceJoinRequested(GameRichPresenceJoinRequested_t param)
    {
        Logging.Log($"RichPresence join to {ulong.Parse(param.m_rgchConnect)} requested, sending them a join request...", "Lobby");
        AttemptJoinToLobby(ulong.Parse(param.m_rgchConnect), true);
    }

    /// <summary>
    /// Sends a join request to the target user. Nothing actually happens till we get a response.
    /// </summary>
    /// <param name="steamID"></param>
    /// <param name="leaveCurrent"></param>
    public void AttemptJoinToLobby(ulong steamID, bool leaveCurrent = false)
    {
        //TODO: Potential race condition if we send out multiple join requests to multiple users before getting the first response.
        //Only an issue under extreme performance/network conditions
        if (bInLobby)
        {
            if (leaveCurrent)
            {
                Logging.Log($"Leaving current lobby before joining a new one...", "Lobby");
                LeaveLobby();
            }
            else
            {
                Logging.Error($"Cannot join a new lobby while still in a lobby. Leave first!", "Lobby");
            }
        }
        Logging.Log($"Attempting to join lobby hosted by: {steamID}", "Lobby");
        SendLobbyMessage([(byte)LobbyHostFlag.FromNonHost], LobbyMessageType.JoinRequest, steamID);
    }

    /// <summary>
    /// Packs the data byte array into the correct format for a Lobby Message, which takes a one byte type flag and sets it as the first byte of the message payload.
    /// Then sends the message to the designated user.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    /// <param name="toSteamID"></param>
    /// <returns></returns>
    public EResult SendLobbyMessage(byte[] data, LobbyMessageType type, ulong toSteamID)
    {
        Logging.Log($"Sending Lobby Message with type {type.ToString()} to {toSteamID} | payload length:{data.Length}", "LobbyWire");
        byte[] newData = new byte[data.Length + 1];
        newData[0] = (byte)type;
        data.CopyTo(newData, 1);
        SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
        identity.SetSteamID64(toSteamID);
        return Global.network.SendData(newData, NetType.LOBBY_BYTES, identity);
    }

    /// <summary>
    /// Helper function that sends a lobby message to all peers.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public List<EResult> BroadcastLobbyMessage(byte[] data, LobbyMessageType type)

    {
        Logging.Log($"Broadcasting Lobby Message to all {lobbyPeers.Count} peers in our Lobby...", "LobbyWire");
        List<EResult> retval = new List<EResult>();
        foreach (ulong steamID in lobbyPeers)
        {
            retval.Add(SendLobbyMessage(data, type, steamID));
        }
        return retval;
    }

    /// <summary>
    /// Leaves the lobby we're currently in and resets all relevant lobby fields. After this we can still join other people but are not joinable.
    /// </summary>
    /// <param name="restart">If true, host a new lobby right away (like we do when the game starts) </param>
    public void LeaveLobby(bool restart = false, bool announce = true)
    {
        if (!bInLobby)
        {
            Logging.Warn($"Can't leave lobby, as we are not in a lobby", "Lobby");
            return;
        }
        else
        {
            Logging.Log($"Leaving lobby and resetting lobby status.", "Lobby");
            if (announce)
            {
                byte flag = bIsLobbyHost ? (byte)LobbyHostFlag.FromHost : (byte)LobbyHostFlag.FromNonHost;
                foreach (ulong peer in lobbyPeers)
                {
                    if (NetworkUtils.IsMe(peer)) continue;
                    SendLobbyMessage([flag], LobbyMessageType.Leave_Quit, peer);
                }
            }

            SteamFriends.ClearRichPresence();
            bInLobby = false;
            bIsLobbyHost = false;
            LobbyHostSteamID = 0;
            lobbyPeers = new();
            LeftLobbyEvent?.Invoke();
        }
        if (restart)
        {
            HostNewLobby();
        }
    }

    /// <summary>
    /// Bail out of the lobby and tell everyone I've suffered a fatal error. 
    /// </summary>
    public void ErrorOutOfLobby()
    {
        Logging.Log($"Attempting to tell my peers I'm leaving because of a critical error", "Lobby");
        byte flag = bIsLobbyHost ? (byte)LobbyHostFlag.FromHost : (byte)LobbyHostFlag.FromNonHost;
        foreach (ulong peer in lobbyPeers)
        {
            SendLobbyMessage([flag], LobbyMessageType.Leave_Error, peer);
        }
        LeaveLobby(true);
    }

    /// <summary>
    /// Core processor for incoming network messages with LOBBY type. NOTE: messages sent to ourself are loopbacked and received here
    /// </summary>
    /// <param name="payload"></param>
    /// <param name="fromSteamID"></param>
    /// <exception cref="ArgumentException"></exception>
    public void HandleLobbyBytes(byte[] payload, ulong fromSteamID)
    {
        Logging.Log($"Lobby Message from {fromSteamID} has type {((LobbyMessageType)payload[0]).ToString()}", "LobbyWire");

        //We know by virtue of the NetType.LOBBY that the sender (is supposed to) set the first byte of the message payload to a LobbyMessageType value
        //Here we split off that one byte, and use it to process the rest of the message correctly.
        LobbyMessageType type = (LobbyMessageType)payload[0];
        byte[] data = payload.Skip(1).ToArray();

        //TODO: Better documentation/organization of this messy switch statement
        switch (type)
        {
            case LobbyMessageType.JoinRequest:
                if (!bInLobby)
                {
                    Logging.Warn("Ignoring unexpected Join Request! We are not in a lobby!", "Lobby");
                    SendLobbyMessage([(byte)LobbyHostFlag.FromNonHost], LobbyMessageType.ERROR_JoinRejectedNotInLobby, fromSteamID);
                    break;
                }
                if (bIsLobbyHost)
                {
                    Logging.Log($"Accepting Join Request from {fromSteamID} as Host", "Lobby");
                    if (lobbyPeers.Add(fromSteamID))
                    {
                        NewLobbyPeerAddedEvent?.Invoke(fromSteamID);
                        SendLobbyMessage([(byte)LobbyHostFlag.FromHost], LobbyMessageType.JoinAccepted, fromSteamID);
                    }
                    else
                    {
                        SendLobbyMessage([(byte)LobbyHostFlag.FromHost], LobbyMessageType.ERROR_AlreadyPeer, fromSteamID);
                    }
                }
                else
                {
                    Logging.Log($"Accepting Join Request from {fromSteamID} as non-host", "Lobby");
                    if (lobbyPeers.Add(fromSteamID))
                    {
                        NewLobbyPeerAddedEvent?.Invoke(fromSteamID);
                        SendLobbyMessage([(byte)LobbyHostFlag.FromNonHost], LobbyMessageType.JoinAccepted, fromSteamID);
                    }
                    else
                    {
                        SendLobbyMessage([(byte)LobbyHostFlag.FromNonHost], LobbyMessageType.ERROR_AlreadyPeer, fromSteamID);
                    }
                }
                break;
            case LobbyMessageType.JoinAccepted:
                if (data[0] == (byte)LobbyHostFlag.FromHost) // we just joined the host look alive
                {
                    if (bInLobby)
                    {
                        Logging.Warn($"We're already in a lobby but just joined another!", "Lobby");
                        Logging.Warn($"Leaving current lobby before joining the new one...", "Lobby");
                        LeaveLobby(false);
                    }
                    if (lobbyPeers.Add(fromSteamID))
                    {
                        Logging.Log($"Successfully joined to host {fromSteamID}. Sending request for other peers.", "Lobby");
                        lobbyPeers.Add(fromSteamID);
                        LobbyHostSteamID = fromSteamID;
                        bInLobby = true;
                        SteamFriends.SetRichPresence("connect", fromSteamID.ToString());
                        lobbyPeers.Add(Global.steamid);
                        JoinedToLobbyEvent?.Invoke(fromSteamID);
                        NewLobbyPeerAddedEvent?.Invoke(Global.steamid);
                        SendLobbyMessage([(byte)LobbyHostFlag.FromNonHost], LobbyMessageType.PeerListRequest, fromSteamID);
                        break;
                    }
                    else
                    {
                        Logging.Warn($"Our join request to host {fromSteamID} was accepted but we already have them as a peer. What happened?", "Lobby");
                        break;
                    }
                }
                else if (data[0] == (byte)LobbyHostFlag.FromNonHost) // established a peer connection to a non host
                {
                    if (lobbyPeers.Add(fromSteamID))
                    {
                        Logging.Log($"Successfully joined to non-host {fromSteamID}. Sending request for other peers.", "Lobby");
                        NewLobbyPeerAddedEvent?.Invoke(fromSteamID);
                        SendLobbyMessage([(byte)LobbyHostFlag.FromNonHost], LobbyMessageType.PeerListRequest, fromSteamID);
                        break;
                    }
                    else
                    {
                        Logging.Warn($"Our join request to non-host {fromSteamID} was accepted but we already have them as a peer. What happened?", "Lobby");
                        break;
                    }
                }
                else
                {
                    throw new ArgumentException($"Malformed JoinAccepted Message | First Byte: {((int)data[0])}");
                }
            case LobbyMessageType.PeerListRequest:
                Logging.Log($"Request for peers from {fromSteamID}. Sending peer data.", "Lobby");
                foreach (ulong peerID in lobbyPeers)
                {
                    if (peerID == fromSteamID) continue;
                    SendLobbyMessage(BitConverter.GetBytes(peerID), LobbyMessageType.PeerListResponse, fromSteamID);
                }
                break;
            case LobbyMessageType.PeerListResponse:
                ulong newPeerID = BitConverter.ToUInt64(data, 0);
                if (lobbyPeers.Add(newPeerID))
                {
                    Logging.Log($"Received data on new shared peer {newPeerID}, requesting to join it", "Lobby");
                    SendLobbyMessage([(byte)LobbyHostFlag.FromNonHost], LobbyMessageType.JoinRequest, newPeerID);
                }
                else
                {
                    Logging.Log($"Received data on duplicate peer {newPeerID}, ignoring.", "Lobby");
                }
                break;
            case LobbyMessageType.Leave_Quit:

                if (data[0] == (byte)LobbyHostFlag.FromNonHost)
                {
                    Logging.Log("Peer left lobby, removing them from list.", "Lobby");
                    LobbyPeerRemovedEvent?.Invoke(fromSteamID);
                    lobbyPeers.Remove(fromSteamID);
                }
                else if (data[0] == (byte)LobbyHostFlag.FromHost)
                {
                    Logging.Warn("The Host just quit out of our lobby, attempting to migrate.", "Lobby");
                    //The Lobby host just quit the game
                    //TODO: Implement Lobby Host Migration. In theory this could just pick a random person, but all of the peers need to come to the same conclusion somehow.
                    Logging.Warn("Host migration not implemented. Closing lobby!", "Lobby");
                    LeaveLobby(true, false);
                }
                break;
            case LobbyMessageType.Leave_Error:
                if (data[0] == (byte)LobbyHostFlag.FromNonHost)
                {
                    Logging.Log("Peer errored out of lobby, removing them from list.", "Lobby");
                    LobbyPeerRemovedEvent?.Invoke(fromSteamID);
                    lobbyPeers.Remove(fromSteamID);
                }
                else if (data[0] == (byte)LobbyHostFlag.FromHost)
                {
                    Logging.Warn("The Host just errored out of our lobby, attempting to migrate.", "Lobby");
                    //The Lobby host just quit the game
                    //TODO: Implement Lobby Host Migration. In theory this could just pick a random person, but all of the peers need to come to the same conclusion somehow.
                    Logging.Warn("Host migration not implemented. Closing lobby!", "Lobby");
                    LeaveLobby();
                }
                break;
            case LobbyMessageType.ERROR_AlreadyPeer:
                Logging.Warn($"We tried to joined a peer who thinks we are already joined to. Locally adding them as peer and Requesting updated peer list.", "Lobby");
                lobbyPeers.Add(fromSteamID);
                SendLobbyMessage([(byte)LobbyHostFlag.FromNonHost], LobbyMessageType.PeerListRequest, fromSteamID);
                break;
            case LobbyMessageType.ERROR_JoinRejectedNotInLobby:
                Logging.Warn($"Join Request rejected because the user we're trying to join is not in a lobby.", "Lobby");
                break;
            default:
                throw new ArgumentException($"Malformed Lobby Message | First Byte: {((int)payload[0])} Cast As LobbyMessageType:{((LobbyMessageType)payload[0]).ToString()}");
                break;
        }
    }
}

