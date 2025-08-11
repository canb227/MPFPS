using Google.Protobuf;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public interface SteamNetworkInterface
{

    /// <summary>
    /// Allows the Steam Networking Interface to start getting messages 
    /// </summary>
    /// <returns></returns>
    public bool GoOnline();

    /// <summary>
    /// Stops the Steam Networking Interface from getting messages
    /// </summary>
    /// <returns></returns>
    public bool GoOffline();

    /// <summary>
    /// Attempts to establish connection with the given steam user. We won't know this worked till later.
    /// </summary>
    /// <param name="identity"></param>
    public void AttemptConnectionToUser(SteamNetworkingIdentity identity);


    /// <summary>
    /// /// Disconnects from the given peer. It is very likely you will reconnect right away if messages are in flight
    /// </summary>
    /// <param name="identity"></param>
    /// <returns>true if disconnect successful</returns>
    public bool DisconnectFromUser(SteamNetworkingIdentity identity);

    /// <summary>
    /// Disconnects from all peers. It is very likely you will reconnect right away if messages are in flight
    /// </summary>
    public void DisconnectFromAllUsers();

    /// <summary>
    /// Sends a protobuf message to a steam user
    /// </summary>
    /// <param name="message"></param>
    /// <param name="identity"></param>
    /// <param name="channel"></param>
    /// <param name="sendFlags"></param>
    /// <returns></returns>
    public EResult SendMessageToUser(IMessage message, SteamNetworkingIdentity identity, NetworkManager.NetworkChannel channel, int sendFlags);

    /// <summary>
    /// Sends a byte array to a steam user
    /// </summary>
    /// <param name="data"></param>
    /// <param name="identity"></param>
    /// <param name="channel"></param>
    /// <param name="sendFlags"></param>
    /// <returns></returns>
    public EResult SendBytesToUser(byte[] data, SteamNetworkingIdentity identity, NetworkManager.NetworkChannel channel, int sendFlags);

    /// <summary>
    /// Sends a protobuf message to all registered peers
    /// </summary>
    /// <param name="message"></param>
    /// <param name="channel"></param>
    /// <param name="sendFlags"></param>
    /// <returns>a list of tuples holding results for each peer we sent this to</returns>
    public List<(SteamNetworkingIdentity peer, EResult result)> SendMessageToAllPeers(IMessage message, NetworkManager.NetworkChannel channel, int sendFlags);

    /// <summary>
    /// Sends a byte array to all registered peers
    /// </summary>
    /// <param name="data"></param>
    /// <param name="channel"></param>
    /// <param name="sendFlags"></param>
    /// <returns>a list of tuples holding results for each peer we sent this to</returns>
    public List<(SteamNetworkingIdentity peer, EResult result)> SendBytesToAllPeers(byte[] data, NetworkManager.NetworkChannel channel, int sendFlags);

    /// <summary>
    /// Returns true is we are connected to the given steam user
    /// </summary>
    /// <param name="identity"></param>
    /// <returns></returns>
    public bool IsUserConnectedPeer(SteamNetworkingIdentity identity);

    /// <summary>
    /// If we are connected to the given user, return the connection info struct from SteamNetworking (mostly static data) and the connection status struct from SteamNetworking (mostly real-time data like ping)
    /// </summary>
    /// <param name="identity"></param>
    /// <returns></returns>
    public ESteamNetworkingConnectionState GetConnectionInfo(SteamNetworkingIdentity identity, out SteamNetConnectionInfo_t connectionInfo, out SteamNetConnectionRealTimeStatus_t connectionStatus);

    /// <summary>
    /// Pulls a single message off the requested channel - if there is one
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="msg"></param>
    /// <returns>true if a message was set, false if the channel had no messages</returns>
    public bool GetNextPendingSteamMessageOnChannel(NetworkManager.NetworkChannel channel, out SteamNetworkingMessage_t msg);

    /// <summary>
    /// Pulls up to maxNumMessages off of the requested channel
    /// </summary>
    /// <param name="channel"></param>
    /// <param name=""></param>
    /// <returns>number of messages actually retrieved</returns>
    public int GetNumPendingSteamMessagesOnChannel(NetworkManager.NetworkChannel channel, int maxNumMessages, out List<SteamNetworkingMessage_t> messages);
    public void HandleSteamMessage(SteamNetworkingMessage_t message);
    public void EnableLoopback();
    public void DisableLoopback();
    public ESteamNetworkingAvailability GetSteamRelayNetworkStatus();

}
