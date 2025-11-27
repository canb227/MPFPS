
using Godot;
using System;
using System.Collections.Generic;
using static GameState;


public abstract partial class GOBaseRigidBody : RigidBody3D, GameObject
{
    [Export]
    public virtual ulong id { get; set; }
    [Export]
    public virtual float priority { get; set; } = 1;
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
    public ulong tickOfLastUpdate { get; set; }
    public override void _Ready()
    {

        SetPhysicsProcess(predict);
    }

    public virtual bool InitFromData(GameObjectConstructorData data)
    {
        try
        {
            GlobalTransform = data.spawnTransform;
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    public abstract byte[] GenerateStateUpdate();
    public abstract void ProcessStateUpdate(byte[] update);
    public abstract void PerTickAuth(double delta);
    public abstract void PerFrameAuth(double delta);
    public abstract void PerTickLocal(double delta);
    public abstract void PerFrameLocal(double delta);
    public abstract string GenerateStateString();
    public abstract void PerTickShared(double delta);
    public abstract void PerFrameShared(double delta);
}

