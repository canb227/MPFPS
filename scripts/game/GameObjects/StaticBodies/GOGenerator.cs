using System.Collections.Generic;
using Godot;
using Godot.Collections;
using MessagePack;

[GlobalClass]
public partial class GOGenerator : GOBaseStaticBody
{
    [Export] Area3D generatorArea;
    public float generatorHealthInSeconds = 0.0f;
    public float generatorMaxHealth = 45.0f;
    public override void _Ready()
    {
        base._Ready();
        Global.gameState.gameModeManager.generator = this;
        generatorArea.BodyEntered += OnBodyEntered;
        generatorArea.BodyExited += OnBodyExited;
    }
    private int robotsInArea = 0;
    private void OnBodyEntered(Node3D body)
    {
        if (body.IsInGroup("enemies")) // or body.IsInGroup("robots")
        {
            robotsInArea++;
        }
    }

    private void OnBodyExited(Node3D body)
    {
        if (body.IsInGroup("enemies"))
        {
            robotsInArea--;
            if (robotsInArea < 0) robotsInArea = 0;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (robotsInArea > 0)
        {
            generatorHealthInSeconds -= (float)delta;

            if(generatorHealthInSeconds <= 0)
            {
                //traitors win
            }
            else if(generatorHealthInSeconds <= 30)
            {
                //announcer alert
            }
        }
        else
        {
            generatorHealthInSeconds += (float)delta;
            if (generatorHealthInSeconds > generatorMaxHealth)
                generatorHealthInSeconds = generatorMaxHealth;
        }
    }

    public override string GenerateStateString()
    {
        return $"I am the generator";
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