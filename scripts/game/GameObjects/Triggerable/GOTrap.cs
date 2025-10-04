using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class GOTrap : GOBaseStaticTriggerable
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

}
