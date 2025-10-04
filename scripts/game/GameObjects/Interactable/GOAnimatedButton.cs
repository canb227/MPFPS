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
    [ExportCategory("Animations")]
    [Export]
    public string SuccessfulPressAnimationStateName { get; set; } = "success";
    [Export]
    public string FailedPressAnimationStateName { get; set; } = "failed";
    [Export]
    public string DisabledPressAnimationStateName { get; set; } = "disabled";
    [Export]
    public string DisableAnimationStateName { get; set; } = "disable";
    [Export]
    public string EnableAnimationStateName { get; set; } = "enable";


    private AnimationNodeStateMachinePlayback stateMachine {  get; set; }

    public override void _Ready()
    {
        base._Ready();
        if (animationTree == null)
        {
            Logging.Error($"Button {Name} ({id}) could not load its Animation Tree! Check object properties.", "GOButton");
        }
        stateMachine = animationTree.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>();
        if (stateMachine == null)
        {
            Logging.Error($"Button {Name} ({id}) could not load its Animation State machine! Check animation tree configuration.", "GOButton");
        }
    }

    public override void PressedFailed(ulong byID, ulong onTick)
    {
        base.PressedFailed(byID, onTick);
        if (animationTree != null)
        {
            AnimationNodeStateMachine stateMachineNode = (AnimationNodeStateMachine)animationTree.TreeRoot;
            if (stateMachineNode.HasNode(FailedPressAnimationStateName))
            {
                stateMachine.Travel(FailedPressAnimationStateName);
            }
        }

    }

    public override void PressedSuccessfully(ulong byID, ulong onTick)
    {
        base.PressedSuccessfully(byID, onTick);
        if (animationTree != null)
        {
            AnimationNodeStateMachine stateMachineNode = (AnimationNodeStateMachine)animationTree.TreeRoot;
            if (stateMachineNode.HasNode(SuccessfulPressAnimationStateName))
            {
                stateMachine.Travel(SuccessfulPressAnimationStateName);
            }
        }

    }

    public override void PressedWhileDisabled(ulong byID, ulong onTick)
    {
        base.PressedWhileDisabled(byID, onTick);
        if (animationTree != null)
        {
            AnimationNodeStateMachine stateMachineNode = (AnimationNodeStateMachine)animationTree.TreeRoot;
            if (stateMachineNode.HasNode(DisabledPressAnimationStateName))
            {
                stateMachine.Travel(DisabledPressAnimationStateName);
            }
        }

    }

    public override void OnEnable(ulong onTick)
    {
        base.OnEnable(onTick);
        if (animationTree != null)
        {
            AnimationNodeStateMachine stateMachineNode = (AnimationNodeStateMachine)animationTree.TreeRoot;
            if (stateMachineNode.HasNode(EnableAnimationStateName))
            {
                stateMachine.Travel(EnableAnimationStateName);
            }
        }
    }

    public override void OnDisable(ulong onTick)
    {
        base.OnDisable(onTick);
        if (animationTree != null)
        {
            AnimationNodeStateMachine stateMachineNode = (AnimationNodeStateMachine)animationTree.TreeRoot;
            if (stateMachineNode.HasNode(DisableAnimationStateName))
            {
                stateMachine.Travel(DisableAnimationStateName);
            }
        }
    }

    public override string GenerateStateString()
    {
        return base.GenerateStateString() + $"|currentAnimation:{stateMachine.GetCurrentNode()}";
    }

}

