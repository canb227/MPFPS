using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class GOTrapAnimTree : GOBaseStaticTriggerable
{
    [Export]
    public AnimationTree animationTree { get; set; }

    private AnimationNodeStateMachinePlayback stateMachine { get; set; }


    public override void _Ready()
    {

        if (animationTree == null)
        {
            Logging.Error($"Trap {Name} ({id}) could not load its Animation Tree! Check object properties.", "GOTrap");
        }

        stateMachine = animationTree.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>();
        if (stateMachine == null)
        {
            Logging.Error($"Trap {Name} ({id}) could not load its Animation State machine! Check animation tree configuration.", "GOTrap");
        }
    }

    public override void ActivateTriggerEffects(string triggerName, ulong byID)
    {
        if (animationTree.HasNode(triggerName))
        {
            stateMachine.Travel(triggerName);
        }
        else
        {
            Logging.Error($"The AnimationTree State machine of {Name} ({id}) is missing a node that matches the triggerName: {triggerName}!", "GOTrap");
        }
    }
}
