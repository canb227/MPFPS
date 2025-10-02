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
        stateMachine = animationTree.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>();
    }

    public override void OnInteract(ulong byID)
    {
        base.OnInteract(byID);
        if (openingOrOpen)
        {
            stateMachine.Travel(closeAnimationStateName);
            openingOrOpen = false;
        }
        else
        {
            stateMachine.Travel(openAnimationStateName);
            openingOrOpen = true;
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

