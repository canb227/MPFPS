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
    private ulong authCache = 0;

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
                        if(item is GameObject obj)
                        {
                            if (obj is IsHoldable ih)
                            {
                                if (ih.currentlyHeldBy == 0)
                                {
                                    RPCManager.RPC(this, "Hold", [obj.id]);
                                }
                                else
                                {
                                    Logging.Warn($"Cannot hold item, it is already held by: {ih.currentlyHeldBy}", "Hands");
                                }
                            }

                        }
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
                RPCManager.RPC(this, "ReleaseHeld", []);
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
            //RPCManager.RPC(this, "ApplyRotation", [rb.id, torque]);
        }
        lastTickActions = input.actions;
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void Hold(ulong itemID)
    {
        var obj = Global.gameState.GameObjects[itemID];
        if (obj is IsHoldable item)
        {
            authCache = obj.authority;
            //obj.authority = equippedBySteamID;
            CurrentHoldPosition.Position = HoldPosition.Position;
            holding = item;
            holding.OnHold(equippedBySteamID);
            item.currentlyHeldBy = equippedBySteamID;
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void ReleaseHeld()
    {
        //(holding as GameObject).authority = authCache;
        authCache = 0;
        holding.OnRelease(equippedBySteamID);
        holding.currentlyHeldBy = 0;
        holding = null;
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void ApplyRotation(ulong rbID, Vector3 torque)
    {
        var obj = Global.gameState.GameObjects[rbID];
        if (obj is GOBaseRigidBody rb)
        {
            rb.ApplyTorque(torque);
        }
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

