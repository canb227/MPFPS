using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public abstract partial class GOBaseInventoryItem : GOBaseRigidBody, IsInventoryItem
{
    [Export]
    public virtual Node3D firstPersonScene { get; set; }

    [Export]
    public virtual Node3D thirdPersonScene { get; set; }

    [Export]
    public virtual AnimationPlayer animationPlayer { get; set; }

    [Export]
    public ImageTexture icon { get; set; }

    public virtual InventoryGroupCategory category { get; set; }
    public virtual ulong inInventoryOf { get; set; }
    public virtual ulong equippedBy { get; set; }
    public virtual bool droppable { get; set; }

    public abstract void HandleInput(ActionFlags actionFlags);


    public override void _Ready()
    {
        firstPersonScene.Hide();
        thirdPersonScene.Show();
        this.CollisionLayer = 2;
    }

    public virtual void OnDropped(ulong byID)
    {
        this.CollisionLayer = 2;
        equippedBy = 0;
        inInventoryOf = 0;
    }
    public virtual void OnEquipped(ulong byID)
    {
        this.CollisionLayer = 0;
        equippedBy = byID;
        inInventoryOf = 0;
        if (byID == Global.steamid)
        {
            firstPersonScene.Show();
        }
        else
        {
            thirdPersonScene.Show();
        }
    }
    public virtual void OnPickup(ulong byID)
    {
        this.CollisionLayer = 0;
        firstPersonScene.Hide();
        thirdPersonScene.Hide();
        inInventoryOf = byID;
    }
    public virtual void OnUnequipped(ulong byID)
    {
        equippedBy = 0;
        inInventoryOf = byID;
        firstPersonScene.Hide();
        thirdPersonScene.Hide();
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

    public override string GenerateStateString()
    {
        return $"category:{category.ToString()} | equippedBy:{equippedBy} | inInventoryOf {inInventoryOf}";
    }

    public override byte[] GenerateStateUpdate()
    {
        return new byte[0];
    }
    public override void ProcessStateUpdate(byte[] update)
    {

    }
}

