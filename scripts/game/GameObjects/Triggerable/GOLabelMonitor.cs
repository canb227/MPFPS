using System.Collections.Generic;
using Godot;
using Godot.Collections;
using MessagePack;

[GlobalClass]
public partial class GOLabelMonitor : GOBaseStaticTriggerable
{

    [Export]
    public SubViewport viewport { get; set; }
    private Label viewportLabel { get; set; }

    //export is temporary for testing, should be filled elsewhere
    [Export]
    public Array<string> addressTextOptions { get; set; }
    public int textOptionsIndex { get; set; } = 0;


    public override void _Ready()
    {
        base._Ready();
        viewportLabel = viewport.GetNode<Label>("Label");
        viewportLabel.Text = addressTextOptions[textOptionsIndex];
    }


    public override void ActivateTriggerEffects(string triggerName, ulong byID)
    {
        textOptionsIndex = (textOptionsIndex + 1) % addressTextOptions.Count;
        viewportLabel.Text = addressTextOptions[textOptionsIndex];
    }
    
}