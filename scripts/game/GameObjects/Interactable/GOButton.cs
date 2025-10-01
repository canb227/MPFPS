using Godot;
using Godot.Collections;
using MessagePack;
using System;
using System.Net.Sockets;


[GlobalClass]
public partial class GOButton : GOBaseStaticBody, IsButton
{

    [Export]
    public ulong interactCooldownSeconds { get; set; }

    [Export]
    public Godot.Collections.Array<Triggers> triggers { get; set; }

    [Export]
    public ButtonCooldownSetting ButtonCooldownSetting { get; set; }

    public ulong lastInteractTick { get; set; }
    public ulong lastInteractPlayer { get; set; }

    private float cooldownTimer = 0;
    private bool waitingForCooldowns;
    private bool ready = true;
    public override GameObjectType type { get; set; }
    public void OnInteract(ulong byID)
    {
        Logging.Log($"Sending Interaction request over network", "GOInteractable");
        GOButtonRPC packet = new();
        packet.byID = byID;
        byte[] data = MessagePackSerializer.Serialize(packet);
        RPCManager.SendRPC(this, "rpc_OnInteract", data);
    }

    public void rpc_OnInteract(byte[] data)
    {
        GOButtonRPC packet = MessagePackSerializer.Deserialize<GOButtonRPC>(data);
        Logging.Log($"Interaction request received from network: Object {id} interacted with by {packet.byID}", "GOInteractable");
        _OnInteract(packet.byID);
    }

    private void _OnInteract(ulong byID)
    {
        if (ButtonCooldownSetting == ButtonCooldownSetting.DisableOnlyIfSelfOnCooldown)
        {
            if (!ready)
            {
                PressedWhileOnCooldown(byID);
                return;
            }
            else
            {
                PressedSuccessfully(byID);
                return;
            }
        }
        else if (ButtonCooldownSetting == ButtonCooldownSetting.DisableIfSelfOrAnyTriggersOnCooldown)
        {
            if (!ready)
            {
                PressedWhileOnCooldown(byID);
                return;
            }
            bool allReady = true;
            foreach (Triggers t in triggers)
            {
                HasTriggerables triggerableNode = GetNode<HasTriggerables>(t.triggerableNode);
                if (!triggerableNode.CanTrigger(t.triggerName, byID))
                {
                    allReady = false;
                }
            }
            if (allReady)
            {
                PressedSuccessfully(byID);
                return;
            }
            else
            {
                PressedFailed(byID);
                return;
            }
        }
        else if (ButtonCooldownSetting == ButtonCooldownSetting.DisableIfSelfOrAllTriggersOnCooldown)
        {
            if (!ready)
            {
                PressedWhileOnCooldown(byID);
                return;
            }
            bool atLeastOneReady = false;
            foreach (Triggers t in triggers)
            {
                HasTriggerables triggerableNode = GetNode<HasTriggerables>(t.triggerableNode);
                if (triggerableNode.CanTrigger(t.triggerName, byID))
                {
                    atLeastOneReady = true;
                    break;
                }
            }
            if (atLeastOneReady)
            {
                PressedSuccessfully(byID);
                return;
            }
            else
            {
                PressedFailed(byID);
                return;
            }
        }
    }

    public virtual void PressedWhileOnCooldown(ulong byID)
    {
        lastInteractPlayer = byID;
        lastInteractTick = Global.gameState.tick;
    }

    public virtual void PressedFailed(ulong byID)
    {
        lastInteractPlayer = byID;
        lastInteractTick = Global.gameState.tick;
        if (interactCooldownSeconds > 0)
        {
            cooldownTimer = interactCooldownSeconds;
            ready = false;
        }
    }
    public virtual void PressedSuccessfully(ulong byID)
    {
        foreach (Triggers t in triggers)
        {
            if (GetNode<HasTriggerables>(t.triggerableNode).CanTrigger(t.triggerName, byID))
            {
                GetNode<HasTriggerables>(t.triggerableNode).Trigger(t.triggerName, byID);
            }
        }
        lastInteractPlayer = byID;
        lastInteractTick = Global.gameState.tick;
        if (interactCooldownSeconds>0)
        {
            cooldownTimer = interactCooldownSeconds;
            ready = false;
        }
    }

    public override byte[] GenerateStateUpdate()
    {
        GOButtonState update = new();
        update.cooldownTimer = cooldownTimer;
        return MessagePackSerializer.Serialize(update);
    }

    public override void ProcessStateUpdate(byte[] _update)
    {
        GOButtonState update = MessagePackSerializer.Deserialize<GOButtonState>(_update);

        if(cooldownTimer==0 && update.cooldownTimer!=0)
        {
            Logging.Error($"Sync error, interactable cooldown", "GOInteractable");
        }
        //cooldownTimer = update.cooldownTimer;
    }
    public virtual void OnEnable()
    {
        Logging.Log($"Interactable is now Enabled!", "GOInteractable");

    }
    public bool CanInteract(ulong byID)
    {
        if (ButtonCooldownSetting == ButtonCooldownSetting.DisableOnlyIfSelfOnCooldown)
        {
            if (!ready)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        else if (ButtonCooldownSetting == ButtonCooldownSetting.DisableIfSelfOrAnyTriggersOnCooldown)
        {
            if (!ready)
            {
                return false;
            }
            bool allReady = true;
            foreach (Triggers t in triggers)
            {
                HasTriggerables triggerableNode = GetNode<HasTriggerables>(t.triggerableNode);
                if (!triggerableNode.CanTrigger(t.triggerName, byID))
                {
                    allReady = false;
                }
            }
            if (allReady)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else if (ButtonCooldownSetting == ButtonCooldownSetting.DisableIfSelfOrAllTriggersOnCooldown)
        {
            if (!ready)
            {
                return false;
            }
            bool atLeastOneReady = false;
            foreach (Triggers t in triggers)
            {
                HasTriggerables triggerableNode = GetNode<HasTriggerables>(t.triggerableNode);
                if (triggerableNode.CanTrigger(t.triggerName, byID))
                {
                    atLeastOneReady = true;
                    break;
                }
            }
            if (atLeastOneReady)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
        
    }
    public override void PerTickAuth(double delta)
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= (float)delta;
        }
        if (!ready && cooldownTimer <= 0)
        {
            ready = true;
            cooldownTimer = 0;
            waitingForCooldowns = true;
            Logging.Log($"Interactable {id} has finished its cooldown.", "GOInteractable");
        }
        if (waitingForCooldowns)
        {
            if (ButtonCooldownSetting == ButtonCooldownSetting.DisableOnlyIfSelfOnCooldown)
            {
                OnEnable();
                waitingForCooldowns = false;
            }
            else if (ButtonCooldownSetting == ButtonCooldownSetting.DisableIfSelfOrAllTriggersOnCooldown)
            {
                foreach (Triggers t in triggers)
                {
                    HasTriggerables triggerableNode = GetNode<HasTriggerables>(t.triggerableNode);
                    if (triggerableNode.GetTriggerCooldown(t.triggerName, 0) == 0)
                    {
                        OnEnable();
                        waitingForCooldowns = false;
                        break;
                    }
                }
            }
            else if (ButtonCooldownSetting == ButtonCooldownSetting.DisableIfSelfOrAnyTriggersOnCooldown)
            {
                bool allReady = true;
                foreach (Triggers t in triggers)
                {
                    HasTriggerables triggerableNode = GetNode<HasTriggerables>(t.triggerableNode);
                    if (triggerableNode.GetTriggerCooldown(t.triggerName, 0) != 0)
                    {
                        allReady = false;
                    }
                }
                if (allReady)
                {
                    OnEnable();
                    waitingForCooldowns = false;
                }
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
public struct GOButtonRPC
{
    [Key(0)]
    public ulong byID;
}