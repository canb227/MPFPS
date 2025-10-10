using Godot;
using Godot.Collections;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public partial class BasicGun : GOBaseRigidBody, IsInventoryItem
{
    public InventoryGroupCategory category { get; set; }
    public ulong inInventoryOf { get; set; }
    public ulong equippedBy { get; set; }
    public bool droppable { get; set; }
    public Node3D firstPersonScene { get; set; }
    public Node3D thirdPersonScene { get; set; }

    public override string GenerateStateString()
    {
        return "help";
    }

    public override byte[] GenerateStateUpdate()
    {
        return new byte[0];
    }

    public void HandleInput(ActionFlags actionFlags)
    {
        
    }

    public void OnDropped(ulong byID)
    {
        
    }

    public void OnEquipped(ulong byID)
    {
        
    }

    public void OnPickup(ulong byID)
    {
        
    }

    public void OnUnequipped(ulong byID)
    {
        
    }

    public override void PerFrameAuth(double delta)
    {
        
    }

    public override void PerFrameLocal(double delta)
    {
        
    }

    public override void PerFrameShared(double delta)
    {
        
    }

    public override void PerTickAuth(double delta)
    {
        
    }

    public override void PerTickLocal(double delta)
    {
        
    }

    public override void PerTickShared(double delta)
    {
        
    }

    public override void ProcessStateUpdate(byte[] update)
    {
        
    }
}

[MessagePackObject]
public struct BasicGunStateUpdate
{
    [Key(0)]
    public ulong inInventoryOf;
    [Key(1)]
    public ulong equippedBy;
    [Key(2)]
    public Vector3 position;
    [Key(3)]
    public Vector3 rotation;
}