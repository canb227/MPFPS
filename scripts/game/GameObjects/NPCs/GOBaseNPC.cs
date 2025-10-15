using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public partial class GOBaseNPC : GOBaseCharacterBody3D
{
    [Export]
    public NavigationAgent3D navAgent;

    [Export]
    public Node3D MovementTarget = new();


    public override string GenerateStateString()
    {
        return "Not Implemented :)";
    }

    public override byte[] GenerateStateUpdate()
    {
        return new byte[0];
    }

    public override void PerFrameAuth(double delta)
    {
        
    }

    public override void PerFrameLocal(double delta)
    {
        
    }

    public override void PerFrameShared(double delta)
    {
        
    }

    public override void PerTickAuth(double delta)
    {
        
    }

    public override void PerTickLocal(double delta)
    {
        
    }

    public override void PerTickShared(double delta)
    {
        
    }

    public override void ProcessStateUpdate(byte[] update)
    {
        
    }
}

