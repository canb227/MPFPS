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

    protected bool internalCooldownReady = true;
    protected float internalCooldownTimer;

    public abstract bool CanInteract(ulong byID);

    public virtual void OnInteract(ulong byID)
    {
        if (!internalCooldownReady)
        {
            Logging.Log($"Door interact ignored as it is on internal cooldown! ({internalCooldownTimer} seconds remaining)", "GODoor");
            return;
        }
        else
        {
            internalCooldownTimer = interactCooldownSeconds;
            internalCooldownReady = false;
        }
    }


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

