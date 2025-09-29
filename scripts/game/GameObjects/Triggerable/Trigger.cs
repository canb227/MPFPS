using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class Trigger : Resource
{
    [Export]
    public NodePath triggerableNode;

    [Export]
    public string triggerName;
}

