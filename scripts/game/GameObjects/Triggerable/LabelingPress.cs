using Godot;
using System;

public partial class LabelingPress : Triggerable
{
    public override void Triggered()
    {
        GD.Print("trigger");
    }
}