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

    public override void ActivateTriggerEffects(string triggerName, ulong byID)
    {
        if (!animationPlayer.HasAnimation(triggerName))
        {
            Logging.Error($"The AnimationPlayer of {Name} ({id}) is missing an animation that matches the triggerName: {triggerName}!", "GOTrap");
            return;
        }
        else
        {
            animationPlayer.Play(triggerName);
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
           return new byte[0];
    }

    public override void PerFrameShared(double delta)
    {

    }

    public override void PerTickAuth(double delta)
    {

    }
}
