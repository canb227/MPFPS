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
    public AnimationPlayer animationPlayer { get; set; }

    [Export]
    public string SuccessfulPressAnimation { get; set; }

    [Export]
    public string FailedPressAnimation { get; set; }

    [Export]
    public string PressedWhileOnCooldownAnimation { get; set; }

    [Export]
    public string DisableAnimation { get; set; }

    [Export]
    public string EnableAnimation { get; set; }

    public override void PressedFailed(ulong byID)
    {
        base.PressedFailed(byID);
        if (FailedPressAnimation == null || FailedPressAnimation == "")
        {
            return;
        }
        else
        {
            animationPlayer.Play(FailedPressAnimation);
        }

    }

    public override void PressedWhileOnCooldown(ulong byID)
    {
        base.PressedWhileOnCooldown(byID);
        if (PressedWhileOnCooldownAnimation==null || PressedWhileOnCooldownAnimation == "")
        {
            return;
        }
        else
        {
            animationPlayer.Play(PressedWhileOnCooldownAnimation);
        }
    }

    public override void PressedSuccessfully(ulong byID)
    {
        base.PressedSuccessfully(byID);
        if (SuccessfulPressAnimation == null || SuccessfulPressAnimation == "")
        {
            return;
        }
        else
        {
            animationPlayer.Play(SuccessfulPressAnimation);
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        if (EnableAnimation == null || EnableAnimation == "")
        {
            return;
        }
        else
        {
            animationPlayer.Play(EnableAnimation);
        }
    }
}

