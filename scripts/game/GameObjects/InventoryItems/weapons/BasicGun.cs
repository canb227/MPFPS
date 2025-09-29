using Godot;
using Godot.Collections;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public partial class BasicGun : GOBaseRigidBody, IsInventoryItem, IsInteractable
{

    public ulong inInventoryOf { get; set; }
    public ulong equippedBy { get; set; }
    public  bool droppable { get; set; }

    [Export]
    public Node3D firstPersonScene { get; set; }

    [Export]
    public Node3D thirdPersonScene { get; set; }

    public InventoryGroupCategory category { get; set; } = InventoryGroupCategory.Weapon;
    public override float priority { get; set; } = 10;

    private MeshInstance3D mesh {  get; set; }
    private CollisionShape3D collider {  get; set; }
    public ulong lastInteractTick { get; set; }
    public ulong lastInteractPlayer { get; set; }
    public Array<Node> triggers { get; set; }
    public ulong cooldown { get; set; }
    Array<Trigger> IsInteractable.triggers { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public void HandleInput(ActionFlags actionFlags)
    {
        if (actionFlags.HasFlag(ActionFlags.Fire))
        {
            Fire();
        }
    }






    public override void PerFrameAuth(double delta)
    {
        throw new NotImplementedException();
    }

    public override void PerFrameLocal(double delta)
    {
        throw new NotImplementedException();
    }

    public override void PerTickAuth(double delta)
    {
        throw new NotImplementedException();
    }

    public override void PerTickLocal(double delta)
    {
        throw new NotImplementedException();
    }

    public override void ProcessStateUpdate(byte[] update)
    {
        BasicGunStateUpdate upd = MessagePackSerializer.Deserialize<BasicGunStateUpdate>(update);
        GlobalPosition = upd.position;
        GlobalRotation = upd.rotation;
        inInventoryOf = upd.inInventoryOf;
        equippedBy = upd.equippedBy;
    }
    public override string GenerateStateString()
    {
        return MessagePackSerializer.ConvertToJson(GenerateStateUpdate());
    }

    public override byte[] GenerateStateUpdate()
    {
        BasicGunStateUpdate upd = new BasicGunStateUpdate();
        upd.position = GlobalPosition;
        upd.rotation = GlobalRotation;
        upd.equippedBy = equippedBy;
        upd.inInventoryOf = inInventoryOf;
        return MessagePackSerializer.Serialize(upd);
    }

    private void Fire()
    {
        throw new NotImplementedException();
    }

    public void OnPickup(ulong byID)
    {
        mesh.Hide();
        collider.Disabled = true;
        GlobalPosition = Vector3.Zero;
        inInventoryOf = byID;
    }

    public void OnUnequipped(ulong byID)
    {
        equippedBy = 0;
        inInventoryOf = byID;
    }

    public void OnInteract(ulong byID)
    {
        
        OnPickup(byID);
    }

    public void OnDropped(ulong byID)
    {
        firstPersonScene.Hide();
        thirdPersonScene.Hide();
        inInventoryOf = 0;
        equippedBy = 0;
        this.GlobalPosition = PCUtils.InFrontOf(Global.gameState.PlayerCharacters[byID], 1);
        mesh.Show();
        collider.Disabled = false;
        this.SetPhysicsProcess(true);
    }

    public void OnEquipped(ulong byID)
    {
        equippedBy = byID;

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