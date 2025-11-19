using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public partial class GOBaseHordeNPC : Node3D, GameObject
{
    [Export]
    public Node3D MovementTarget = new();

    [Export]
    public virtual ulong id { get; set; }
    [Export]
    public virtual float priority { get; set; } = 2;
    [Export]
    public virtual ulong authority { get; set; }
    public virtual bool dirty { get; set; } = false;
    [Export]
    public virtual GameObjectType type { get; set; }
    public virtual bool predict { get; set; } = true;
    public virtual bool sleeping { get; set; }
    public virtual bool destroyed { get; set; }
    [Export]
    public virtual float priorityAccumulator { get; set; }

    public virtual bool InitFromData(GameObjectConstructorData data)
    {
        try
        {
            GlobalTransform = data.spawnTransform;
            return true;
        }
        catch (Exception ex)
        {
            Logging.Log(ex.ToString(), "GOBaseHordeNPC");
            return false;
        }
    }


    public override void _Ready()
    {
        SetPhysicsProcess(predict);
    }

    public virtual string GenerateStateString()
    {
        return "Not Implemented :)";
    }

    public virtual byte[] GenerateStateUpdate()
    {
        return new byte[0];
    }

    public virtual void PerFrameAuth(double delta)
    {
        
    }

    public virtual void PerFrameLocal(double delta)
    {
        
    }

    public virtual void PerFrameShared(double delta)
    {
        
    }

    public virtual void PerTickAuth(double delta)
    {
        
    }

    public virtual void PerTickLocal(double delta)
    {
        
    }

    public virtual void PerTickShared(double delta)
    {
        
    }

    public virtual void ProcessStateUpdate(byte[] update)
    {
        
    }
}

