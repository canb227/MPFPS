using Godot;
using ImGuiGodot.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class Hands : GOBaseInventoryItem
{
    public override InventoryGroupCategory category { get; set; } = InventoryGroupCategory.Hands; 
    public override bool droppable { get; set; } = false;
    
    public IsHoldable holding { get; set; }

    private ActionFlags lastTickActions;
    private RayCast3D rayCast;

    [Export]
    Node3D HoldPosition { get; set; }

    Node3D CurrentHoldPosition { get; set; }

    public override void _Ready()
    {
        base._Ready();


        if (equippedBy!=0)
        {
            GOBasePlayerCharacter pc = (Global.gameState.GameObjects[equippedBy] as GOBasePlayerCharacter);
            pc.Pickup(this);
            pc.Equip(InventoryGroupCategory.Hands);
            rayCast = pc.rayCast;
        }

        CurrentHoldPosition = new();
        AddChild(CurrentHoldPosition);
        CurrentHoldPosition.Position = HoldPosition.Position;
    }

    public override void PerFrameShared(double delta)
    {
        if (holding != null)
        {
            if (holding is GOBaseRigidBody rb)
            {
                rb.ApplyForce((CurrentHoldPosition.GlobalPosition - rb.GlobalPosition)*50);
            }
        }
    }

    public override void HandleInput(ActionFlags input)
    {
        if (!lastTickActions.HasFlag(ActionFlags.Fire) && input.HasFlag(ActionFlags.Fire))
        {
            if (holding == null)
            {
                var col = rayCast.GetCollider();
                if (col!=null)
                {
                    if (col is IsHoldable item)
                    {
                        Logging.Log($"Hand raycast hit holdable item: {(item as Node).ToString()}", "Hands");
                        CurrentHoldPosition.Position = HoldPosition.Position;
                        holding = item;
                        holding.OnHold(equippedBy);
                        item.currentlyHeldBy = equippedBy;
                    }
                    else
                    {
                        Logging.Log($"Hand raycast hit non holdable item: {(col as Node).ToString()}", "Hands");
                    }
                }
                else
                {
                    Logging.Log($"hands raycast hit nothing", "Hands");
                }
            }
            else
            {
                holding.OnRelease(equippedBy);
                holding = null;
            }
        }

        if (input.HasFlag(ActionFlags.NextSlot))
        {

            Vector3 pos = CurrentHoldPosition.Position;
            pos.Z -= 0.3f;
            CurrentHoldPosition.Position = pos;
        }

        if (input.HasFlag(ActionFlags.PrevSlot))
        {

            Vector3 pos = CurrentHoldPosition.Position;
            pos.Z += 0.3f;
            CurrentHoldPosition.Position = pos;
        }
        lastTickActions = input;
    }

    public override void OnDropped(ulong byID)
    {
        base.OnDropped(byID);
        Logging.Log($"Hands Dropped?", "Hands");
    }

    public override void OnEquipped(ulong byID)
    {
        base.OnEquipped(byID);
        Logging.Log($"Hands Equipped", "Hands");
    }

    public override void OnPickup(ulong byID)
    {
        base.OnPickup(byID);
        Logging.Log($"Hands PickedUp?", "Hands");
    }

    public override void OnUnequipped(ulong byID)
    {
        base.OnUnequipped(byID) ;
        Logging.Log($"Hands Unequipped", "Hands");
    }

    public override bool InitFromData(GameObjectConstructorData data)
    {
        ulong objID = (ulong)data.paramList[0];
        equippedBy = objID;

        return true;
    }
}

