using Godot;
using Godot.Collections;
using MessagePack;
using System;
using System.Net.Sockets;


[GlobalClass]
public partial class GOButton : GOBaseStaticBody, IsButton
{

    [Export]
    public float interactCooldownSeconds { get; set; }

    [Export]
    public Godot.Collections.Array<Triggers> triggers { get; set; }

    [Export]
    public ButtonDisableCondition ButtonDisableCondition { get; set; } = ButtonDisableCondition.DisableIfAnyTriggersOnCooldown;

    public ulong lastInteractTick { get; set; }
    public ulong lastInteractPlayer { get; set; }

    private float internalCooldownTimer = 0;
    public bool interactCooldownReady { get; set; } = true;

    private bool waitingForCooldowns;
    private bool enabled = true;
    public override GameObjectType type { get; set; }


    public void OnInteract(ulong byID)
    {
        if (!interactCooldownReady)
        {
            Logging.Log($"Button {Name} ({id}) press ignored as it is on internal cooldown! ({internalCooldownTimer} seconds remaining)", "GOButton");
            return;
        }
        else
        {
            if (interactCooldownSeconds > 0)
            {
                internalCooldownTimer = interactCooldownSeconds;
                interactCooldownReady = false;
            }
            Logging.Log($"Button {Name} ({id}) pressed locally. Sending press over network", "GOButton");
            RPCManager.RPC(this, MethodName.rpc_OnInteract, [byID]);
        }
    }

    [RPCMethod(mode = RPCMode.OnlySendToAuth)]
    public void rpc_OnInteract(ulong byID)
    {
        Logging.Log($"Button press received on network. Button {Name} ({id}) interacted with by {byID}", "GOButton");
        lastInteractPlayer = byID;
        lastInteractTick = Global.gameState.tick;
        if (!enabled)
        {
            RPCManager.RPC(this, MethodName.PressedWhileDisabled, [byID]);
            return;
        }
        else if (!CanInteract(byID))
        {
            RPCManager.RPC(this, MethodName.PressedFailed, [byID]);
            return;
        }
        else
        {
            RPCManager.RPC(this, MethodName.PressedSuccessfully, [byID]);
            foreach (Triggers t in triggers)
            {
                if (GetNode<HasTriggerables>(t.triggerableNode).IsTriggerReady(t.triggerName))
                {
                    RPCManager.RPC((GameObject)GetNode<HasTriggerables>(t.triggerableNode), "Trigger", [t.triggerName, byID]);
                }
            }
            return;
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public virtual void PressedWhileDisabled(ulong byID)
    {
  
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public virtual void PressedFailed(ulong byID)
    {

    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public virtual void PressedSuccessfully(ulong byID)
    {

    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void SetEnabled(bool _enabled)
    {
        Logging.Log($"Button enable/disable received on network. Button {Name} ({id}) -> SetEnabled({_enabled})", "GOButton");
        if (enabled && !_enabled)
        {
            enabled = false;
            OnDisable();
        }
        else if (!enabled && _enabled)
        {
            enabled = true;
            OnEnable();
        }
        else
        {
            Logging.Warn($"Recoverable Desync! Button enable state mismatch.", "GOButton");
        }
    }


    public virtual void OnEnable()
    {
        Logging.Log($"Button {Name} ({id}) is now Enabled!", "GOButton");
    }
    public virtual void OnDisable()
    {
        Logging.Log($"Button {Name} ({id}) is now Disabled!", "GOButton");
    }

    public override byte[] GenerateStateUpdate()
    {
        return new byte[0];

    }

    public override void ProcessStateUpdate(byte[] _update)
    {

    }


    public bool CanInteract(ulong byID)
    {
        if (!enabled)
        {
            return false;
        }
        foreach (Triggers t in triggers)
        {
            if (!GetNode<HasTriggerables>(t.triggerableNode).UserCanTrigger(t.triggerName, byID))
            {
                return false;
            }
        }
        return true;
    }

    public override void PerTickAuth(double delta)
    {
        bool allReady = true;
        bool anyReady = false;
        foreach (Triggers t in triggers)
        {
            HasTriggerables triggerableNode = GetNode<HasTriggerables>(t.triggerableNode);
            if (!triggerableNode.IsTriggerReady(t.triggerName))
            {
                allReady = false;
            }
            else
            {
                anyReady = true;
            }
        }
        if (ButtonDisableCondition == ButtonDisableCondition.DisableIfAnyTriggersOnCooldown)
        {
            if (allReady && !enabled)
            {
                RPCManager.RPC(this, MethodName.SetEnabled, [true]);
            }
            else if (!allReady && enabled)
            {
                RPCManager.RPC(this, MethodName.SetEnabled, [false]);
            }

        }
        else if (ButtonDisableCondition == ButtonDisableCondition.DisableIfAllTriggersOnCooldown)
        {
            if (anyReady && !enabled)
            {
                RPCManager.RPC(this, MethodName.SetEnabled, [true]);
            }
            else if (!anyReady && enabled)
            {
                RPCManager.RPC(this, MethodName.SetEnabled, [false]);
            }
        }
    }

    public override void PerFrameAuth(double delta)
    {

    }

    public override void PerTickLocal(double delta)
    {

    }

    public override void PerFrameLocal(double delta)
    {

    }
    public override void PerFrameShared(double delta)
    {

    }

    public override void PerTickShared(double delta)
    {
        if (internalCooldownTimer > 0)
        {
            internalCooldownTimer -= (float)delta;
        }
        if (!interactCooldownReady && internalCooldownTimer <= 0)
        {
            internalCooldownTimer = 0;
            interactCooldownReady = true;
            Logging.Log($"Button {id} has finished its internal cooldown.", "GOButton");
        }
    }

    public override string GenerateStateString()
    {
        return $"Enabled:{enabled}|internalCooldownTimer:{internalCooldownTimer}|numTriggers{triggers.Count}";
    }
}


[MessagePackObject]
public struct GOButtonState
{
    [Key(0)]
    public float cooldownTimer;
}

[MessagePackObject]
public struct GOButtonInteractRPC
{
    [Key(0)]
    public ulong byID;
}

[MessagePackObject]
public struct GOButtonEnabledRPC
{
    [Key(0)]
    public bool enabled;
}