using System.Collections.Generic;
using Godot;
using Godot.Collections;
using MessagePack;

[GlobalClass]
public partial class GOLabelPrinter : GOTriggerable
{
    [Export]
    public AnimationPlayer animationPlayer { get; set; }

    [Export]
    public SubViewport viewport { get; set; }

    [Export]
    public Node3D paperPrintLocation { get; set; }

    [Export]
    public PackedScene PaperLabelScene { get; set; }
    
    private Label viewportLabel { get; set; }
    public int paperLoadedCount { get; set; } = 4;
    public bool waitingForPaper { get; set; } = false;


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

    public override void PerFrameAuth(double delta)
    {

    }

    public override void PerFrameLocal(double delta)
    {

    }

    public override void PerTickLocal(double delta)
    {

    }

    public override void ProcessStateUpdate(byte[] update)
    {

    }
    public override byte[] GenerateStateUpdate()
    {
        return new byte[0];
    }

    public override void PerFrameShared(double delta)
    {

    }

    public override void PerTickAuth(double delta)
    {

    }

    public void OutOfPaper()
    {
        viewportLabel.Text = "Need Paper, Insert In Tray Below";
        waitingForPaper = true;
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
        waitingForPaper = false;
        paperLoadedCount = 4;
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
        if (paperLoadedCount <= 0 && !waitingForPaper)
        {
            OutOfPaper();
        }
        else
        {
            Node3D paperLabel = PaperLabelScene.Instantiate<Node3D>();
            paperLabel.Position = paperPrintLocation.Position;
        }
    }
    
}