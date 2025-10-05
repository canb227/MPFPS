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
    [Export]
    public GOLabelMonitor monitor1 { get; set; }
    [Export]
    public GOLabelMonitor monitor2 { get; set; }
    [Export]
    public GOLabelMonitor monitor3 { get; set; }

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
        foreach (Trigger t in triggerables)
        {
            if (t.cooldownSecondsRemaining == 0)
            {
                continue;
            }
            if (t.cooldownSecondsRemaining > 0)
            {
                if (t.triggerName == "print" && !waitingForPaper)
                {
                    t.cooldownSecondsRemaining -= (float)delta;
                }
            }
            if (t.cooldownSecondsRemaining <= 0)
            {
                Logging.Log($"Trigger {t.triggerName} is off cooldown!", "GOLabelPrinter");
                t.cooldownSecondsRemaining = 0;
                if (!waitingForPaper)
                {
                    viewportLabel.Text = "Ready To Print!";
                }
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
        viewportLabel.Text = "Cooling Down...";
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
            GameState.GameObjectConstructorData data = new(GameObjectType.LabelPaper);
            data.spawnTransform.Origin = paperPrintLocation.GlobalPosition;
            data.paramList.Add(monitor1.addressTextOptions[monitor1.textOptionsIndex] + " " + monitor2.addressTextOptions[monitor2.textOptionsIndex] + " " + monitor3.addressTextOptions[monitor3.textOptionsIndex]);
            int digit1 = (monitor1.textOptionsIndex + 1) * 100;
            int digit2 = (monitor1.textOptionsIndex + 1) * 10;
            int digit3 = monitor3.textOptionsIndex + 1;
            data.paramList.Add(digit1 + digit2 + digit3);
            Global.gameState.Auth_SpawnObject(GameObjectType.LabelPaper, data);

            // Node3D paperLabel = PaperLabelScene.Instantiate<Node3D>();
            // paperLabel.Position = paperPrintLocation.Position;
        }
    }

    public void ReadyToPrint()
    {

    }
    
}