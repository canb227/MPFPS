using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class GameObject : Node3D
{

    ulong UID;

    [Export]
    GameObjectType type;
    
    public ulong GetUID()
    {
        return UID;
    }

    internal void SetUID(ulong withID)
    {
        this.UID= withID;
    }

    public void PerFrame(double delta)
    {

    }

    public void Tick(double delta)
    {

    }
}

public enum GameObjectType
{
    NONE,


}