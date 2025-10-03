using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class GOAnimatedButton : GOButton
{

    [Export]
    public AnimationTree animationTree { get; set; }

    [Export]
    public string SuccessfulPressAnimationStateName { get; set; } = "success";

    [Export]
    public string FailedPressAnimationStateName { get; set; } = "failed";

    [Export]
    public string PressedWhileDisabledAnimationStateName { get; set; } = "disabled";

    [Export]
    public string DisableAnimationStateName { get; set; } = "disable";

    [Export]
    public string EnableAnimationStateName { get; set; } = "enable";

    private AnimationNodeStateMachinePlayback stateMachine {  get; set; }

    public override void _Ready()
    {
        base._Ready();
        stateMachine = animationTree.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>();
    }

    public override void PressedFailed(ulong byID)
    {
        base.PressedFailed(byID);
        AnimationNodeStateMachine stateMachineNode = (AnimationNodeStateMachine)animationTree.TreeRoot;
        if (stateMachineNode.HasNode(FailedPressAnimationStateName))
        {
            stateMachine.Travel(FailedPressAnimationStateName);
        }
        else
        {
            Logging.Error($"The AnimationTree State machine of {Name} ({id}) is missing a node that matches the request state: {FailedPressAnimationStateName}", "GOButton");
        }
    }

    public override void PressedSuccessfully(ulong byID)
    {
        base.PressedSuccessfully(byID);
        AnimationNodeStateMachine stateMachineNode = (AnimationNodeStateMachine)animationTree.TreeRoot;
        if (stateMachineNode.HasNode(SuccessfulPressAnimationStateName))
        {
            stateMachine.Travel(SuccessfulPressAnimationStateName);
        }
        else
        {
            Logging.Error($"The AnimationTree State machine of {Name} ({id}) is missing a node that matches the request state: {SuccessfulPressAnimationStateName}!", "GOButton");
        }
    }

    public override void PressedWhileDisabled(ulong byID)
    {
        base.PressedWhileDisabled(byID);
        AnimationNodeStateMachine stateMachineNode = (AnimationNodeStateMachine)animationTree.TreeRoot;
        if (stateMachineNode.HasNode(PressedWhileDisabledAnimationStateName))
        {
            stateMachine.Travel(PressedWhileDisabledAnimationStateName);
        }
        else
        {
            Logging.Error($"The AnimationTree State machine of {Name} ({id}) is missing a node that matches the request state: {PressedWhileDisabledAnimationStateName}!", "GOButton");
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        AnimationNodeStateMachine stateMachineNode = (AnimationNodeStateMachine)animationTree.TreeRoot;
        if (stateMachineNode.HasNode(EnableAnimationStateName))
        {
            stateMachine.Travel(EnableAnimationStateName);
        }
        else
        {
            Logging.Error($"The AnimationTree State machine of {Name} ({id}) is missing a node that matches the request state: {EnableAnimationStateName}!", "GOButton");
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();
        AnimationNodeStateMachine stateMachineNode = (AnimationNodeStateMachine)animationTree.TreeRoot;
        if (stateMachineNode.HasNode(DisableAnimationStateName))
        {
            stateMachine.Travel(DisableAnimationStateName);
        }
        else
        {
            Logging.Error($"The AnimationTree State machine of {Name} ({id}) is missing a node that matches the request state: {DisableAnimationStateName}!", "GOButton");
        }
    }



}

