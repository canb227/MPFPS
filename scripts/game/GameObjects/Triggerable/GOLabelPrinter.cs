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

    [Export]
    public Node3D paperPrintLocation { get; set; }
    [Export]
    public Area3D paperTrayArea { get; set; }

    private Label viewportLabel { get; set; }
    public int paperLoadedCount { get; set; } = 1;
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

    public override void PerTickShared(double delta)
    {
        base.PerTickShared(delta);
        foreach (Trigger t in triggerables)
        {
            if (t.cooldownSecondsRemaining == 0)
            {
                continue;
            }
            if (t.cooldownSecondsRemaining > 0)
            {
                t.cooldownSecondsRemaining -= (float)delta;
            }
            if (t.cooldownSecondsRemaining <= 0)
            {
                Logging.Log($"Trigger {t.triggerName} is off cooldown!", "GOLabelPrinter");
                t.cooldownSecondsRemaining = 0;
                viewportLabel.Text = "Ready To Print!";
            }
        }
    }

    //NETWORKINGTODO is this okay? its called from a trigger animation
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
        viewportLabel.Text = "Ready To Print!";
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

    public void CheckForPaperTray()
    {
        foreach (Node3D node in paperTrayArea.GetOverlappingBodies())
        {
            if (node is GOPaperBox paperBox)
            {
                //node.Dispose();
                node.GlobalPosition = new Vector3(0, 0, 0);
                PaperRefilled();
                break;
            }
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
            paperLoadedCount--;
            if (paperLoadedCount <= 0)
            {
                OutOfPaper();
            }
            else
            {
                viewportLabel.Text = "Cooling Down...";
            }
            GameObject obj = GameObjectLoader.LoadObjectByTypeName("LabelPaper", out GameObjectType type);
            Global.gameState.SpawnObjectAsAuth(obj, type);
            (obj as Node3D).GlobalPosition = paperPrintLocation.GlobalPosition;
        }
    }

    public void ReadyToPrint()
    {

    }
    
}