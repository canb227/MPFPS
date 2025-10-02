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

    private bool open = false;
    private AnimationNodeStateMachinePlayback stateMachine { get; set; }

    public override void _Ready()
    {
        stateMachine = animationTree.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>();
    }

    public override void OnInteract(ulong byID)
    {
        if (open)
        {
            stateMachine.Travel(closeAnimationStateName);
            open = false;
        }
        else
        {
            stateMachine.Travel(openAnimationStateName);
            open = true;
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

