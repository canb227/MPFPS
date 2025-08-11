using Godot;
using Google.Protobuf;
using NetworkMessages;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class NetworkManager : Node
{
  public SteamNetworkInterface SteamNet;

    private int defaultMaxMessagesPerFramePerChannel = 10;

    private bool loopbackNetworkSim = true;
    private double loopbackNetworkDelay = 0.1f;
    private double loopbackNetworkDelayVariance = 0.1f;

    public enum NetworkChannel
    {
        SteamNet = 0,
        Chat = 1,
    }

    public NetworkChannel[] channelsToRead = [0];

    /// <summary>
    /// Fires for lots of reasons, including: Clicking "Join to Player" in Steam and Accepting an invite to play in Steam.
    /// ONLY FIRES IF GAME IS ALREADY RUNNING.
    /// </summary>
    protected Callback<GameRichPresenceJoinRequested_t> m_GameRichPresenceJoinRequested;


    public override void _Ready()
    {
        SteamNet = new SteamMessages();
        SteamNet.GoOnline();
        SteamNet.EnableLoopback();
        m_GameRichPresenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnGameRichPresenceJoinRequested);
        SteamFriends.SetRichPresence("connect",Global.steamid.ToString());

    }

    public override void _Process(double delta)
    {

        foreach (NetworkChannel channel in channelsToRead)
        {
            int numMessages = SteamNet.GetNumPendingSteamMessagesOnChannel(channel, defaultMaxMessagesPerFramePerChannel, out List<SteamNetworkingMessage_t> messages);
            for (int i = 0; i <numMessages; i++)
            {
                SteamNetworkingMessage_t message = messages[i];
                Logging.Log($"Message received from {message.m_identityPeer.GetSteamID64()} on channel {channel}", "SteamNetWire");
                HandleSteamMessage(message, channel);
            }

        }
    }

    private void HandleSteamMessage(SteamNetworkingMessage_t message, NetworkChannel channel)
    {
        switch (channel)
        {
            case NetworkChannel.SteamNet:
                SteamNet.HandleSteamMessage(message);
                break;
            case NetworkChannel.Chat:
                ChatManager.HandleSteamMessage(message);
                break;
            default:
                break;
        }
    }

    public EResult SendMessage(IMessage message,ulong steamID,NetworkChannel channel, int sendFlags=NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle)
    {
        return SteamNet.SendMessageToUser(message, NetworkUtils.SteamIDToIdentity(steamID),channel,sendFlags);
    }

    public List<(ulong,EResult)> BroadcastMessage(IMessage message, NetworkChannel channel, int sendFlags = NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle)
    {
        List<(ulong peer,EResult result)> results = new List<(ulong,EResult)>();
        foreach ( (SteamNetworkingIdentity peer,EResult result) in SteamNet.SendMessageToAllPeers(message,channel,sendFlags))
        {
            results.Add((peer.GetSteamID64(), result));
        }
        return results;
    }

    private void OnGameRichPresenceJoinRequested(GameRichPresenceJoinRequested_t param)
    {
        Logging.Log($"Steam Rich Presence Join Requested to: {param.m_rgchConnect}. Attempting to join...","SteamAPI");
        SteamNet.AttemptConnectionToUser(NetworkUtils.SteamIDStringToIdentity(param.m_rgchConnect));
    }

    public ESteamNetworkingAvailability GetSteamRelayNetworkStatus()
    {
        return SteamNet.GetSteamRelayNetworkStatus();
    }

    public void NetworkCleanup()
    {
        SteamNet.DisconnectFromAllUsers();
        SteamNet.GoOffline();
    }

    public async void Loopback(IMessage message, SteamNetworkingIdentity identity, NetworkManager.NetworkChannel channel,int sendFlags)
    {

        if (loopbackNetworkSim)
        {
            await ToSignal(GetTree().CreateTimer(loopbackNetworkDelay + (new Random().NextDouble() * loopbackNetworkDelayVariance)), "timeout");
        }
        Logging.Log($"Message received on Loopback on channel {channel}", "SteamNetWire");
        switch (channel)
        {
            case NetworkManager.NetworkChannel.SteamNet:
                break;
            case NetworkManager.NetworkChannel.Chat:
                ChatManager.HandleChatMessage(message as ChatMessage, identity.GetSteamID64());
                break;
            default:
                break;
        }
    }

    public List<ulong> GetConnectedPeerList()
    {
        return SteamNet.GetConnectedPeerList();
    }
}

