using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public partial class GOOneWayAnimTreeDoor : GODoor
{
    [Export]
    public AnimationTree animationTree {  get; set; }

    [Export]
    public string openAnimationStateName { get; set; } = "opened";

    [Export]
    public string closeAnimationStateName { get; set; } = "closed";

    private bool openingOrOpen = false;
    private AnimationNodeStateMachinePlayback stateMachine { get; set; }

    public override void _Ready()
    {
        if (animationTree == null)
        {
            Logging.Error($"Door {Name} ({id}) could not load its Animation Tree! Check object properties.", "GODoor");
        }

        stateMachine = animationTree.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>();
        if (stateMachine == null)
        {
            Logging.Error($"Door {Name} ({id}) could not load its Animation State machine! Check animation tree configuration.", "GODoor");
        }
    }

    public override void ActivateDoor(ulong byID)
    {
        if (openingOrOpen)
        {
            if (animationTree.HasNode(closeAnimationStateName))
            {
                stateMachine.Travel(closeAnimationStateName);
                openingOrOpen = false;
            }
            else
            {
                Logging.Error($"The AnimationTree State machine of {Name} ({id}) is missing a node that matches the requested state: {closeAnimationStateName}!", "GODoor");
            }

        }
        else
        {
            if (animationTree.HasNode(openAnimationStateName))
            {
                stateMachine.Travel(openAnimationStateName);
                openingOrOpen = true;
            }
            else
            {
                Logging.Error($"The AnimationTree State machine of {Name} ({id}) is missing a node that matches the requested state: {closeAnimationStateName}!", "GODoor");
            }
        }
    }

    public override bool CanInteract(ulong byID)
    {
        return true;
    }

    public override byte[] GenerateStateUpdate()
    {
        return new byte[0];
    }

    public override void ProcessStateUpdate(byte[] update)
    {

    }


}

