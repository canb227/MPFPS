using Godot;
using Godot.Collections;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public abstract partial class GOTriggerable : GOBaseStaticBody, HasTriggerables
{

    [Export]
    public virtual Array<Trigger> triggerables { get; set; }
    public abstract void ActivateTriggerEffects(string triggerName, ulong byID);


    public virtual void rpc_Trigger(byte[] data)
    {
        TriggerRPCPacket packet = MessagePackSerializer.Deserialize<TriggerRPCPacket>(data);
        _Trigger(packet.triggerName, packet.byID);
    }

    public virtual void Trigger(string triggerName, ulong byID)
    {
       TriggerRPCPacket packet = new(triggerName,byID);
       byte[] data = MessagePackSerializer.Serialize(packet);
       RPCManager.SendRPC(this,"rpc_Trigger",data);
    }

    protected virtual void _Trigger(string triggerName, ulong byID)
    {
        Trigger t = GetTrigger(triggerName);
        if (t!=null)
        {
            if (t.cooldownSecondsRemaining == 0)
            {
                Logging.Log($"Trigger {t.triggerName} triggered!", "GOTriggerable");
                t.cooldownSecondsRemaining = t.cooldownSeconds;
                ActivateTriggerEffects(triggerName,byID);
            }
        }
    }

    public virtual Trigger GetTrigger(string triggerName)
    {
        foreach (Trigger t in triggerables)
        {
            if (t.triggerName == triggerName)
            {
                 return t;
            }
        }
        return null;
    }

    public override string GenerateStateString()
    {
        return MessagePackSerializer.ConvertToJson(GenerateStateUpdate());
    }

    public virtual bool CanTrigger(string triggerName, ulong byID)
    {
        Trigger t = GetTrigger(triggerName);
        if (t.cooldownSecondsRemaining == 0)
        {
            return true;
        }
        return false;
    }

    public virtual float GetTriggerCooldown(string triggerName, ulong byID)
    {
        Trigger t = GetTrigger(triggerName);
        if (t != null)
        {
            return t.cooldownSecondsRemaining;
        }
        return 0;
    }
}

[MessagePackObject]
public struct TriggerRPCPacket
{
    [Key(0)]
    public ulong byID;

    [Key(1)]
    public string triggerName;

    public TriggerRPCPacket(string triggerName, ulong byID)
    { 
        this.triggerName = triggerName;
        this.byID = byID;
    }
}