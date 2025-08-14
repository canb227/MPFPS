using Godot;
using Google.Protobuf;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using static Godot.HttpRequest;
using static System.Runtime.InteropServices.JavaScript.JSType;

/// <summary>
/// one byte (0-255) value for network message type
/// </summary>
public enum NetType
{
    ERROR = 0,

    //Byte Types
    BYTES_LOBBY = 1,


    //100

    //IMessage Types
    MESSAGE_LOBBY = 101,


    //200


    //Other Types
    DEBUG_UTF8 = 254,
    EMPTY = 255
}

/// <summary>
/// Handles low level networking functions - implemented using SteamMessages
/// </summary>
public partial class SteamNetwork : Node
{

    Callback<SteamNetworkingMessagesSessionRequest_t> SessionRequest;
    Callback<SteamNetworkingMessagesSessionFailed_t> SessionFailed;

    Callback<SteamRelayNetworkStatus_t> RelayNetworkStatusChanged;
    Callback<SteamNetConnectionStatusChangedCallback_t> ConnectionStatusChanged;

    /// <summary>
    /// Max number of messages to attempt to process per frame. If we get frame delays because of spiky network traffic this needs turned down
    /// </summary>
    public int maxMessagePerFrame = 100;

    /// <summary>
    /// if true, messages we send to ourself  get processed as if they had been sent over the network. If false, messages sent to ourself are discarded.
    /// <para>IMPORTANT: Does not fully replicate network message process. Skips payload memory allocation and payload byte packing.</para>
    /// </summary>
    public bool bDoLoopback = false;

    /// <summary>
    /// If true, introduce artifical network conditions to loopback messages. TESTING ONLY
    /// </summary>
    private bool bNetworkSimulation = false;
    private double dNetworkSimulationDelay = 0.05f;
    private double dNetworkSimulationDelayVariance = 0.1f;

    public override void _Ready()
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
            await ToSignal(GetTree().CreateTimer(1), "timeout");
            
            SteamNetworkingUtils.GetRelayNetworkStatus(out SteamRelayNetworkStatus_t details);
            if (details.m_eAvail!=ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Current)
            {
                Logging.Warn("Steam Relay Networking not connected, force retrying access...","NetworkRelay");
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
        Logging.Log($"Connection Status to Steam Relay Network has changed: {param.m_eAvail}","NetworkRelay");
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
            Logging.Log($"New Session Established with {idstring}","NetworkSession");
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

    /// <summary>
    /// Helper function for sending Google Protobuf Messages. See <see cref="SendData(byte[], NetType, SteamNetworkingIdentity)"/>
    /// </summary>
    /// <param name="message"></param>
    /// <param name="type"></param>
    /// <param name="remoteIdentity"></param>
    /// <returns></returns>
    public EResult SendMessage(IMessage message, NetType type, SteamNetworkingIdentity remoteIdentity)
    {
        return SendData(message.ToByteArray(), type, remoteIdentity);
    }

    /// <summary>
    /// Message queue pump that pulls up to a set number of messages off the queue per frame. Deconstructs the byte array payload into the one-byte type (derived from the first byte of payload) and the byte array data (the rest of the payload).
    /// </summary>
    /// <param name="delta"></param>
    public override void _Process(double delta)
    {
        nint[] messages = new nint[maxMessagePerFrame];
        for (int i = 0; i < SteamNetworkingMessages.ReceiveMessagesOnChannel(0, messages, maxMessagePerFrame); i++)
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

            //BYTE TYPES
            case NetType.BYTES_LOBBY:
                Global.Lobby.HandleLobbyBytes(data, fromSteamID);
                break;


            //MESSAGE TYPES
            case NetType.MESSAGE_LOBBY:
                //Global.Lobby.HandleLobbyMessage(data, fromSteamID);
                break;

            //OTHER TYPES
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
            await ToSignal(GetTree().CreateTimer(dNetworkSimulationDelay + (new Random().NextDouble() * dNetworkSimulationDelayVariance)), "timeout");
        }
        Logging.Log($" MSGRCV | FROM: LOOPBACK | TYPE: {type.ToString()} | SIZE: {data.Length}", "NetworkWire");
        ProcessData(data, type, Global.steamid);
    }
}

