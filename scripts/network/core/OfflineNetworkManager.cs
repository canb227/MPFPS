using Godot;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;




//DEPRECATED













public partial class OfflineNetworkManager : NetworkManager
{

    public string localhost = "127.0.0.1";

    public override void _Ready()
    {

        Multiplayer.ConnectedToServer += Multiplayer_ConnectedToServer;
        Multiplayer.ConnectionFailed += Multiplayer_ConnectionFailed;
        Multiplayer.PeerConnected += Multiplayer_PeerConnected;
        Multiplayer.PeerDisconnected += Multiplayer_PeerDisconnected;

    }

    private void Multiplayer_PeerDisconnected(long id)
    {
        Logging.Log($"API: PeerDisconnected: {id}", "OfflineNetwork");
    }

    private void Multiplayer_PeerConnected(long id)
    {
        Logging.Log($"API: PeerConnected: {id}", "OfflineNetwork");
    }

    private void Multiplayer_ConnectionFailed()
    {
        Logging.Log("API: ConnectionFailed", "OfflineNetwork");
    }

    private void Multiplayer_ConnectedToServer()
    {
        Logging.Log("API: ConnectedToServer", "OfflineNetwork");
    }

    public override void HostServer(ushort port = 4433)
    {
        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        peer.CreateServer(port);
        if (peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Disconnected)
        {
            Logging.Error("Hosting server failed.", "OfflineNetwork");
        }
        else
        {
            Logging.Log("Successfully hosted server. Now joinable!", "OfflineNetwork");
            Multiplayer.MultiplayerPeer = peer;
            InvokeHostedServerEvent();
        }

    }

    public override void JoinServer(ulong peerID, ushort port = 4433)
    {
        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
        peer.CreateClient(localhost,port);
        if (peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Disconnected)
        {
            Logging.Error("Joining server failed.", "OfflineNetwork");
        }
        else
        {
            Logging.Log("Successfully joined localhost server.", "OfflineNetwork");
            Multiplayer.MultiplayerPeer = peer;
        }

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
        return (ulong)peerID;
    }

    public override int userIDToPeerID(ulong userID)
    {
        return (int)userID;
    }
}

