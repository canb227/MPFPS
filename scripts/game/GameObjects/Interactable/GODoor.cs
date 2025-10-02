using Godot;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public abstract partial class GODoor : GOBaseStaticBody, IsInteractable
{
    [Export]
    public virtual float interactCooldownSeconds { get; set; }
    public virtual ulong lastInteractTick { get; set; }
    public virtual ulong lastInteractPlayer { get; set; }
    public bool interactCooldownReady { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    protected bool internalCooldownReady = true;
    protected float internalCooldownTimer;

    public abstract bool CanInteract(ulong byID);

    public virtual void OnInteract(ulong byID)
    {
        if (!internalCooldownReady)
        {
            Logging.Log($"Door {Name} ({id}) press ignored as it is on internal cooldown! ({internalCooldownTimer} seconds remaining)", "GODoor");
            return;
        }
        else
        {
            if (interactCooldownSeconds > 0)
            {
                internalCooldownTimer = interactCooldownSeconds;
                internalCooldownReady = false;
            }
            Logging.Log($"Door {Name} ({id}) pressed locally. Sending press over network", "GODoor");
            RPCManager.RPC(this, "rpc_OnInteract", [byID]);
        }
    }

    [RPCMethod(mode = RPCMode.OnlySendToAuth)]
    public virtual void rpc_OnInteract(ulong byID)
    {
        Logging.Log($"Door press received on network. Door {Name} ({id}) interacted with by {byID}", "GODoor");
        lastInteractPlayer = byID;
        lastInteractTick = Global.gameState.tick;
        if (interactCooldownSeconds > 0)
        {
            internalCooldownTimer = interactCooldownSeconds;
            internalCooldownReady = false;
        }
        RPCManager.RPC(this,MethodName.ActivateDoor, [byID]);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public abstract void ActivateDoor(ulong byID);

    public override string GenerateStateString()
    {
        return MessagePackSerializer.ConvertToJson(GenerateStateUpdate());
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
        if (internalCooldownTimer > 0)
        {
            internalCooldownTimer -= (float)delta;
        }
        if (!internalCooldownReady && internalCooldownTimer <= 0)
        {
            internalCooldownTimer = 0;
            internalCooldownReady = true;
            Logging.Log($"Door {id} has finished its internal cooldown.", "GODoor");
        }
    }


}

[MessagePackObject]
public struct GODoorInteractRPC
{
    [Key(0)]
    public ulong byID;
}