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

    private const bool HoldUsesPhysics = true;

    public override InventoryGroupCategory category { get; set; } = InventoryGroupCategory.Hands; 
    public override bool droppable { get; set; } = false;
    
    public IsHoldable heldObject { get; set; }

    private ActionFlags lastTickActions;
    private RayCast3D rayCast;
    private ulong authCache = 0;
    private bool charging = false;
    private float chargeAmount = 0;

    [Export]
    Node3D HoldPosition { get; set; }

    Node3D CurrentHoldPosition { get; set; }
    public bool rotateMode = false;
    [Export] private string iconPath;

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

    public override void PerTickAuth(double delta)
    {

        if (heldObject != null)
        {
            if (heldObject is GOBaseRigidBody rb)
            {
                rb.ApplyCentralForce((CurrentHoldPosition.GlobalPosition - (rb.GlobalTransform * rb.CenterOfMass)) * 750f);

                if (charging)
                {
                    if (chargeAmount < 3)
                    {
                        chargeAmount += (float)delta * 3;
                        CurrentHoldPosition.Translate(new Vector3(0, 0, .0075f));
                    }

                }
                //rb.GlobalPosition = CurrentHoldPosition.GlobalPosition;
                //rb.GlobalRotation = CurrentHoldPosition.GlobalRotation;
            }
        }
        
    }


    public override void HandleInput(ActionFlags actionFlags)
    {
        //throw new NotImplementedException();
    }


    public void HandleHandInput(PlayerInputData input, double delta)
    {
        if (charging && lastTickActions.HasFlag(ActionFlags.Fire) && !input.actions.HasFlag(ActionFlags.Fire))
        {
            //mouse released while charging
            if(heldObject is RigidBody3D rb2)
            {
                var impulse = new Vector3(chargeAmount * 20 * (rb2.Mass * 0.5f), chargeAmount * 20 * (rb2.Mass * 0.5f), chargeAmount * 20 * (rb2.Mass * 0.5f));
                var vectoredImpulse = (CurrentHoldPosition.GlobalPosition - HoldPosition.GlobalPosition) * -impulse;
                rb2.ApplyCentralImpulse(vectoredImpulse);
                RPCManager.RPCID(id, "ReleaseHeld", []);
            }
            charging = false;
            chargeAmount = 0;
        }
        else if (!lastTickActions.HasFlag(ActionFlags.Fire) && input.actions.HasFlag(ActionFlags.Fire))
        {
            if (heldObject == null)
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
                                    RPCManager.RPCID(id, "Hold", [obj.id]);
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
                charging = true;
            }
        }

        if (input.actions.HasFlag(ActionFlags.Aim))
            rotateMode = true;
        else
            rotateMode = false;

        float mouseX = input.LookInputVector.X * 5f * (float)delta;
        float mouseY = input.LookInputVector.Y * 5f * (float)delta;

        if (rotateMode && heldObject is GOBaseRigidBody rb)
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
        Logging.Log($"{equippedBySteamID} has started holding something", "Hands");
        var obj = Global.gameState.GameObjects[itemID];
        if (obj is IsHoldable item)
        {
            CurrentHoldPosition.Position = HoldPosition.Position;
            heldObject = item;
            heldObject.OnHold(equippedBySteamID);
            item.currentlyHeldBy = equippedBySteamID;
            (heldObject as RigidBody3D).AddCollisionExceptionWith(Global.gameState.GetCharacterControlledBy(equippedBySteamID));
            if (NetworkUtils.IsMe(equippedBySteamID))
            {
                (heldObject as Node3D).SetPhysicsProcess(true);
            }
            
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void ReleaseHeld()
    {
        if(heldObject != null)
        {
            (heldObject as GameObject).priority = 10;
            (heldObject as RigidBody3D).RemoveCollisionExceptionWith(Global.gameState.GetCharacterControlledBy(equippedBySteamID));
            heldObject.OnRelease(equippedBySteamID);
            heldObject.currentlyHeldBy = 0;
            heldObject = null;
        }
    }

    //[RPCMethod(mode = RPCMode.SendToAllPeers)]
    //public void ApplyRotation(ulong rbID, Vector3 torque)
    //{
    //    var obj = Global.gameState.GameObjects[rbID];
    //    if (obj is GOBaseRigidBody rb)
    //    {
    //        rb.ApplyTorque(torque);
    //    }
    //}

    public override void OnDropped(ulong byID)
    {
        if (inInventoryOf == Global.steamid)
        {
            Global.ui.inGameUI.PlayerUIManager.UpdateInventorySlot(1, "");
        }        
        base.OnDropped(byID);
        if(heldObject != null)
        {
            heldObject.OnRelease(equippedBySteamID);
            heldObject = null; 
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
        if (inInventoryOf == Global.steamid)
        {
            Global.ui.inGameUI.PlayerUIManager.UpdateInventorySlot(1, iconPath);
        }
        base.OnPickup(byID);
        Logging.Log($"Hands PickedUp?", "Hands");
    }

    public override void OnUnequipped(ulong byID)
    {
        base.OnUnequipped(byID);
        RPCManager.RPCID(id, "ReleaseHeld", []);
        Logging.Log($"Hands Unequipped", "Hands");
    }

    public override bool InitFromData(GameObjectConstructorData data)
    {
        ulong objID = (ulong)data.paramList[0];
        equippedBySteamID = objID;

        return true;
    }
}

