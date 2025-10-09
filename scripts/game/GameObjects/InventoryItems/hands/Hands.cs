using Godot;
using ImGuiGodot.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
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

    public override void _Ready()
    {
        base._Ready();
        rayCast = new();
        rayCast.TargetPosition = new Vector3(0, 0, -20);
        rayCast.CollideWithBodies = true;
        AddChild(rayCast);

        Global.gameState.PlayerCharacters[authority].Pickup(this);
        Global.gameState.PlayerCharacters[authority].Equip(this.category);
    }

    public override void PerFrameShared(double delta)
    {
        if (holding != null)
        {
            if (holding is GOBaseRigidBody rb)
            {
                rb.ApplyForce((HoldPosition.GlobalPosition - rb.GlobalPosition)*50);
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
            Vector3 pos = HoldPosition.Position;
            pos.Z -= 1 / 120;
            HoldPosition.Position = pos;
        }

        if (input.HasFlag(ActionFlags.PrevSlot))
        {
            Vector3 pos = HoldPosition.Position;
            pos.Z += 1 / 120;
            HoldPosition.Position = pos;
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

    public override bool InitFromData(GameState.GameObjectConstructorData data)
    {

        return true;
    }
}

