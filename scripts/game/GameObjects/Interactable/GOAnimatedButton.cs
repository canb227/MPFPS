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
        stateMachine.Travel(FailedPressAnimationStateName);
    }

    public override void PressedSuccessfully(ulong byID)
    {
        base.PressedSuccessfully(byID);
        stateMachine.Travel(SuccessfulPressAnimationStateName);
    }

    public override void PressedWhileDisabled(ulong byID)
    {
        base.PressedWhileDisabled(byID);
        stateMachine.Travel(PressedWhileDisabledAnimationStateName);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        stateMachine.Travel(EnableAnimationStateName);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        stateMachine.Travel(DisableAnimationStateName);
    }



}

