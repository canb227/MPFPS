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
    [Export] public MeshInstance3D _outline;
	private bool outlineDesiredState;


    public override void _Ready()
    {
        base._Ready();
        viewportLabel = viewport.GetNode<Label>("Label");
        if (paperLoadedCount <= 0)
        {
            OutOfPaper();
        }
        Global.gameState.gameModeManager.labelPrinter = this;
    }

    public void SetHighlighted(bool enabled)
    {
		outlineDesiredState = enabled;
        _outline.Visible = enabled;
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
        if(outlineDesiredState)
        {
            if(Global.gameState.AIManager.localPlayer != null && this.GlobalPosition.DistanceSquaredTo(Global.gameState.AIManager.localPlayer.GlobalPosition) < 20f)
            {
                _outline.Visible = false;
            }
            else
            {
                _outline.Visible = true;
            }
        }
        else
		{
			_outline.Visible = false;
		}
        
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
                if (!waitingForPaper)
                {
                    viewportLabel.Text = "Ready To Print!";
                }
            }
        }
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
        viewportLabel.Text = "Preparing...";
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
            if (node is GOComponent paperBox)
            {
                if(paperBox.itemType == GameObjectType.PaperBox)
                {
                    //node.Dispose();
                    node.GlobalPosition = new Vector3(0, 0, 0);
                    PaperRefilled();
                    break;
                }
            }
        }
    }

    public void PrintLabel()
    {
        if (paperLoadedCount <= 0 && !waitingForPaper)
        {
            OutOfPaper();
        }
        else if (waitingForPaper)
        {
            CheckForPaperTray();
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
                viewportLabel.Text = "Preparing...";
            }
            if (Global.Lobby.bIsLobbyHost)
            {
                GameObjectConstructorData data = new(GameObjectType.LabelPaper);
                data.spawnTransform.Origin = paperPrintLocation.GlobalPosition;
                data.paramList.Add(monitor1.addressTextOptions[monitor1.textOptionsIndex] + " " + monitor2.addressTextOptions[monitor2.textOptionsIndex] + " " + monitor3.addressTextOptions[monitor3.textOptionsIndex]);
                //CALCULATE WHAT ORDER THIS ADDRESS RELATES TO, -1 IF NONE
                int orderNum = FindOrderNumber(monitor1.addressTextOptions[monitor1.textOptionsIndex], monitor2.addressTextOptions[monitor2.textOptionsIndex], monitor3.addressTextOptions[monitor3.textOptionsIndex]);
                data.paramList.Add(orderNum);
                Global.gameState.Auth_SpawnObject(GameObjectType.LabelPaper, data);
            }
            // Node3D paperLabel = PaperLabelScene.Instantiate<Node3D>();
            // paperLabel.Position = paperPrintLocation.Position;
        }
    }

    public int FindOrderNumber(string addressNumber, string addressStreet, string addressSuffix)
    {
        // Loop through orders and try to match
        List<PackageOrderInfo> orderList = Global.gameState.gameModeManager.packageOrders;
        for (int orderNumber = 0; orderNumber < orderList.Count; orderNumber++)
        {
            if (addressNumber == orderList[orderNumber].addressNumber && addressStreet == orderList[orderNumber].addressStreet && addressSuffix == orderList[orderNumber].addressSuffix)
            {
                return orderNumber;
            }
        }
        // No match
        return -1;
    }

    public void ReadyToPrint()
    {

    }
    
}