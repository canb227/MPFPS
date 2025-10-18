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

    [Export] public LabelMonitorType labelMonitorType { get; set; }
    public List<string> addressTextOptions { get; set; }
    
    public int textOptionsIndex { get; set; } = 0;


    public override void _Ready()
    {
        base._Ready();
        viewportLabel = viewport.GetNode<Label>("Label");
        GameModeManager.OnPossibleAddressesUpdated += AddressTextOptionsChanged;
    }

    public void AddressTextOptionsChanged()
    {
        if (labelMonitorType == LabelMonitorType.Number)
        {
            addressTextOptions = Global.gameState.gameModeManager.possibleRoundAddressNumbers;
        }
        else if (labelMonitorType == LabelMonitorType.Street)
        {
            addressTextOptions = Global.gameState.gameModeManager.possibleRoundAddressStreets;
        }
        else if (labelMonitorType == LabelMonitorType.Suffix)
        {
            addressTextOptions = Global.gameState.gameModeManager.possibleRoundAddressSuffixes;
        }
        viewportLabel.Text = addressTextOptions[textOptionsIndex];
    }


    public override void ActivateTriggerEffects(string triggerName, ulong byID)
    {
        textOptionsIndex = (textOptionsIndex + 1) % addressTextOptions.Count;
        viewportLabel.Text = addressTextOptions[textOptionsIndex];
    }

}

public enum LabelMonitorType
{
    Number,
    Street,
    Suffix
}