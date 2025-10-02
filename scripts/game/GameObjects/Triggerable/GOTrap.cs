using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class GOTrap : GOTriggerable
{
    [Export]
    public AnimationPlayer animationPlayer { get; set; }

    [Export]
    public Godot.Collections.Array<GOHurtbox> hurtboxList { get; set; }

    public override void ActivateTriggerEffects(string triggerName, ulong byID)
    {
        Logging.Log($"Trigger {triggerName} is now on a {GetTriggerCooldown(triggerName,byID)} cooldown!", "GOTriggerable");
        if (!animationPlayer.HasAnimation(triggerName))
        {
            return;
        }
        else
        {
            animationPlayer.Play(triggerName);
        }
    }


    public override void PerTickAuth(double delta)
    {
        foreach (Trigger t in triggerables)
        {
            if (t.cooldownSecondsRemaining==0)
            {
                return;
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

        foreach (GOHurtbox t in hurtboxList)
        {
            t.DoDamage();
        }
    }

    public override void PerFrameAuth(double delta)
    {

    }

    public override void PerFrameLocal(double delta)
    {

    }

    public override void PerTickLocal(double delta)
    {

    }

    public override void ProcessStateUpdate(byte[] update)
    {

    }
    public override byte[] GenerateStateUpdate()
    {
           return new byte[1];
    }

    public override void PerFrameShared(double delta)
    {

    }

    public override void PerTickShared(double delta)
    {

    }
}
