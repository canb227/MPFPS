using Godot;
using System;

//triggerable is effectively an abstract class but is derived from Node so it can't :)
[GlobalClass]
public partial class Triggerable : Node
{
    public virtual void Triggered()
    {
        Logging.Error($"Abstract Class Triggerable Called", "Triggerable");
    }
}