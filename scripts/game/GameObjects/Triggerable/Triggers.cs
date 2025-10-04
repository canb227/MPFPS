using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class Triggers : Resource
{
    [Export]
    public NodePath triggerableNode
    {
        get { return _triggerableNode; }
        set { 
            
            _triggerableNode = value; 
        }
    }
    private NodePath _triggerableNode;


    [Export]
    public string triggerName;

    public override string ToString()
    {
        return $"Target Triggerable Node:{_triggerableNode} | Trigger Name: {triggerName}";
    }

}

