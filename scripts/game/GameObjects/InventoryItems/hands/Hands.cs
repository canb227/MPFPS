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
    public bool rotateMode = false;

    public override void _Ready()
    {
        base._Ready();


        if (equippedBySteamID!=0)
        {
            GOBasePlayerCharacter pc = Global.gameState.GameObjects[equippedBySteamID] as GOBasePlayerCharacter;
            pc.Pickup(this);
            pc.Equip(InventoryGroupCategory.Hands);
            rayCast = pc.interactRayCast;
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
                rb.ApplyCentralForce((CurrentHoldPosition.GlobalPosition - (rb.GlobalTransform * rb.CenterOfMass)) * 50f);
            }
        }
    }

    public override void HandleInput(ActionFlags actionFlags)
    {
        throw new NotImplementedException();
    }


    public void HandleHandInput(PlayerInputData input, double delta)
    {
        if (!lastTickActions.HasFlag(ActionFlags.Fire) && input.actions.HasFlag(ActionFlags.Fire))
        {
            if (holding == null)
            {
                var col = rayCast.GetCollider();
                if (col != null)
                {
                    if (col is IsHoldable item)
                    {
                        Logging.Log($"Hand raycast hit holdable item: {(item as Node).ToString()}", "Hands");
                        CurrentHoldPosition.Position = HoldPosition.Position;
                        holding = item;
                        holding.OnHold(equippedBySteamID);
                        item.currentlyHeldBy = equippedBySteamID;
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
                holding.OnRelease(equippedBySteamID);
                holding = null;
            }
        }

        if (input.actions.HasFlag(ActionFlags.Aim))
            rotateMode = true;
        else
            rotateMode = false;

        float mouseX = input.LookInputVector.X * 5f * (float)delta;
        float mouseY = input.LookInputVector.Y * 5f * (float)delta;

        if (rotateMode && holding is GOBaseRigidBody rb)
        {
            // Apply torque based on mouse movement
            float torqueStrength = 5f; // tweak sensitivity

            // Mouse X → yaw around world up
            // Mouse Y → pitch around local X
            Vector3 torque = new Vector3(
                mouseY * torqueStrength,
                mouseX * torqueStrength,
                0
            );

            rb.ApplyTorque(torque);
        }

        if (input.actions.HasFlag(ActionFlags.NextSlot))
        {

            Vector3 pos = CurrentHoldPosition.Position;
            pos.Z -= 0.3f;
            CurrentHoldPosition.Position = pos;
        }

        if (input.actions.HasFlag(ActionFlags.PrevSlot))
        {

            Vector3 pos = CurrentHoldPosition.Position;
            pos.Z += 0.3f;
            CurrentHoldPosition.Position = pos;
        }
        lastTickActions = input.actions;
    }

    public override void OnDropped(ulong byID)
    {
        base.OnDropped(byID);
        if(holding != null)
        {
            holding.OnRelease(equippedBySteamID);
            holding = null; 
        }
        Logging.Warn($"Hands Dropped?", "Hands");
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
        base.OnUnequipped(byID);
        if(holding != null)
        {
            holding.OnRelease(equippedBySteamID);
            holding = null; 
        }
        Logging.Log($"Hands Unequipped", "Hands");
    }

    public override bool InitFromData(GameObjectConstructorData data)
    {
        ulong objID = (ulong)data.paramList[0];
        equippedBySteamID = objID;

        return true;
    }
}

