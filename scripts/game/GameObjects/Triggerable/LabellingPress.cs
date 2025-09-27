using Godot;
using System;

public partial class LabellingPress : Crusher
{
    public override void OnTrigger()
    {
        base.OnTrigger();
        Logging.Log($"Labelling Press Triggered","LabellingPress");
    }
}