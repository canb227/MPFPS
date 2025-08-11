using Google.Protobuf;
using NetworkMessages;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

public class SteamMessages : SteamNetworkInterface
{

    public List<SteamNetworkingIdentity> PeerList = new List<SteamNetworkingIdentity>();

    public ESteamNetworkingAvailability SteamRelayNetworkingStatus = ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_NeverTried;

    /// <summary>
    /// Fires when any Steam user sends a SteamNetworkingMessage to our SteamID - unless we already have a session open with them
    /// </summary>
    protected Callback<SteamNetworkingMessagesSessionRequest_t> SessionRequest;

    /// <summary>
    /// Fires when a User tried to send a (first) message to us but it failed for some Steam internal reason
    /// </summary>
    protected Callback<SteamNetworkingMessagesSessionFailed_t> SessionFailed;

    /// <summary>
    /// Fires when our connection the Steam Relay Network changes, this typically signals connecting or disconnecting, or if something has gone wrong (ie internet drops)
    /// </summary>
    Callback<SteamRelayNetworkStatus_t> SteamRelayNetworkStatusChangedCallback;

    /// <summary>
    /// True if we are allowing Steam Messages to be recevied
    /// </summary>
    public bool IsOnline { get; private set; } = false;

    /// <summary>
    /// If this is true, block all messages that are not from friends or members of the below list.
    /// Turn this on if we're getting spammed by some malicious actor, otherwise we're fine.
    /// </summary>
    public bool SecureMode = false;

    private bool loopback = false;


    /// <summary>
    /// If SecureMode is on, users must be Steam Friends or on this list to be allowed to message us.
    /// </summary>
    public List<ulong> TrustedUsers { get; private set; } = new();

    public enum SteamNetByteFlag
    {
        SessionRequest = 0,
        SessionAccepted = 1,
        AddPeer = 2,
    }

    public SteamMessages()
    {
    
    }


    public bool GoOnline()
    {
        SteamNetworkingUtils.InitRelayNetworkAccess();
        //Registers the session request and failed events with the function to call when those events happen.
        //These are coming from the Steamworks API - look them up there.
        SessionRequest = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(OnSessionRequest);
        SessionFailed = Callback<SteamNetworkingMessagesSessionFailed_t>.Create(OnSessionFailed);
        SteamRelayNetworkStatusChangedCallback = Callback<SteamRelayNetworkStatus_t>.Create(OnRelayNetworkStatusChanged);
        Logging.Log("SteamNet Interface started, ready to handle messages.", "SteamNet");
        IsOnline = true;
        return true;
    }

    private void OnRelayNetworkStatusChanged(SteamRelayNetworkStatus_t param)
    {
        if (param.m_eAvail == SteamRelayNetworkingStatus) return;
        Logging.Log($"Status of connection to Steam Relay Network has changed to: {param.m_eAvail}", "SteamNet");
        SteamRelayNetworkingStatus = param.m_eAvail;
    }

    public bool GoOffline()
    {
        IsOnline = false;

        SessionRequest = null;
        SessionFailed = null;
        SteamRelayNetworkStatusChangedCallback = null;
        return true;
    }

    public void EnableLoopback()
    {
        loopback = true;
        SteamNetworkingIdentity self = NetworkUtils.SteamIDToIdentity(Global.steamid);
        if (!PeerList.Contains(self))
        {
            PeerList.Add(self);
        }
    }

    public void DisableLoopback()
    {
        loopback = false;
        SteamNetworkingIdentity self = NetworkUtils.SteamIDToIdentity(Global.steamid);
        if (PeerList.Contains(self))
        {
            PeerList.Remove(self);
        }
    }

    private void OnSessionFailed(SteamNetworkingMessagesSessionFailed_t param)
    {
        if (!IsOnline) return;
        Logging.Warn($"Session with {param.m_info.m_identityRemote} has failed. Reason: {param.m_info.m_eEndReason}", "SteamNet");
    }

    /// <summary>
    /// This function is called when the SessionRequest callback is triggered by Steam. See SessionRequest.
    /// We can pretty liberally accept sessions with users as recevied messages are still validated and processed by upstream code.
    /// SecureMode can be enabled to prevent malicious DDOS
    /// </summary>
    /// <param name="request"></param>
    private void OnSessionRequest(SteamNetworkingMessagesSessionRequest_t request)
    {
        if (!IsOnline) return;

        SteamNetworkingIdentity identity = request.m_identityRemote;
        ulong steamID = identity.GetSteamID64();
        Logging.Log($"Connection request from {steamID}", "SteamNet");
        if (NetworkUtils.IsMe(steamID))
        {
            Logging.Warn($"Cannot connect to self - rejecting", "SteamNet");
        }
        if (SecureMode)
        {
            if (NetworkUtils.IsFriend(steamID) || IsTrustedUser(steamID))
            {

                if (SteamNetworkingMessages.AcceptSessionWithUser(ref identity))
                {
                    PeerList.Add(identity);
                    SendBytesToUser([1], identity, NetworkManager.NetworkChannel.SteamNet, NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle);
                    Logging.Log("SecureMode is on - User is friend or trusted, establishing session", "SteamNet");
                }
                else
                {
                    Logging.Error($"Unknown error accepting session with user {steamID}", "SteamNet");
                }
            }
            else
            {
                Logging.Warn("SecureMode is on - User is untrusted, rejecting", "SteamNet");
            }
        }
        else
        {

            if (SteamNetworkingMessages.AcceptSessionWithUser(ref identity))
            {
                SendBytesToUser([1], identity, NetworkManager.NetworkChannel.SteamNet, NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle);
                PeerList.Add(identity);
                Logging.Log("SecureMode is off, establishing session", "SteamNet");
            }
            else
            {
                Logging.Error($"Unknown error accepting session with user {steamID}", "SteamNet");
            }
        }
    }

    public bool AddTrustedUser(ulong steamID)
    {
        if (!SecureMode)
        {
            Logging.Error($"Cannot add Trusted User if SecureMode is off", "SteamNet");
            throw new Exception("Cannot add Trusted User if SecureMode is off");
        }
        TrustedUsers.Add(steamID);
        return true;
    }

    public bool RemoveTrustedUser(ulong steamID)
    {
        if (!SecureMode)
        {
            Logging.Error($"Cannot remove Trusted User if SecureMode is off", "SteamNet");
            throw new Exception("Cannot remove Trusted User if SecureMode is off");
        }
        return TrustedUsers.Remove(steamID);
    }

    public bool IsTrustedUser(ulong steamID)
    {
        if (!SecureMode)
        {
            Logging.Error($"Cannot query Trusted Users if SecureMode is off", "SteamNet");
            throw new Exception("Cannot query Trusted Users if SecureMode is off");
        }
        return TrustedUsers.Contains(steamID);
    }

    public void AttemptConnectionToUser(SteamNetworkingIdentity identity)
    {
        SendBytesToUser([0], identity, NetworkManager.NetworkChannel.SteamNet, NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle);
    }

    public void DisconnectFromAllUsers()
    {
        foreach (var peer in PeerList)
        {
            DisconnectFromUser(peer);
        }
    }

    public bool DisconnectFromUser(SteamNetworkingIdentity identity)
    {
        PeerList.Remove(identity);
        return SteamNetworkingMessages.CloseSessionWithUser(ref identity);
    }

    public ESteamNetworkingConnectionState GetConnectionInfo(SteamNetworkingIdentity identity, out SteamNetConnectionInfo_t connectionInfo, out SteamNetConnectionRealTimeStatus_t connectionStatus)
    {
        return SteamNetworkingMessages.GetSessionConnectionInfo(ref identity, out connectionInfo, out connectionStatus);
    }

    public bool IsUserConnectedPeer(SteamNetworkingIdentity identity)
    {
        return PeerList.Contains(identity);
    }

    public EResult SendBytesToUser(byte[] data, SteamNetworkingIdentity remotePeer, NetworkManager.NetworkChannel channel, int sendFlags)
    {

        //Todo: Every single message sent goes thru this function - please check if Marshal.AllocHGlobal() and Marshal.Copy() are the best way to do this.

        //Allocate a block of memory to hold the message. I'm not sure this is the best way to do this. nint is a pointer weaing an int hat to sneak thru c# safety checks
        nint ptr = Marshal.AllocHGlobal(data.Length);

        //Fill that memory with the bytes
        Marshal.Copy(data, 0, ptr, data.Length);

        //Steamworks.Net/ Steamworks API function to send the message. See the Steamworks API
        return SteamNetworkingMessages.SendMessageToUser(ref remotePeer, ptr, (uint)data.Length, sendFlags, (int)channel);
    }

    public List<(SteamNetworkingIdentity peer, EResult result)> SendMessageToAllPeers(IMessage message, NetworkManager.NetworkChannel channel, int sendFlags)
    {
        List<(SteamNetworkingIdentity peer, EResult result)> results = new();
        foreach (SteamNetworkingIdentity peer in PeerList)
        {
            results.Add( (peer: peer, result: SendMessageToUser(message, peer, channel, sendFlags)));
        }
        return results;
    }

    public EResult SendMessageToUser(IMessage message, SteamNetworkingIdentity identity, NetworkManager.NetworkChannel channel, int sendFlags)
    {
        Logging.Log($"Sending Message to {identity.GetSteamID64()} on channel {channel.ToString()}","SteamNetWire");
        if (loopback && NetworkUtils.IsMe(identity))
        {
            Global.network.Loopback(message, identity,channel,sendFlags);
            return EResult.k_EResultOK;
        }

        return SendBytesToUser(message.ToByteArray(), identity, channel, sendFlags);
    }



    public List<(SteamNetworkingIdentity peer, EResult result)> SendBytesToAllPeers(byte[] data, NetworkManager.NetworkChannel channel, int sendFlags)
    {
        List<(SteamNetworkingIdentity peer, EResult result)> results = new();
        foreach (SteamNetworkingIdentity peer in PeerList)
        {
            results.Add((peer: peer, result: SendBytesToUser(data, peer, channel, sendFlags)));
        }
        return results;
    }

    public bool GetNextPendingSteamMessageOnChannel(NetworkManager.NetworkChannel channel, out SteamNetworkingMessage_t msg)
    {
        nint[] messagePointer = new nint[1];
        int success = SteamNetworkingMessages.ReceiveMessagesOnChannel((int)channel, messagePointer, 1);
        if (success == 1)
        {
            msg = SteamNetworkingMessage_t.FromIntPtr(messagePointer[0]);
            return true;
        }
        else
        {
            msg = new();
            return false;
        }
    }

    public int GetNumPendingSteamMessagesOnChannel(NetworkManager.NetworkChannel channel, int maxNumMessages, out List<SteamNetworkingMessage_t> messages)
    {
        messages = new();
        nint[] messagePointers = new nint[maxNumMessages];
        int numMessages = SteamNetworkingMessages.ReceiveMessagesOnChannel((int)channel, messagePointers, maxNumMessages);
        if (numMessages == 0) return 0;
        for (int i = 0;i<numMessages;i++)
        {
            messages.Add(SteamNetworkingMessage_t.FromIntPtr(messagePointers[i]));
        }
        return numMessages;
    }

    public void HandleSteamMessage(SteamNetworkingMessage_t message)
    {

        byte[] data = NetworkUtils.UnwrapSteamMessage(message);
        SteamNetByteFlag flag = (SteamNetByteFlag)data[0];
        Logging.Log($"SteamNet Handling message - flag detected as:{flag.ToString()}","SteamNet");
        switch (flag)
        {
            case SteamNetByteFlag.SessionRequest:
                break;
            case SteamNetByteFlag.SessionAccepted:
                Logging.Warn($"Session with {message.m_identityPeer} has been established.", "SteamNet");
                PeerList.Add(message.m_identityPeer);
                break;
            default:
                throw new NotImplementedException("SteamNet Encountered an unexpected flag - exploding");
        }
    }



    public ESteamNetworkingAvailability GetSteamRelayNetworkStatus()
    {
        return SteamRelayNetworkingStatus;
    }

    public List<ulong> GetConnectedPeerList()
    {
        List<ulong> peerList = new List<ulong>();
        foreach(var peer in PeerList)
        {
            peerList.Add(peer.GetSteamID64());
        }
        return peerList;
    }
}

