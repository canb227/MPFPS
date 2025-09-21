using System;
using Godot;
using Godot.Collections;
using SteamMultiplayerPeerCSharp;
using Steamworks;

public partial class SteamNetworkManager : NetworkManager
{
    public const ushort DEFAULT_PORT = 0;

    protected Callback<GameRichPresenceJoinRequested_t> m_GameRichPresenceJoinRequested;

    private SteamMultiplayerPeer _SteamPeer;
    private Node node;
    public override void _Ready()
    {

        node = GD.Load<PackedScene>("res://scenes/GDSteamNetwork.tscn").Instantiate<Node>();
        AddChild(node);

        Multiplayer.ConnectedToServer += Multiplayer_ConnectedToServer;
        Multiplayer.ConnectionFailed += Multiplayer_ConnectionFailed;
        Multiplayer.PeerConnected += Multiplayer_PeerConnected;
        Multiplayer.PeerDisconnected += Multiplayer_PeerDisconnected;
        m_GameRichPresenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnSteamJoinRequested);
    }

    private void OnSteamJoinRequested(GameRichPresenceJoinRequested_t param)
    {
        Logging.Log($"Steam Rich Presence Join Requested, attempting to join a server hosted by: {param.m_rgchConnect} ({ulong.Parse(param.m_rgchConnect)})", "SteamNetwork");
        //JoinServer(param.m_steamIDFriend.m_SteamID);
        node.Call("JoinServer", ulong.Parse(param.m_rgchConnect));
        if (Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Disconnected)
        {
            Logging.Error("Joining server failed.", "SteamNetwork");
        }
        else
        {
            Logging.Log("Successfully joined server. Now joinable!", "SteamNetwork");

            SteamFriends.SetRichPresence("connect", param.m_rgchConnect);
        }
        Global.ui.SwitchFullScreenUI("DEBUG_launcher");
    }

    private void Multiplayer_PeerDisconnected(long id)
    {
        Logging.Log($"PeerDisconnected: {id}", "SteamNetwork");
    }

    private void Multiplayer_PeerConnected(long id)
    {

        Logging.Log($"PeerConnected: {id}", "SteamNetwork");
    }

    private void Multiplayer_ConnectionFailed()
    {
        Logging.Log("ConnectionFailed", "SteamNetwork");
    }

    private void Multiplayer_ConnectedToServer()
    {
        Logging.Log("ConnectedToServer", "SteamNetwork");
    }

    public override void HostServer(ushort port = DEFAULT_PORT)
    {
        node.Call("HostServer",Global.steamid);
        if (Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Disconnected)
        {
            Logging.Error("Hosting server failed.", "SteamNetwork");
        }
        else
        {
            Logging.Log("Successfully hosted server. Now joinable!", "SteamNetwork");
            SteamFriends.SetRichPresence("connect", Global.steamid.ToString());
            InvokeHostedServerEvent();
        }
        //_SteamPeer = new SteamMultiplayerPeer();
        //_SteamPeer.CreateHost(port);
        //Logging.Log($"Am Server?:{Multiplayer.IsServer()}", "SteamNetwork");
        //if (_SteamPeer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Disconnected)
        //{
        //    Logging.Error("Hosting server failed.", "SteamNetwork");
        //}
        //else
        //{
        //    Logging.Log("Successfully hosted server. Now joinable!", "SteamNetwork");
        //    SteamFriends.SetRichPresence("connect", Global.steamid.ToString());
        //    InvokeHostedServerEvent();
        //}
        //Multiplayer.MultiplayerPeer = _SteamPeer.MultiplayerPeer;

    }

    public override void JoinServer(ulong steamID, ushort port = DEFAULT_PORT)
    {
        _SteamPeer = new SteamMultiplayerPeer();
        _SteamPeer.CreateClient(steamID, port);
        if (Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Disconnected)
        {
            Logging.Error("Joining server failed.", "SteamNetwork");
        }
        else
        {
            Logging.Log("Successfully joined server. Now joinable!", "SteamNetwork");

            SteamFriends.SetRichPresence("connect", steamID.ToString());
        }
        Multiplayer.MultiplayerPeer = _SteamPeer.MultiplayerPeer;

    }

    public override void LeaveServer()
    {
        if (Multiplayer.IsServer())
        {
            InvokeClosedServerEvent();
        }
        else
        {
            InvokeLeftServerEvent();
        }
        Multiplayer.MultiplayerPeer = null;
    }

    public override ulong peerIDToUserID(int peerID)
    {
        return _SteamPeer.GetSteam64FromPeerId(peerID);
    }

    public override int userIDToPeerID(ulong userID)
    {
        return _SteamPeer.GetPeerIdFromSteam64(userID);
    }
}

