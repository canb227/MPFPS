using System.Net.Sockets;
using Godot;
using MessagePack;

[GlobalClass]
public partial class GOAmmoBox : SimpleShape
{
    [Export] public CollisionShape3D collider { get; set; }
    [Export] public AmmoType ammoType { get; set; }
    [Export] public int ammoAmount { get; set; }

    public override void _Ready()
    {
        base._Ready();
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void PickupAmmo()
    {
        collider.Disabled = true;
        Visible = false;
    }

    public override bool InitFromData(GameObjectConstructorData data)
    {
        if (base.InitFromData(data))
        {
            return true;
        }
        return false;
    }
}