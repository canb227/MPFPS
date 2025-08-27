using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// one byte (0-255) value for network message type
/// </summary>
public enum NetType : byte
{
    ERROR = 0,

    LOBBY_BYTES = 1,
    SESSION_BYTES = 2,

    //Other Types
    DEBUG_UTF8 = 254,
    EMPTY = 255,
}

/// <summary>
/// Handles low level networking functions - implemented using SteamMessages
/// </summary>
public class SteamNetwork
{
    /// <summary>
    /// Fires when we get packets from a user we haven't seen yet. The incoming packets are in stasis until we accept this request.
    /// </summary>
    Callback<SteamNetworkingMessagesSessionRequest_t> SessionRequest;

    /// <summary>
    /// Fires when the low-level steam network session fails. Bad news.
    /// </summary>
    Callback<SteamNetworkingMessagesSessionFailed_t> SessionFailed;

    /// <summary>
    /// Fires when our connection to the Steam Relay Network changes
    /// </summary>
    Callback<SteamRelayNetworkStatus_t> RelayNetworkStatusChanged;

    /// <summary>
    /// deprecated? I think this doesnt fire at all when using the auto-session management baked into SteamMessages.
    /// </summary>
    Callback<SteamNetConnectionStatusChangedCallback_t> ConnectionStatusChanged;

    /// <summary>
    /// Max number of messages to attempt to process per frame. If we get frame delays because of spiky network traffic this needs turned down
    /// </summary>
    public int maxMessagePerFrame = 100;

    /// <summary>
    /// if true, messages we send to ourself  get processed as if they had been sent over the network. If false, messages sent to ourself are discarded.
    /// <para>IMPORTANT: Look. This has to be true or else everything breaks because we take advantage of loopback to unify our multiplayer and singleplayer code down the line.</para>
    /// <para>IMPORTANT: Does not fully replicate network message process. Skips payload memory allocation and payload byte packing.</para>
    /// </summary>
    public const bool bDoLoopback = true;

    /// <summary>
    /// If true, introduce artifical network conditions to loopback messages. TESTING ONLY
    /// </summary>
    private bool bNetworkSimulation = false;

    /// <summary>
    /// Base miliseconds to delay all loopback messages
    /// </summary>
    private int iNetworkSimulationDelayMS = 50;

    /// <summary>
    /// Randomly add between 0 and this value number of miliseconds to the base delay
    /// </summary>
    private int iNetworkSimulationDelayVarianceMS = 100;

    public SteamNetwork()
    {
        SessionFailed = Callback<SteamNetworkingMessagesSessionFailed_t>.Create(OnSessionFailed);
        SessionRequest = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(OnSessionRequest);
        RelayNetworkStatusChanged = Callback<SteamRelayNetworkStatus_t>.Create(OnRelayNetworkStatusChanged);

        //Uncomment this if steam relay network is being stupid
        //SteamNetworkHealthManager();
    }


    /// <summary>
    /// Attempts to maintain connection with Steam Relay Network - runs forever once called!
    /// </summary>
    public async void SteamNetworkHealthManager()
    {
        while (true)
        {
            await Task.Delay(1000);

            SteamNetworkingUtils.GetRelayNetworkStatus(out SteamRelayNetworkStatus_t details);
            if (details.m_eAvail != ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Current)
            {
                Logging.Warn("Steam Relay Networking not connected, force retrying access...", "NetworkRelay");
                SteamNetworkingUtils.InitRelayNetworkAccess();
            }
        }
    }

    /// <summary>
    /// Fires when our connection to the Steam Relay Network changes. Ideally this starts at NeverTried, goes to Attempting, then settles into Current after a few seconds
    /// </summary>
    /// <param name="param"></param>
    private void OnRelayNetworkStatusChanged(SteamRelayNetworkStatus_t param)
    {
        Logging.Log($"Connection Status to Steam Relay Network has changed: {param.m_eAvail}", "NetworkRelay");
    }

    /// <summary>
    /// Fires when a low-level Steam Networking session fails with an individual user
    /// </summary>
    /// <param name="param"></param>
    private void OnSessionFailed(SteamNetworkingMessagesSessionFailed_t param)
    {
        param.m_info.m_identityRemote.ToString(out string idstring);
        Logging.Warn($"Session Failed with: {idstring} Reason: {((ESteamNetConnectionEnd)(param.m_info.m_eEndReason)).ToString()} DEBUG:{param.m_info.m_szEndDebug}", "NetworkSession");
    }

    /// <summary>
    /// Fires when we get a session request. Important! Session requests are not explictly sent. A session request is automatically generated when we get any steam message from someone we havent talked to yet during this game runtime.
    /// The original message that triggered this request remains in limbo for about three seconds before timing out (so accept within three seconds). 
    /// Accepting the session causes this message to continue thru without issue. Rejecting or ignoring the session prevents the message from ever reaching processing.
    /// Sending a message to a user causes their session requests to be auto-accepted (including any pending) for the remainder of the game runtime.
    /// </summary>
    /// <param name="param"></param>
    void OnSessionRequest(SteamNetworkingMessagesSessionRequest_t param)
    {
        bool sessionEstablished = SteamNetworkingMessages.AcceptSessionWithUser(ref param.m_identityRemote);
        if (sessionEstablished)
        {
            param.m_identityRemote.ToString(out string idstring);
            Logging.Log($"New Session Established with {idstring}", "NetworkSession");
        }
        else
        {
            param.m_identityRemote.ToString(out string idstring);
            Logging.Warn($"Failed to Establish New Session With {idstring}", "NetworkSession");
        }
    }

    /// <summary>
    /// Sends an array of bytes plus a one-byte type enum to the specified user. See <see cref="OnSessionRequest(SteamNetworkingMessagesSessionRequest_t)"/> for session establishment rules.
    /// </summary>
    /// <param name="data">byte array to send</param>
    /// <param name="type">type of data you are sending</param>
    /// <param name="remoteIdentity"> User to send data to. I recommend the <see cref="NetworkUtils.SteamIDToIdentity(ulong)"/> function for this.</param>
    /// <returns>An EResult enum value indicating the result of the send. A value of k_EResultOK indicates that the message was constructed and placed into the Steam Relay Network. A value of k_EResultOK DOES NOT mean that the message was actually delivered.</returns>
    public EResult SendData(byte[] data, NetType type, SteamNetworkingIdentity remoteIdentity)
    {
        if (bDoLoopback && NetworkUtils.IsMe(remoteIdentity))
        {
            Loopback(data, type);
            return EResult.k_EResultOK;
        }
        byte[] payload = NetworkUtils.WrapSteamPayload(data, type);

        nint ptr = NetworkUtils.BytesToPtr(payload);
        remoteIdentity.ToString(out string idstring);
        EResult result = SteamNetworkingMessages.SendMessageToUser(ref remoteIdentity, ptr, (uint)payload.Length, NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle, 0);
        Logging.Log($" MSGSND | TO: {SteamFriends.GetFriendPersonaName(remoteIdentity.GetSteamID())}({idstring}) | TYPE: {type.ToString()} | SIZE: {data.Length} | RESULT: {result.ToString()}", "NetworkWire");
        return result;
    }

    /// <summary>
    /// Helper that sends the byte array to a list of SteamNetworkingIdentities. See <see cref="SendData(byte[], NetType, SteamNetworkingIdentity)"/>
    /// </summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    /// <param name="remoteIdentities"></param>
    /// <returns></returns>
    public List<EResult> BroadcastData(byte[] data, NetType type, List<SteamNetworkingIdentity> remoteIdentities)
    {
        List<EResult> retval = new List<EResult>();
        foreach (SteamNetworkingIdentity identity in remoteIdentities)
        {
            retval.Add(SendData(data, type, identity));
        }
        return retval;
    }

    /// <summary>
    /// Helper that sends the byte array to a list of steamIDs. See <see cref="SendData(byte[], NetType, SteamNetworkingIdentity)"/>
    /// </summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    /// <param name="remoteIdentities"></param>
    /// <returns></returns>
    public List<EResult> BroadcastData(byte[] data, NetType type, List<ulong> remoteSteamIDs)
    {
        List<EResult> retval = new List<EResult>();
        foreach (ulong identity in remoteSteamIDs)
        {
            retval.Add(SendData(data, type, NetworkUtils.SteamIDToIdentity(identity)));
        }
        return retval;
    }

    /// <summary>
    /// Helper function for sending structs. See <see cref="SendData(byte[], NetType, SteamNetworkingIdentity)"/>
    /// </summary>
    /// <param name="message"></param>
    /// <param name="type"></param>
    /// <param name="remoteIdentity"></param>
    /// <returns></returns>
    public EResult SendStruct<T>(T structure, NetType type, SteamNetworkingIdentity remoteIdentity)
    {
        return SendData(NetworkUtils.StructToBytes<T>(structure), type, remoteIdentity);
    }


    /// <summary>
    /// Helper that sends the struct to a list of SteamNetworkingIdentities.. See <see cref="SendStruct{T}(T, NetType, SteamNetworkingIdentity)"/>
    /// </summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    /// <param name="remoteIdentities"></param>
    /// <returns></returns>
    public List<EResult> BroadcastStruct<T>(T structure, NetType type, List<SteamNetworkingIdentity> remoteIdentities)
    {
        List<EResult> retval = new List<EResult>();
        foreach (SteamNetworkingIdentity identity in remoteIdentities)
        {
            retval.Add(SendStruct(structure, type, identity));
        }
        return retval;
    }

    /// <summary>
    /// Helper that sends the struct to a list of steamIDs. See <see cref="SendStruct{T}(T, NetType, SteamNetworkingIdentity)"/>
    /// </summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    /// <param name="remoteIdentities"></param>
    /// <returns></returns>
    public List<EResult> BroadcastStruct<T>(T structure, NetType type, List<ulong> remoteSteamIDs)
    {
        List<EResult> retval = new List<EResult>();
        foreach (ulong identity in remoteSteamIDs)
        {
            retval.Add(SendStruct(structure, type, NetworkUtils.SteamIDToIdentity(identity)));
        }
        return retval;
    }

    /// <summary>
    /// Message queue pump that pulls up to a set number of messages off the queue per frame. Deconstructs the byte array payload into the one-byte type (derived from the first byte of payload) and the byte array data (the rest of the payload).
    /// </summary>
    /// <param name="delta"></param>
    public void PerFrame(double delta)
    {
        nint[] messages = new nint[maxMessagePerFrame];
        for (int i = 0; i < SteamNetworkingMessages.ReceiveMessagesOnChannel(0, messages, maxMessagePerFrame); i++)
        {
            SteamNetworkingMessage_t steamMessage = SteamNetworkingMessage_t.FromIntPtr(messages[i]);
            byte[] payload = NetworkUtils.PtrToBytes(steamMessage.m_pData, steamMessage.m_cbSize);
            NetworkUtils.UnwrapSteamPayload(payload, out byte[] data, out NetType type);
            steamMessage.m_identityPeer.ToString(out string idstring);
            Logging.Log($" MSGRCV | FROM: {idstring} | TYPE: {type.ToString()} | SIZE: {data.Length}", "NetworkWire");
            ProcessData(data, type, steamMessage.m_identityPeer.GetSteamID64());
            SteamNetworkingMessage_t.Release(messages[i]);
        }
    }

    /// <summary>
    /// Sends the byte array data off to the correct handler as indicated by the type
    /// </summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    /// <param name="fromSteamID"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void ProcessData(byte[] data, NetType type, ulong fromSteamID)
    {
        switch (type)
        {

            ///////////////////////////////////  LOBBY   /////////////////////
            case NetType.LOBBY_BYTES:
                Global.Lobby.HandleLobbyBytes(data, fromSteamID);
                break;


            ///////////////////////////////////  SESSION  /////////////////////
            case NetType.SESSION_BYTES:
                Global.GameSession?.HandleSessionMessageBytes(data, fromSteamID);
                break;

            ///////////////////////////////////  OTHER  /////////////////////
            case NetType.DEBUG_UTF8:
                Logging.Log($"Reencoded (UTF8) Message: {Encoding.UTF8.GetString(data)}", "NetworkDEBUG");
                break;
            default:
                throw new NotImplementedException($" BYTES TYPE ERROR | FROM: {fromSteamID} | TYPE: {type} | SIZE: {data.Length}");
        }
    }

    /// <summary>
    /// Takes the same parameters as a SendData() but just routes the message back to ourselves with optional network simulation.
    /// See <see cref="bDoLoopback"/>
    /// <para>IMPORTANT: Does not fully replicate network message process. Skips payload memory allocation and payload byte packing.</para>
    /// </summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    private async void Loopback(byte[] data, NetType type)
    {
        if (bNetworkSimulation)
        {
            await Task.Delay(iNetworkSimulationDelayMS + (int)(new Random().NextInt64(iNetworkSimulationDelayVarianceMS)));
        }
        Logging.Log($" MSGRCV | FROM: LOOPBACK | TYPE: {type.ToString()} | SIZE: {data.Length}", "NetworkWire");
        ProcessData(data, type, Global.steamid);
    }

    public void Tick(double delta)
    {

    }
}

