using Godot;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


public abstract partial class GOBaseStaticInteractable : GOBaseStaticBody, IsInteractable
{
    public virtual ulong lastInteractTick { get; set; }
    public virtual ulong lastInteractPlayer { get; set; }

    [Export]
    public virtual float interactCooldownSeconds { get; set; }
    public virtual bool interactCooldownReady { get; set; } = true;
    public virtual float interactCooldownTimer { get; set; }
    public abstract void Auth_HandleInteractionRequest(ulong byID, ulong onTick);

    public void Local_OnInteract(ulong byID)
    {
        if (!interactCooldownReady)
        {
            Logging.Log($"Interactable {Name} ({id}) interact ignored as it is on internal cooldown! ({interactCooldownTimer} seconds remaining)", "GOInteractable");
            return;
        }
        else
        {
            if (interactCooldownSeconds > 0)
            {
                interactCooldownTimer = interactCooldownSeconds;
                interactCooldownReady = false;
            }
            Logging.Log($"Interactable {Name} ({id}) interacted with locally. Sending interact RPC over network to authority {authority}", "GOInteractable");
            RPCManager.RPC(this, MethodName.rpc_Auth_HandleInteractionRequest, [byID,Global.gameState.tick]);
        }
    }

    [RPCMethod(mode = RPCMode.OnlySendToAuth)]
    public void rpc_Auth_HandleInteractionRequest(ulong byID, ulong onTick)
    {
        Auth_HandleInteractionRequest(byID, onTick);
    }

    public override void PerTickShared(double delta)
    {
        if (interactCooldownTimer > 0)
        {
            interactCooldownTimer -= (float)delta;
        }
        if (!interactCooldownReady && interactCooldownTimer <= 0)
        {
            interactCooldownTimer = 0;
            interactCooldownReady = true;
            Logging.Log($"Interactable {Name} ({id}) has finished its internal (local) interact cooldown.", "GOInteractable");
        }
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
    public override byte[] GenerateStateUpdate()
    {
        return new byte[0];
    }

    public override void ProcessStateUpdate(byte[] _update)
    {

    }
    public override string GenerateStateString()
    {
        return $"interactCooldown: {interactCooldownTimer.ToString("0.00")}s / {interactCooldownSeconds.ToString("0.00")}s | ready?{interactCooldownReady}";
    }
}

