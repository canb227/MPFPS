using System.Collections.Generic;
using Godot;
using MessagePack;

[GlobalClass]
public partial class GOComponent : SimpleShape
{
    [Export] public MeshInstance3D packageItemMesh { get; set; }
    [Export] public CollisionShape3D packageItemCollider { get; set; }
    [Export] public GameObjectType itemType;

    public override void _Ready()
    {
        base._Ready();
    }

    public override bool InitFromData(GameObjectConstructorData data)
    {
        if (base.InitFromData(data))
        {
            return true;
        }
        return false;
    }
}


