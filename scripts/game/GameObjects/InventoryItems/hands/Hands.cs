using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class Hands : Node3D, IsInventoryItem
{

    [Export]
    public Node3D firstPersonScene { get; set; }

    [Export]
    public Node3D thirdPersonScene { get; set; }

    [Export]
    public AnimationPlayer animation {  get; set; }

    public bool droppable { get; set; } = false;
    public InventoryGroupCategory category { get; set; } = InventoryGroupCategory.Hands;

    public ulong inInventoryOf { get; set; }
    public ulong equippedBy { get; set; }

    public IsHoldable held {  get; set; }

    private RayCast3D rayCast { get; set; }


    public void HandleInput(ActionFlags actionFlags)
    {
        if( actionFlags.HasFlag(ActionFlags.Use))
        {
            if (held != null)
            {

            }
            else
            {

            }
        }

        if (actionFlags.HasFlag(ActionFlags.Fire))
        {
            if (held != null)
            {
                ThrowHeldItem();
            }
            else
            {
                AttemptPickup();
            }
        }

        if (actionFlags.HasFlag(ActionFlags.Aim))
        {
            if (held != null)
            {

            }
            else
            {

            }
        }
    }

    private void AttemptPickup()
    {
        if (animation.HasAnimation("attempt_pickup"))
        {
            animation.Play("attempt_pickup");
        }

        if (rayCast.IsColliding())
        {
            if (rayCast.GetCollider() is IsHoldable h)
            {
                Logging.Log($"Hands found a holdable item! Picking it up!", "Hands");
                PickupItem(h);
                h.OnHold(equippedBy);
            }
            else
            {
                Logging.Log($"Hands found a collision, but it is not Holdable", "Hands");
            }
        }
        Logging.Log($"Hands found no collision.", "Hands");
    }

    private void PickupItem(IsHoldable h)
    {
        Logging.Warn($"Hands item pickup not implemented :(", "Hands");
    }

    private void ThrowHeldItem()
    {
        if (held!= null)
        {
            if (animation.HasAnimation("throw"))
            {
                animation.Play("throw");
            }
        }
    }

    public void OnDropped(ulong byID)
    {
        if (!droppable)
        {
            Logging.Warn($"This item can't be dropped", "Hands");
        }
    }

    public void OnEquipped(ulong byID)
    {
        animation.Play("equipped");
        rayCast = new();
        rayCast.TargetPosition = new Vector3(0, 0, -10);
        rayCast.CollideWithBodies = true;
        AddChild(rayCast);
    }

    public List<ActionFlags> GetHandledInputActions()
    {
        return new List<ActionFlags>() { ActionFlags.Fire, ActionFlags.Aim, ActionFlags.Use };
    }

    public void OnPickup(ulong byID)
    {
        Logging.Warn($"This item can't be picked up", "Hands");
    }

    public void OnUnequipped(ulong byID)
    {
        rayCast.QueueFree();
    }

}

