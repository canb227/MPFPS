using Google.Protobuf;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static SteamSockets;
using static System.Runtime.InteropServices.JavaScript.JSType;

////////////////////////////////

//READ ME FIRST

//The Steam Sockets implementation has the following BENEFITS over Messages:
//      1. Its slightly faster
//      2. It allows for actual network loopback (insertion into message queue), with network condition simulation
//      3. It has an extra (int) per message of available sidechannel to store message type (doesnt need a message wrapper!)
//      4. It is SIGNIFIGANTLY more configurable, with full control over data flows

//The Steam Sockets implementation has the following DRAWBACKS over Messages:
//      1. I CANT GET IT TO FUCKING WORK

//   For any brave soul that comes after: Sending messages appears to work, SteamRelay reports successful sending of messages with trivial payloads using both a second machine and
//      using socket pair loopbacks. In both the two machine and the loopback cases, the connection never receives any messages.


/////////////////////////////////




/// <summary>
/// Ok heres the deal. SteamNetworkingSockets is the actual Steam Networking API. SteamNetworkingMessages is the "easy" version that Valve made.
/// SteamNetworkingMessages uses SteamNetworkingSockets under the hood, using a special flag called "SymmetricConnect". 
/// 
/// https://github.com/ValveSoftware/GameNetworkingSockets/blob/a246ee39c32cc4b34580dc24d434ac9a4007a74b/include/steam/steamnetworkingtypes.h#L1373
/// 
/// With this flag set, Steam handles all the hard shit for us, and makes all peers equal (no client/server).
/// 
/// Here's the rub, SteamNetworkingMessages is cool because of that special flag, but the rest of the implementation hides complexity from us that we
/// actually need to interact with.
/// 
/// So this is effectively my reimplementation of SteamNetworkingMessages to expose those bits inside we need. May god have mercy on my soul.
/// </summary>
public class SteamSockets
{
    public const int k_nSteamNetworkingSend_NoNagle = 1;
    public const int k_nSteamNetworkingSend_NoDelay = 4;
    public const int k_nSteamNetworkingSend_Unreliable = 0;
    public const int k_nSteamNetworkingSend_Reliable = 8;
    public const int k_nSteamNetworkingSend_UnreliableNoNagle = k_nSteamNetworkingSend_Unreliable | k_nSteamNetworkingSend_NoNagle;
    public const int k_nSteamNetworkingSend_UnreliableNoDelay = k_nSteamNetworkingSend_Unreliable | k_nSteamNetworkingSend_NoDelay | k_nSteamNetworkingSend_NoNagle;
    public const int k_nSteamNetworkingSend_ReliableNoNagle = k_nSteamNetworkingSend_Reliable | k_nSteamNetworkingSend_NoNagle;

    /// <summary>
    /// SteamID to Connection Handle map - serves as a list of active peer connections
    /// </summary>
    public Dictionary<SteamNetworkingIdentity, HSteamNetConnection> connectionMap;

    public List<HSteamNetConnection> connectionList;

    public HSteamNetConnection LoopbackSendTo;
    public HSteamNetConnection LoopbackReceiveOn;


    /// <summary>
    /// Groups together collections to get messages from all of them more efficiently
    /// </summary>
    HSteamNetPollGroup pollGroup;

    public SteamRelayNetworkStatus_t steamRelayNetworkStatus;

    /// <summary>
    /// Our local listen socket - where all incoming messages arrive
    /// </summary>
    HSteamListenSocket ListenSocket;

    /// <summary>
    /// Fires whenever a connection with a peer changes - this may get spammed a lot as it fires for incoming connections from anyone, even if we dont know them.
    /// </summary>
    Callback<SteamNetConnectionStatusChangedCallback_t> SteamNetConnectionStatusChangedCallback;

    /// <summary>
    /// Fires when our connection the Steam Relay Network changes, this typically signals connecting or disconnecting, or if something has gone wrong (ie internet drops)
    /// </summary>
    Callback<SteamRelayNetworkStatus_t> SteamRelayNetworkStatusChangedCallback;

    public delegate void NewConnectionEstablished(SteamNetworkingIdentity identity);
    public static event NewConnectionEstablished NewConnectionEstablishedEvent;

    public SteamSockets()
    {
        Logging.Log("Starting Steam Sockets networking interface...", "SteamNet");
        SteamNetworkingUtils.InitRelayNetworkAccess(); //NOTE: Our access to the Steam Relay Network takes about 3 seconds after this call to be functional.
        connectionMap = new();
        connectionList = new();
        pollGroup = SteamNetworkingSockets.CreatePollGroup();

        SteamNetConnectionStatusChangedCallback = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
        SteamRelayNetworkStatusChangedCallback = Callback<SteamRelayNetworkStatus_t>.Create(OnRelayNetworkStatusChanged);

        //SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_P2P_Transport_ICE_Enable, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32);

        Logging.Log("Steam Sockets initalized.", "SteamNet");
    }

    public void RefreshSteamRelayNetworkConnection()
    {
        SteamNetworkingUtils.InitRelayNetworkAccess();
    }

    public void StartListenSocket()
    {
        Logging.Log($"Opening new Steam Listen Socket...","SteamNet");
        List<SteamNetworkingConfigValue_t> optionsList = new();

        //Set the symmetric mode flag - see top of file
        SteamNetworkingConfigValue_t option = new SteamNetworkingConfigValue_t();
        option.m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SymmetricConnect;
        option.m_val.m_int32 = 1;
        option.m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32;
        optionsList.Add(option);

        ListenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, optionsList.Count, optionsList.ToArray());
        Logging.Log($"Symmetric Steam Listen Socket ({ListenSocket.m_HSteamListenSocket}) has been opened, waiting for messages...", "SteamNet");
    }

    public void EnableLoopback()
    {
        SteamNetworkingIdentity nullptr = new();
        bool help = SteamNetworkingSockets.CreateSocketPair(out LoopbackSendTo, out LoopbackReceiveOn, true, ref nullptr, ref nullptr);
        Logging.Log($"Socket Pair Success? {help} LoopbackSendTo m_conn:{ LoopbackSendTo.m_HSteamNetConnection}, LoopbackReceiveOn m_conn:{LoopbackReceiveOn.m_HSteamNetConnection}", "SteamNet");
        SteamNetworkingSockets.ConfigureConnectionLanes(LoopbackSendTo, 1, [1], [1]);
        SteamNetworkingSockets.ConfigureConnectionLanes(LoopbackReceiveOn, 1, [1], [1]);


        connectionList.Add(LoopbackSendTo);
        SteamNetworkingSockets.SetConnectionPollGroup(LoopbackReceiveOn,pollGroup);
    }

    public void AttemptConnectionToUser(SteamNetworkingIdentity identity)
    {
        Logging.Log($"Sending symmetric connection request to {identity.GetSteamID64()}","SteamNet");
        SteamNetworkingConfigValue_t[] optionsList = new SteamNetworkingConfigValue_t[1];

        //Set the symmetric mode flag - see top of file
        SteamNetworkingConfigValue_t option = new SteamNetworkingConfigValue_t();
        option.m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SymmetricConnect;
        option.m_val.m_int32 = 1;
        option.m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32;
        optionsList[0] = (option);

        HSteamNetConnection conn = SteamNetworkingSockets.ConnectP2P(ref identity, 0, 1, optionsList.ToArray());
        //pendingConnections.Add(identity, conn);
    }


    ///Callbacks
    private void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t param)
    {
        Logging.Log($"Connection with {param.m_info.m_identityRemote.GetSteamID64()} has changed from {param.m_eOldState} to {param.m_info.m_eState}","SteamNet");
        switch (param.m_info.m_eState)
        {
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None:
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                
                if (ShouldAcceptConnectionFrom(param.m_info.m_identityRemote))
                {
                    Logging.Log($"Accepting connection request from {param.m_info.m_identityRemote.GetSteamID64()}.","SteamNet");
                    SteamNetworkingSockets.AcceptConnection(param.m_hConn);
                    
                }
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FindingRoute:
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                connectionList.Add(param.m_hConn);
                connectionMap.Add(param.m_info.m_identityRemote, param.m_hConn);
                if (SteamNetworkingSockets.SetConnectionPollGroup(param.m_hConn, pollGroup))
                {
                    Logging.Log($"Sucessfully added to pollgroup. connection: {param.m_hConn} set to poll group: {pollGroup.m_HSteamNetPollGroup}","SteamNet");
                }
                else
                {
                    Logging.Error($"Error adding to pollgroup! connection: {param.m_hConn} set to poll group: {pollGroup.m_HSteamNetPollGroup}","SteamNet");
                }
                NewConnectionEstablishedEvent?.Invoke(param.m_info.m_identityRemote);
                Logging.Log($"Connection established with {param.m_info.m_identityRemote.GetSteamID64()}.","SteamNet");
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FinWait:
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Linger:
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Dead:
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState__Force32Bit:
                break;
            default:
                break;
        }
    }


    private void OnRelayNetworkStatusChanged(SteamRelayNetworkStatus_t param)
    {
        steamRelayNetworkStatus = param;
        Logging.Log($"Steam Relay Network Status has changed to: {param.m_eAvail}", "SteamNet");
        switch (param.m_eAvail)
        {
            case ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_CannotTry:
                break;
            case ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Failed:
                break;
            case ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Previously:
                break;
            case ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Retrying:
                break;
            case ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_NeverTried:
                break;
            case ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Waiting:
                break;
            case ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Attempting:
                break;
            case ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Current:
                SteamFriends.SetRichPresence("connect", Global.steamid.ToString());
                break;
            case ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Unknown:
                break;
            case ESteamNetworkingAvailability.k_ESteamNetworkingAvailability__Force32bit:
                break;
            default:
                break;
        }
    }

    private bool ShouldAcceptConnectionFrom(SteamNetworkingIdentity m_identityRemote)
    {
        if (NetworkUtils.IsFriend(m_identityRemote))
        {
            return true;
        }

        return false;
    }


    public int ReceiveMessages(nint[] messages, int maxMessages)
    {
        return SteamNetworkingSockets.ReceiveMessagesOnPollGroup(pollGroup, messages, maxMessages);
    }

    public void DisconnectFromUser(HSteamNetConnection conn)
    {
        SteamNetworkingSockets.CloseConnection(conn, 0, "", false);
    }

    public void DisconnectFromAllUsers()
    {
        foreach (HSteamNetConnection conn in connectionList)
        {
            DisconnectFromUser(conn);
        }
    }

    public List<long> SendMessageToAllConnections(nint dataPointer, uint dataSize, int sendFlags)
    {
        List<long> results = new List<long>();
        foreach (HSteamNetConnection conn in connectionList)
        {
           results.Add( SendMessageToConnection(conn, dataPointer, dataSize, sendFlags));
        }
        return results;
    }

    public EResult DEBUGSendMessageToConnection(HSteamNetConnection conn,nint dataPointer,uint dataSize,int sendFlags,out long msgNum)
    {
        return SteamNetworkingSockets.SendMessageToConnection(conn, dataPointer, dataSize, sendFlags, out msgNum);
    }

    public long SendMessageToConnection(HSteamNetConnection conn, nint dataPointer, uint dataSize, int sendFlags)
    {
        unsafe
        {
            byte[] bytes = *(byte[]*)(dataPointer);
            Logging.Log($"Sending a message with payload (size:{dataSize}) (as string): {Encoding.UTF8.GetString(bytes, 0, bytes.Length)}", "SteamNet");
        }
        nint[] messages = new nint[1];
        unsafe
        {
            SteamNetworkingMessage_t* msg = (SteamNetworkingMessage_t*)SteamNetworkingUtils.AllocateMessage((int)dataSize);
            msg->m_conn = conn;
            msg->m_nFlags = sendFlags;
            msg->m_idxLane = 0;
            messages[0] = (nint)msg;
        }
        long[] results = new long[1];
        SteamNetworkingSockets.SendMessages(1, messages, results);
        return results[0];
        
    }

    public bool GoOnline()
    {
        throw new NotImplementedException();
    }

    public bool GoOffline()
    {
        throw new NotImplementedException();
    }

    public bool DisconnectFromUser(SteamNetworkingIdentity identity)
    {
        throw new NotImplementedException();
    }

    public EResult SendMessageToUser(IMessage message, SteamNetworkingIdentity identity, NetworkManager.NetworkChannel channel, int sendFlags)
    {
        throw new NotImplementedException();
    }

    public EResult SendBytesToUser(byte[] data, SteamNetworkingIdentity identity, NetworkManager.NetworkChannel channel, int sendFlags)
    {
        throw new NotImplementedException();
    }

    public List<(SteamNetworkingIdentity peer, EResult result)> SendMessageToAllPeers(IMessage message, NetworkManager.NetworkChannel channel, int sendFlags)
    {
        throw new NotImplementedException();
    }

    public List<(SteamNetworkingIdentity peer, EResult result)> SendBytesToAllPeers(byte[] data, NetworkManager.NetworkChannel channel, int sendFlags)
    {
        throw new NotImplementedException();
    }

    public bool IsUserConnectedPeer(SteamNetworkingIdentity identity)
    {
        throw new NotImplementedException();
    }

    public ESteamNetworkingConnectionState GetConnectionInfo(SteamNetworkingIdentity identity, out SteamNetConnectionInfo_t connectionInfo, out SteamNetConnectionRealTimeStatus_t connectionStatus)
    {
        throw new NotImplementedException();
    }

    public bool GetNextPendingSteamMessageOnChannel(NetworkManager.NetworkChannel channel, out SteamNetworkingMessage_t msg)
    {
        throw new NotImplementedException();
    }

    public int GetNumPendingSteamMessagesOnChannel(NetworkManager.NetworkChannel channel, int maxNumMessages, out List<SteamNetworkingMessage_t> messages)
    {
        throw new NotImplementedException();
    }

    public void HandleIncomingMessage(SteamNetworkingMessage_t message)
    {
        throw new NotImplementedException();
    }
}