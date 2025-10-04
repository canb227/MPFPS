using System.Collections.Generic;
using Godot;
using Godot.Collections;
using MessagePack;

[GlobalClass]
public partial class GOLabelPrinter : GOBaseStaticTriggerable
{
    [Export]
    public AnimationPlayer animationPlayer { get; set; }
    [Export]
    public SubViewport viewport { get; set; }
    private Label viewportLabel { get; set; }
    public int paperLoadedCount { get; set; }


    public override void _Ready()
    {
        base._Ready();
        viewportLabel = viewport.GetNode<Label>("Label");
        if (paperLoadedCount <= 0)
        {
            OutOfPaper();
        }
    }


    public override void ActivateTriggerEffects(string triggerName, ulong byID)
    {
        if (!animationPlayer.HasAnimation(triggerName))
        {
            Logging.Error($"The AnimationPlayer of {Name} ({id}) is missing an animation that matches the triggerName: {triggerName}!", "GOLabelPrinter");
            return;
        }
        else
        {
            animationPlayer.Play(triggerName);
        }
    }

    //NETWORKINGTODO is this okay? its called from a trigger animation
    public void OutOfPaper()
    {
        viewportLabel.Text = "Need Paper, Insert In Tray Below";
        if (!animationPlayer.HasAnimation("need_paper"))
        {
            Logging.Error($"The AnimationPlayer of {Name} ({id}) is missing an animation that matches the triggerName: need_paper!", "GOLabelPrinter");
            return;
        }
        else
        {
            animationPlayer.Play("need_paper");
        }
    }

    public void PaperRefilled()
    {
        viewportLabel.Text = "Ready To Print";
        if (!animationPlayer.HasAnimation("paper_filled"))
        {
            Logging.Error($"The AnimationPlayer of {Name} ({id}) is missing an animation that matches the triggerName: paper_filled!", "GOLabelPrinter");
            return;
        }
        else
        {
            animationPlayer.Play("paper_filled");
        }
    }

    public void PrintLabel()
    {
        if (paperLoadedCount <= 0)
        {
            OutOfPaper();
        } 
    }
    
}