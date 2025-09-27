using Godot;
using System;

public partial class LabellingPress : Triggerable
{
    [Export] public AnimationPlayer animationPlayer;
    public override void Triggered()
    {
        Logging.Log($"LabellingPress Triggered", "Triggerable");
        //play animation
        if (animationPlayer != null)
            animationPlayer.Play("press_down");
    }
}