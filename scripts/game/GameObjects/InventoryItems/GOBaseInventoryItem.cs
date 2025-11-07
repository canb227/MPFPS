using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public abstract partial class GOBaseInventoryItem : SimpleShape, IsInventoryItem
{
    [Export]
    public virtual Node3D firstPersonScene { get; set; }

    [Export]
    public virtual Node3D thirdPersonScene { get; set; }

    [Export]
    public virtual AnimationPlayer animationPlayer { get; set; }

    [Export]
    public ImageTexture icon { get; set; }//test inventory

    public virtual InventoryGroupCategory category { get; set; }
    public virtual ulong inInventoryOf { get; set; }
    public virtual ulong equippedBySteamID { get; set; }
    public virtual bool droppable { get; set; }
    public virtual bool pickupable { get; set; } = true;
    public Node3D currentParent {get; set; }

    public abstract void HandleInput(ActionFlags actionFlags);


    public override void _Ready()
    {
        firstPersonScene.Hide();
        thirdPersonScene.Show();
        this.CollisionLayer = 1 << 3;
        Freeze = false;
    }

    public virtual void OnDropped(ulong bySteamID)
    {
        Logging.Log(bySteamID + " Just Dropped a " + category.ToString()+ $"({id})", "GOBaseInventoryItem");
        firstPersonScene.Hide();
        thirdPersonScene.Show();
        this.CollisionLayer = 1 << 3;
        Freeze = false;
        equippedBySteamID = 0;
        inInventoryOf = 0;
        if (currentParent != null)
        {
            DetachFromPlayer(currentParent);
        }

    }
    public virtual void OnEquipped(ulong bySteamID)
    {
        Logging.Log(bySteamID + " Just Equipped a " + category.ToString() + $"({id})", "GOBaseInventoryItem");
        this.CollisionLayer = 0;
        Freeze = true;
        equippedBySteamID = bySteamID;
        inInventoryOf = 0;
        if (bySteamID == Global.steamid)
        {
            firstPersonScene.Show();
        }
        else
        {
            thirdPersonScene.Show();
        }
    }
    public virtual void OnPickup(ulong bySteamID)
    {
        Logging.Log(bySteamID + " Just Picked a " + category.ToString() + " Up" + $"({id})", "GOBaseInventoryItem");
        Freeze = true;
        this.CollisionLayer = 0;
        firstPersonScene.Hide();
        thirdPersonScene.Hide();
        inInventoryOf = bySteamID;
    }
    public virtual void OnUnequipped(ulong bySteamID)
    {
        Logging.Log(bySteamID + " Just Unequipped a " + category.ToString() + $"({id})", "GOBaseInventoryItem");
        Freeze = true;
        equippedBySteamID = 0;
        inInventoryOf = bySteamID;
        firstPersonScene.Hide();
        thirdPersonScene.Hide();
    }

    public void AttachToPlayer(Node3D newParent)
    {
        Reparent(newParent, false);
        // Transform3D newTransform = Transform;
        // newTransform.Origin = new(0, 0, 0);
        Transform = Transform3D.Identity;
        currentParent = newParent;
    }

    public void DetachFromPlayer(Node3D oldParent)
    {
        Reparent(Global.gameState.GameObjectNodeParent);
        Transform3D newTransform = Transform;
        newTransform.Origin = oldParent.GlobalPosition;
        Transform = newTransform;
        currentParent = null;
    }
    
    public override void PerTickLocal(double delta)
    {
        // if(currentParent == null)
        // {
        //     base.PerFrameLocal(delta);
        // }
    }

    public override string GenerateStateString()
    {
        return $"category:{category.ToString()} | equippedBySteamID:{equippedBySteamID} | inInventoryOf {inInventoryOf}";
    }

    /// <summary>
    /// Gets the <c>GOBasePlayerCharacter</c> holding this object.
    /// </summary>
    /// <returns>
    /// <c>GOBasePlayerCharacter</c> or <c>null</c> if not found
    /// </returns>
    public GOBasePlayerCharacter GetHeldBy()
    {
        if (equippedBySteamID == 0) return null;

        if (!Global.gameState.PlayerIDToControlledCharacter.TryGetValue(equippedBySteamID, out ulong controlledChar))
        {
            return null;
        }
        else
        {
            if (!Global.gameState.GameObjects.TryGetValue(controlledChar, out GameObject gameObject))
            {
                return null;
            }
            else
            {
                return gameObject as GOBasePlayerCharacter; // making an assumption here
            }
        }
    }
}

