using Godot;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class NetworkManager : Node
{
    private SteamSockets SteamNet;


    /// <summary>
    /// Fires for lots of reasons, including: Clicking "Join to Player" in Steam and Accepting an invite to play in Steam.
    /// ONLY FIRES IF GAME IS ALREADY RUNNING.
    /// </summary>
    protected Callback<GameRichPresenceJoinRequested_t> m_GameRichPresenceJoinRequested;


    public override void _Ready()
    {
        SteamNet = new SteamSockets();
        m_GameRichPresenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnGameRichPresenceJoinRequested);

    }

    public bool IsConnected()
    {
        return (SteamNet.activeConnections.Count > 0);
    }

    public bool IsConnectedToUser(SteamNetworkingIdentity identity)
    {
        return (SteamNet.activeConnections.ContainsKey(identity));
    }

    public ESteamNetworkingAvailability GetSteamNetworkStatus()
    {
        return SteamNet.steamRelayNetworkStatus.m_eAvail;
    }

    private void OnGameRichPresenceJoinRequested(GameRichPresenceJoinRequested_t param)
    {
        Logging.Log($"Steam Rich Presence Join Requested to: {param.m_rgchConnect}. Attempting to join...","Network");
        SteamNet.AttemptConnectionToUser(NetworkUtils.SteamIDStringToIdentity(param.m_rgchConnect));
    }

}

