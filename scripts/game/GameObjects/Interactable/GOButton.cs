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
    private bool internalCooldownReady = true;

    private bool waitingForCooldowns;
    private bool enabled = true;
    public override GameObjectType type { get; set; }
    public void OnInteract(ulong byID)
    {
        if (!internalCooldownReady)
        {
            Logging.Log($"Button press ignored as it is on internal cooldown! ({internalCooldownTimer} seconds remaining)", "GOButton");
            return;
        }
        else
        {
            internalCooldownTimer = interactCooldownSeconds;
            internalCooldownReady = false;
            Logging.Log($"Button {id} pressed locally. Sending press over network", "GOButton");
            GOButtonInteractRPC packet = new();
            packet.byID = byID;
            byte[] data = MessagePackSerializer.Serialize(packet);
            RPCManager.SendRPC(this, "rpc_OnInteract", data);
        }
    }

    public void rpc_OnInteract(byte[] data)
    {
        GOButtonInteractRPC packet = MessagePackSerializer.Deserialize<GOButtonInteractRPC>(data);
        Logging.Log($"Button press received on network. Button {id} interacted with by {packet.byID}", "GOButton");
        lastInteractPlayer = packet.byID;
        lastInteractTick = Global.gameState.tick;
        if (interactCooldownSeconds > 0)
        {
            internalCooldownTimer = interactCooldownSeconds;
        }
        _OnInteract(packet.byID);
    }

    private void _OnInteract(ulong byID)
    {
        if (!enabled)
        {
            PressedWhileDisabled(byID);
            return;
        }
        else if (!CanInteract(byID))
        {
            PressedFailed(byID);
            return;
        }
        else
        {
            PressedSuccessfully(byID);
        }
    }

    public virtual void PressedWhileDisabled(ulong byID)
    {
  
    }

    public virtual void PressedFailed(ulong byID)
    {

    }

    public virtual void PressedSuccessfully(ulong byID)
    {
        foreach (Triggers t in triggers)
        {
            GetNode<HasTriggerables>(t.triggerableNode).Trigger(t.triggerName, byID);
        }
    }

    public void rpc_SetEnabled(bool enabled)
    {
        GOButtonEnabledRPC packet = new();
        packet.enabled = enabled;
        byte[] data = MessagePackSerializer.Serialize(packet);
        RPCManager.SendRPC(this, "_SetEnabled", data);
    }

    public void _SetEnabled(byte[] data)
    {
        GOButtonEnabledRPC packet = MessagePackSerializer.Deserialize<GOButtonEnabledRPC>(data);
        if (enabled && !packet.enabled)
        {
            enabled = false;
            OnDisable();
        }
        else if (!enabled && packet.enabled)
        {
            enabled = true;
            OnEnable();
        }
    }


    public virtual void OnEnable()
    {
        Logging.Log($"Button {id} is now Enabled!", "GOButton");
    }
    public virtual void OnDisable()
    {
        Logging.Log($"Button {id} is now Disabled!", "GOButton");
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
                rpc_SetEnabled(true);
            }
            else if (!allReady && enabled)
            {
                rpc_SetEnabled(false);
            }

        }
        else if (ButtonDisableCondition == ButtonDisableCondition.DisableIfAllTriggersOnCooldown)
        {
            if (anyReady && !enabled)
            {
                rpc_SetEnabled(true);
            }
            else if (!anyReady && enabled)
            {
                rpc_SetEnabled(false);
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
        if (!internalCooldownReady && internalCooldownTimer <= 0)
        {
            internalCooldownTimer = 0;
            internalCooldownReady = true;
            Logging.Log($"Button {id} has finished its internal cooldown.", "GOButton");
        }
    }

    public override string GenerateStateString()
    {
        return MessagePackSerializer.ConvertToJson(GenerateStateUpdate());
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