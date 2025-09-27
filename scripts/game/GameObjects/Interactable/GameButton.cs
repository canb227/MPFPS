using Godot;
using System;

public partial class GameButton : Node
{
    [Export] public StaticBody3D staticBody;
    [Export] public AnimationPlayer animationPlayer;

    //all effects of buttons are handeled by a Triggerable script
    //for example the labelling press will be a LabellingPress : Triggerable : Node
    [Export] private Triggerable triggerTarget;

    public override void _Ready()
    {
        if (staticBody == null)
            GD.PrintErr($"{Name}: StaticBody not assigned!");
    }

    public void Press()
    {
        //play animation
        if (animationPlayer != null )
            animationPlayer.Play("button_press");

        //trigger effect on target
        if (triggerTarget != null)
            triggerTarget.Triggered();
    }
}
