
using Godot;
using System;


public abstract partial class GOBaseStaticBody : StaticBody3D, GameObject
{
    public virtual ulong id { get; set; }
    public virtual float priority { get; set; } = 1;
    public virtual ulong authority { get; set; }
    public virtual bool dirty { get; set; } = false;

    public virtual GameObjectType type { get; set; }
    public virtual bool predict { get; set; } = true;
    public virtual bool sleeping { get; set; }
    public virtual bool destroyed { get; set; }
    public virtual float priorityAccumulator { get; set; }
    public abstract string GenerateStateString();
    public abstract byte[] GenerateStateUpdate();
    public abstract void PerFrameAuth(double delta);
    public abstract void PerFrameLocal(double delta);
    public abstract void PerFrameShared(double delta);
    public abstract void PerTickAuth(double delta);
    public abstract void PerTickLocal(double delta);
    public abstract void PerTickShared(double delta);
    public abstract void ProcessStateUpdate(byte[] update);
}

