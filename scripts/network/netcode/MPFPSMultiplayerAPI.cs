using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class MPFPSMultiplayerAPI : MultiplayerApiExtension
{
    SceneMultiplayer baseMP = new();

    public MPFPSMultiplayerAPI()
    {
        Logging.Log($"Starting MPAPI Override!", "MPAPI");
        MultiplayerPeer = null;
        ConnectedToServer += guh;
        baseMP.ConnectedToServer += guh2;
        PeerConnected += guh3;
        baseMP.PeerConnected += guh4;
    }

    private void guh()
    {
        Logging.Warn("guh", "guh");
    }

    private void guh2()
    {
        Logging.Warn("guh2", "guh2");
    }

    private void guh3(long id)
    {
        Logging.Warn("guh3", "guh3");
    }

    private void guh4(long id)
    {
        Logging.Warn("guh4", "guh4");
    }

    public override Error _Rpc(int peer, GodotObject @object, StringName method, Godot.Collections.Array args)
    {
        Logging.Log($"RPC: {method}", "MPAPI");
        return baseMP.Rpc(peer, @object, method, args);
    }

    public override Error _ObjectConfigurationAdd(GodotObject @object, Variant configuration)
    {
        Logging.Log($"SyncObjAdded", "MPAPI");
        return baseMP.ObjectConfigurationAdd(@object, configuration);
    }

    public override Error _ObjectConfigurationRemove(GodotObject @object, Variant configuration)
    {
        Logging.Log($"SyncObjRemoved", "MPAPI");
        return baseMP.ObjectConfigurationRemove(@object, configuration);
    }

    public override void _SetMultiplayerPeer(MultiplayerPeer multiplayerPeer)
    {
        Logging.Log($"MPAPI-Peer Set", "MPAPI");
        baseMP.MultiplayerPeer = multiplayerPeer;
    }

    public override MultiplayerPeer _GetMultiplayerPeer()
    {
        return baseMP.GetMultiplayerPeer();
    }

    public override int _GetUniqueId()
    {
        return baseMP.GetUniqueId();
    }

    public override int _GetRemoteSenderId()
    {
        return baseMP.GetRemoteSenderId();
    }
    public override int[] _GetPeerIds()
    {
        return baseMP.GetPeers();
    }
}

