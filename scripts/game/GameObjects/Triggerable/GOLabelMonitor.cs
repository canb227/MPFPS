using System.Collections.Generic;
using Godot;
using Godot.Collections;
using MessagePack;

[GlobalClass]
public partial class GOLabelMonitor : GOTriggerable
{
    [Export]
    public AnimationPlayer animationPlayer { get; set; }
    [Export]
    public SubViewport viewport { get; set; }
    private Label viewportLabel { get; set; }

    //export is temporary for testing, should be filled elsewhere
    [Export]
    public Array<string> addressTextOptions { get; set; }
    private int textOptionsIndex { get; set; } = 0;


    public override void _Ready()
    {
        base._Ready();
        viewportLabel = viewport.GetNode<Label>("Label");
        viewportLabel.Text = addressTextOptions[textOptionsIndex];
    }


    public override void ActivateTriggerEffects(string triggerName, ulong byID)
    {
        if (!animationPlayer.HasAnimation(triggerName))
        {
            Logging.Error($"The AnimationPlayer of {Name} ({id}) is missing an animation that matches the triggerName: {triggerName}!", "GOMonitor");
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

    //NETWORKINGTODO is this okay? its called from a trigger animation
    public void NextDisplay()
    {
        textOptionsIndex = (textOptionsIndex + 1) % addressTextOptions.Count;
        viewportLabel.Text = addressTextOptions[textOptionsIndex];
    }
    
}