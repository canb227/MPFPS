using Google.Protobuf;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
    /// <summary>
    /// SteamID to Connection Handle map - serves as a list of active peer connections
    /// </summary>
    public Dictionary<SteamNetworkingIdentity, HSteamNetConnection> activeConnections;

    /// <summary>
    /// SteamID to Connection Handle map - serves as a list of active peer connections
    /// </summary>
    Dictionary<SteamNetworkingIdentity, HSteamNetConnection> pendingConnections;

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

    public SteamSockets()
    {
        Logging.Log("Starting Steam Sockets networking interface...", "SteamNet");
        SteamNetworking.AllowP2PPacketRelay(true);
        SteamNetworkingUtils.InitRelayNetworkAccess(); //NOTE: Our access to the Steam Relay Network takes about 3 seconds after this call to be functional.
        pendingConnections = new();
        activeConnections = new();
        pollGroup = SteamNetworkingSockets.CreatePollGroup();

        SteamNetConnectionStatusChangedCallback = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
        SteamRelayNetworkStatusChangedCallback = Callback<SteamRelayNetworkStatus_t>.Create(OnRelayNetworkStatusChanged);
        Logging.Log("Steam Sockets initalized.", "SteamNet");
    }

    public void RefreshSteamRelayNetworkConnection()
    {
        SteamNetworkingUtils.InitRelayNetworkAccess();
    }

    public void StartListenSocket()
    {
        Logging.Log($"Opening new Steam Listen Socket...");
        List<SteamNetworkingConfigValue_t> optionsList = new();

        //Set the symmetric mode flag - see top of file
        SteamNetworkingConfigValue_t option = new SteamNetworkingConfigValue_t();
        option.m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SymmetricConnect;
        option.m_val.m_int32 = 1;
        option.m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32;
        optionsList.Add(option);

        ListenSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, optionsList.Count, optionsList.ToArray());
        Logging.Log($"Symmetric Steam Listen Socket has been opened, waiting for messages...", "SteamNet");
    }


    public void AttemptConnectionToUser(SteamNetworkingIdentity identity)
    {
        Logging.Log($"Sending symmetric connection request to {identity.GetSteamID64()}","SteamNet");
        List<SteamNetworkingConfigValue_t> optionsList = new();

        //Set the symmetric mode flag - see top of file
        SteamNetworkingConfigValue_t option = new SteamNetworkingConfigValue_t();
        option.m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SymmetricConnect;
        option.m_val.m_int32 = 1;
        option.m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32;
        optionsList.Add(option);

        HSteamNetConnection conn = SteamNetworkingSockets.ConnectP2P(ref identity, 0, optionsList.Count, optionsList.ToArray());
        pendingConnections.Add(identity, conn);
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
                    pendingConnections.Remove(param.m_info.m_identityRemote);
                    activeConnections.Add(param.m_info.m_identityRemote, param.m_hConn);
                    SteamNetworkingSockets.SetConnectionPollGroup(param.m_hConn,pollGroup);
                }
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FindingRoute:
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
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
        if (pendingConnections.ContainsKey(m_identityRemote))
        {
            return true;
        }
        return false;
    }



    public long[] SendBytesToUser(SteamNetworkingIdentity identity, byte[] data, int length, int sidechannel=0)
    {
        //Allocate memory for the steam message. https://partner.steamgames.com/doc/api/ISteamNetworkingUtils#AllocateMessage
        nint allocatedMessage = SteamNetworkingUtils.AllocateMessage(length);

        //This is the C# version of dereferencing the pointer
        SteamNetworkingMessage_t steamMessage = SteamNetworkingMessage_t.FromIntPtr(allocatedMessage);
        
        //Set who this message is going to
        steamMessage.m_conn = activeConnections[identity];
        
        //Fill the memory the message payload pointer points to with the data
        Marshal.Copy(data, 0, steamMessage.m_pData, data.Length);

        //We get one int of sidechannel data
        steamMessage.m_nUserData = sidechannel;

        //This is the C# version of getting the address of the message object. This is maybe the same address as "allocatedMessage" but I dont fucking know
        nint msg = Unsafe.As<SteamNetworkingMessage_t, nint>(ref steamMessage);

        //The Steam send messages always takes an array of messages to batch send - but this just sends one
        nint[] msgs = new nint[1];
        msgs[1] = msg;

        long[] results = new long[1];
        SteamNetworkingSockets.SendMessages(1, msgs,results);

        return results;
    }

    public nint[] ReceiveMessages(int maxMessages)
    {
        nint[] messages = new nint[maxMessages];
        SteamNetworkingSockets.ReceiveMessagesOnPollGroup(pollGroup, messages, maxMessages);
        return messages;
    }
}