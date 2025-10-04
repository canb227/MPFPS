using Godot;
using Godot.Collections;
using MessagePack;
using System;
using System.Net.Sockets;


[GlobalClass]
public partial class GOButton : GOBaseStaticInteractable, IsButton
{

    [Export]
    public virtual Godot.Collections.Array<Triggers> triggers { get; set; }

    [Export]
    public virtual ButtonDisableCondition ButtonDisableCondition { get; set; } = ButtonDisableCondition.DisableIfAnyTriggersOnCooldown;

    public virtual bool enabled { get; set; } = true;
    public override GameObjectType type { get; set; }

    public override void Auth_HandleInteractionRequest(ulong byID, ulong onTick)
    {
        Logging.Log($"Button press received on network. Button {Name} ({id}) interacted with by {byID}", "GOButton");
        if (!enabled)
        {
            RPCManager.RPC(this, MethodName.PressedWhileDisabled, [byID,onTick]);
            return;
        }
        else if (!PlayerHasPermissions(byID))
        {
            RPCManager.RPC(this, MethodName.PressedFailed, [byID, onTick]);
            return;
        }
        else
        {
            RPCManager.RPC(this, MethodName.PressedSuccessfully, [byID, onTick]);
            foreach (Triggers t in triggers)
            {
                if (GetNode<HasTriggerables>(t.triggerableNode).IsTriggerReady(t.triggerName))
                {
                    RPCManager.RPC((GameObject)GetNode<HasTriggerables>(t.triggerableNode), "Trigger", [t.triggerName, byID, onTick]);
                }
            }
            return;
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public virtual void PressedWhileDisabled(ulong byID, ulong onTick)
    {
  
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public virtual void PressedFailed(ulong byID, ulong onTick)
    {

    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public virtual void PressedSuccessfully(ulong byID, ulong onTick)
    {

    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public virtual void OnEnable(ulong onTick)
    {

    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public virtual void OnDisable(ulong onTick)
    {

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
                enabled = true;
                RPCManager.RPC(this, MethodName.OnEnable, [Global.gameState.tick]);
            }
            else if (!allReady && enabled)
            {
                enabled = false;
                RPCManager.RPC(this, MethodName.OnDisable, [Global.gameState.tick]);
            }

        }
        else if (ButtonDisableCondition == ButtonDisableCondition.DisableIfAllTriggersOnCooldown)
        {
            if (anyReady && !enabled)
            {
                enabled = true;
                RPCManager.RPC(this, MethodName.OnEnable, [Global.gameState.tick]);
            }
            else if (!anyReady && enabled)
            {
                enabled = false;
                RPCManager.RPC(this, MethodName.OnDisable, [Global.gameState.tick]);
            }
        }
    
    }

    public virtual bool PlayerHasPermissions(ulong byID)
    {
        return true;
    }

    public override string GenerateStateString()
    {
        return base.GenerateStateString() + $" | Enabled:{enabled} | Triggers:{string.Join(",",triggers)}";
    }

}