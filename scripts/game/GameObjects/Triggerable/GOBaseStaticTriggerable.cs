using Godot;
using Godot.Collections;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public abstract partial class GOBaseStaticTriggerable : GOBaseStaticBody, HasTriggerables
{

    [Export]
    public virtual Array<Trigger> triggerables { get; set; }
    public abstract void ActivateTriggerEffects(string triggerName, ulong byID);

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public virtual void Trigger(string triggerName,ulong byID,ulong onTick)
    {
        Trigger t = GetTrigger(triggerName);
        if (t != null)
        {
            if (t.cooldownSecondsRemaining != 0)
            {
                Logging.Warn($"Recoverable Desync! Disagreement on trigger cooldown - triggering anyway.", "GOTriggerable");
            }
            Logging.Log($"Trigger {t.triggerName} triggered!", "GOTriggerable");
            t.cooldownSecondsRemaining = t.cooldownSeconds;
            ActivateTriggerEffects(triggerName, byID);
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

    public override void PerTickShared(double delta)
    {
        foreach (Trigger t in triggerables)
        {
            if (t.cooldownSecondsRemaining == 0)
            {
                continue;
            }
            if (t.cooldownSecondsRemaining > 0)
            {
                t.cooldownSecondsRemaining -= (float)delta;
            }
            if (t.cooldownSecondsRemaining <= 0)
            {
                Logging.Log($"Trigger {t.triggerName} is off cooldown!", "GOTriggerable");
                t.cooldownSecondsRemaining = 0;
            }
        }
    }

    public override string GenerateStateString()
    {
        return $"Triggerables List: {string.Join("\n", triggerables)}";
    }

    public virtual bool UserCanTrigger(string triggerName, ulong byID)
    {
        return IsTriggerReady(triggerName);
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

    public virtual bool IsTriggerReady(string triggerName)
    {
        Trigger t = GetTrigger(triggerName);
        if (t.cooldownSecondsRemaining == 0)
        {
            return true;
        }
        return false;
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
}