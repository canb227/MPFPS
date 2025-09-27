
using Godot;
using System;


public abstract partial class GOBaseStaticBody : StaticBody3D, IGameObject
{
    public virtual ulong id { get; set; }
    public virtual float priority { get; set; } = 1;
    public virtual ulong authority { get; set; }
    public virtual bool dirty { get; set; } = false;

    [Export]
    public virtual CollisionShape3D collider { get; set; }

    [Export]
    public virtual MeshInstance3D mesh { get; set; }
    public virtual GameObjectType type { get; set; }
    public virtual bool predict { get; set; } = true;
    public virtual bool sleeping { get; set; }
    public virtual bool destroyed { get; set; }

    public override void _Ready()
    {
        if(mesh==null)
        {
            mesh = GetNode<MeshInstance3D>("mesh");
        }
        if (collider==null)
        {
            collider = GetNode<CollisionShape3D>("collider");
        }

    }
    public abstract byte[] GenerateStateUpdate();
    public abstract void ProcessStateUpdate(byte[] update);
    public abstract void PerTickAuth(double delta);
    public abstract void PerFrameAuth(double delta);
    public abstract void PerTickLocal(double delta);
    public abstract void PerFrameLocal(double delta);
    public abstract string GenerateStateString();
}

