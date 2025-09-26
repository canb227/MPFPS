
using Godot;
using System;


public abstract partial class GOBaseRigidBody : RigidBody3D, IGameObject
{
    public virtual ulong id { get; set; }
    public virtual float priority { get; set; } = 1;
    public virtual ulong authority { get; set; }
    public virtual bool dirty { get; set; } = false;
    public virtual CollisionShape3D collider { get; set; }
    public virtual MeshInstance3D mesh { get; set; }
    public virtual GameObjectType type { get; set; }
    public virtual bool predict { get; set; } = true;
    public virtual bool sleeping { get; set; }
    public virtual bool destroyed { get; set; }

    public override void _Ready()
    {
        mesh = GetNode<MeshInstance3D>("mesh");
        collider = GetNode<CollisionShape3D>("collider");
        SetPhysicsProcess(predict);
    }
    public abstract byte[] GenerateStateUpdate();
    public abstract void ProcessStateUpdate(byte[] update);
    public abstract void PerTickAuth(double delta);
    public abstract void PerFrameAuth(double delta);
    public abstract void PerTickLocal(double delta);
    public abstract void PerFrameLocal(double delta);
    public abstract string GenerateStateString();
}

