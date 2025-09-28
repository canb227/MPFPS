
using MessagePack;
using MessagePack.Resolvers;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

public enum Channel
{
    ERROR = 0,
    LobbyMessageBytes = 1,
    GameObjectState = 2,
    PlayerInput = 3,
    GameStateOptions = 4,
    PlayerData = 5,
    NetCommands = 6,
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
    private int maxMessagePerFramePerChannel = 100;

    /// <summary>
    /// if true, messages we send to ourself  get processed as if they had been sent over the network. If false, messages sent to ourself are discarded.
    /// <para>IMPORTANT: Look. This has to be true or else everything breaks because we take advantage of loopback to unify our multiplayer and singleplayer code down the line.</para>
    /// <para>IMPORTANT: Does not fully replicate network message process. </para>
    /// </summary>
    private const bool bDoLoopback = true;

    /// <summary>
    /// If true, pack and unpack loopback messages as if they were being sent across the network. Negatively impacts performance but can help test for issues.
    /// </summary>
    private const bool bLoopbackMemoryAllocation = false;

    /// <summary>
    /// If true, introduce artifical network conditions to loopback messages. TESTING ONLY
    /// </summary>
    private const bool bNetworkSimulation = false;

    /// <summary>
    /// Base miliseconds to delay all loopback messages
    /// </summary>
    private const int iNetworkSimulationDelayMS = 50;

    /// <summary>
    /// Randomly add between 0 and this value number of miliseconds to the base delay
    /// </summary>
    private const int iNetworkSimulationDelayVarianceMS = 100;

    public bool BandwidthTrackerEnabled = true;
    public bool BandwidthTrackerCountLoopbackSend = true;
    public bool BandwidthTrackerCountLoopbackReceive = true;
    private double BandwidthTrackerWindow = 1;
    private double BandwidthTrackerTimer = 0;
    private int SendBandwidthTracker = 0;
    private int ReceiveBandwidthTracker = 0;
    private bool UltraDetailWireLogging = true;

    public delegate void SteamNetworkingMessageReceived(Channel channel, byte[] payload, ulong sender);
    public static event SteamNetworkingMessageReceived SteamNetworkingMessageReceivedEvent;

    public SteamNetwork()
    {
        SessionFailed = Callback<SteamNetworkingMessagesSessionFailed_t>.Create(OnSessionFailed);
        SessionRequest = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(OnSessionRequest);
        RelayNetworkStatusChanged = Callback<SteamRelayNetworkStatus_t>.Create(OnRelayNetworkStatusChanged);

        //Uncomment this if steam relay network is being stupid
        //SteamNetworkHealthManager();

        var resolver = MessagePack.Resolvers.CompositeResolver.Create(GodotResolver.Instance, MessagePack.Resolvers.StandardResolver.Instance);
        var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
        MessagePackSerializer.DefaultOptions = options;
    }



    /// <summary>
    /// Attempts to maintain connection with Steam Relay Network - runs forever once called!
    /// </summary>
    private async void SteamNetworkHealthManager()
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
    private void OnSessionRequest(SteamNetworkingMessagesSessionRequest_t param)
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

    public EResult SendStruct<T>(T structure, Channel channel, ulong remoteSteamID, int sendFlags = NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle)
    {
        byte[] data = NetworkUtils.StructToBytes(structure);
        return SendData(data, channel, remoteSteamID, sendFlags);
    }

    public List<EResult> BroadcastStruct<T>(T structure, Channel channel, List<ulong> remoteSteamIDs, int sendFlags = NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle)
    {
        List<EResult> retval = new List<EResult>();
        foreach (ulong identity in remoteSteamIDs)
        {
            retval.Add(SendStruct(structure, channel, identity, sendFlags));
        }
        return retval;
    }

    public EResult SendExpando(ExpandoObject obj, Channel channel, ulong remoteSteamID, int sendFlags = NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle)
    {
        byte[] data = MessagePackSerializer.Serialize(obj, ContractlessStandardResolver.Options);
        if (UltraDetailWireLogging)
        {
            Logging.Log(MessagePackSerializer.ConvertToJson(data, ContractlessStandardResolver.Options),"UltraDetailWireLogging");
        }
        return SendData(data, channel, remoteSteamID, sendFlags);
    }

    public List<EResult> BroadcastExpando(ExpandoObject obj, Channel channel, List<ulong> remoteSteamIDs, int sendFlags = NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle)
    {
        List<EResult> retval = new List<EResult>();
        foreach (ulong identity in remoteSteamIDs)
        {
            retval.Add(SendExpando(obj, channel, identity, sendFlags));
        }
        return retval;
    }


    /// <summary>
    /// Sends an array of bytes plus a one-byte type enum to the specified user. See <see cref="OnSessionRequest(SteamNetworkingMessagesSessionRequest_t)"/> for session establishment rules.
    /// </summary>
    /// <param name="data">byte array to send</param>
    /// <param name="type">type of data you are sending</param>
    /// <param name="remoteSteamID"> User to send data to.</param>
    /// <returns>An EResult enum value indicating the result of the send. A value of k_EResultOK indicates that the message was constructed and placed into the Steam Relay Network. A value of k_EResultOK DOES NOT mean that the message was actually delivered.</returns>
    public EResult SendData(byte[] data, Channel channel, ulong remoteSteamID, int sendFlags = NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle)
    {
        EResult result;
        if (bDoLoopback && NetworkUtils.IsMe(remoteSteamID))
        {
            Loopback(channel, data);
            result = EResult.k_EResultOK;
        }
        else
        {
            SendBandwidthTracker += data.Length;
            nint ptr = NetworkUtils.BytesToPtr(data);
            SteamNetworkingIdentity identity = NetworkUtils.SteamIDToIdentity(remoteSteamID);
            result = SteamNetworkingMessages.SendMessageToUser(ref identity, ptr, (uint)data.Length, sendFlags, (int)channel);
            Logging.Log($" MSGSND | TO: {SteamFriends.GetFriendPersonaName(identity.GetSteamID())}({identity.GetSteamID64()}) | SIZE: {data.Length} | RESULT: {result.ToString()}", "NetworkWire");
        }
        return result;
    }



    /// <summary>
    /// Helper that sends the byte array to a list of steamIDs. See <see cref="SendData(byte[], NetType, SteamNetworkingIdentity)"/>
    /// </summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    /// <param name="remoteIdentities"></param>
    /// <returns></returns>
    public List<EResult> BroadcastData(byte[] data, Channel channel, List<ulong> remoteSteamIDs, int sendFlags = NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle)
    {
        List<EResult> retval = new List<EResult>();
        foreach (ulong identity in remoteSteamIDs)
        {
            retval.Add(SendData(data, channel, identity));
        }
        return retval;
    }

    /// <summary>
    /// Message queue pump that pulls up to a set number of messages off the queue per frame.
    /// </summary>
    /// <param name="delta"></param>
    public void PerFrame(double delta)
    {
        foreach (Channel channel in Enum.GetValues(typeof(Channel)))
        {
            nint[] messages = new nint[maxMessagePerFramePerChannel];
            for (int k = 0; k < SteamNetworkingMessages.ReceiveMessagesOnChannel((int)channel, messages, maxMessagePerFramePerChannel); k++)
            {
                SteamNetworkingMessage_t steamMessage = SteamNetworkingMessage_t.FromIntPtr(messages[k]);
                byte[] payload = NetworkUtils.PtrToBytes(steamMessage.m_pData, steamMessage.m_cbSize);
                ReceiveBandwidthTracker += payload.Length;
                Logging.Log($" MSGRCV | FROM: {steamMessage.m_identityPeer.GetSteamID64()}| CHANNEL: {channel} | SIZE: {payload.Length} | Tracker: {ReceiveBandwidthTracker}", "NetworkWire");
                ProcessMessage(payload, channel,steamMessage.m_identityPeer.GetSteamID64());
                SteamNetworkingMessage_t.Release(messages[k]);
            }
        }
    }

    private void ProcessMessage(byte[] payload, Channel channel, ulong sender)
    {
        switch (channel)
        {
            case Channel.ERROR:
                break;
            case Channel.LobbyMessageBytes:
                Global.Lobby.HandleLobbyBytes(payload, sender);
                break;
            case Channel.GameObjectState:
                Global.gameState.ProcessStateUpdatePacketBytes(payload, sender);
                break;
            case Channel.PlayerInput:
                Global.gameState.ProcessPlayerInputPacketBytes(payload, sender);
                break;
            case Channel.GameStateOptions:
                Global.gameState.ProcessGameStateOptionsPacketBytes(payload, sender);
                break;
            case Channel.PlayerData:
                Global.gameState.ProcessPlayerDataPacketBytes(payload, sender);
                break;
            case Channel.NetCommands:
                RPCManager.ProcessNetCommandBytes(payload, sender);
                break;
            default:
                SteamNetworkingMessageReceivedEvent?.Invoke(channel, payload, sender);
                break;
        }
    }

    /// <summary>
    /// Takes the same parameters as a SendData() but just routes the message back to ourselves with optional network simulation.
    /// See <see cref="bDoLoopback"/>
    /// <para>IMPORTANT: Does not fully replicate network message process.</para>
    /// </summary>
    /// <param name="payload"></param>
    /// <param name="type"></param>
    private async void Loopback(Channel channel, byte[] data)
    {
        if (BandwidthTrackerCountLoopbackSend)
        {
            SendBandwidthTracker += data.Length;
        }

        Logging.Log($" MSGSND | TO: LOOPBACK | CH: {channel} | SIZE: {data.Length}", "NetworkWire");

        if (bLoopbackMemoryAllocation)
        {
            nint ptr = NetworkUtils.BytesToPtr(data);
            data = NetworkUtils.PtrToBytes(ptr, data.Length);
        }

        if (bNetworkSimulation)
        {
            await Task.Delay(iNetworkSimulationDelayMS + (int)(new Random().NextInt64(iNetworkSimulationDelayVarianceMS)));
        }

        if (BandwidthTrackerCountLoopbackReceive)
        {
            ReceiveBandwidthTracker += data.Length;
        }

        Logging.Log($" MSGRCV | FROM: LOOPBACK | CH: {channel} | SIZE: {data.Length}", "NetworkWire");
        ProcessMessage(data, channel, Global.steamid);
    }

    public void Tick(double delta)
    {
        if (BandwidthTrackerEnabled)
        {
            BandwidthTrackerTimer += delta;
            if (BandwidthTrackerTimer > BandwidthTrackerWindow)
            {
                if (SendBandwidthTracker>0 || ReceiveBandwidthTracker >0)
                {
                    Logging.Log($"Window: {BandwidthTrackerWindow} | send: {SendBandwidthTracker} | receive: {ReceiveBandwidthTracker}", "NetworkBandwidthTracker");
                }

                SendBandwidthTracker = 0;
                ReceiveBandwidthTracker = 0;
                BandwidthTrackerTimer = 0;
            }
        }
    }
}

