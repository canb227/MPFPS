using System.Collections.Generic;
using Godot;
using MessagePack;

[GlobalClass]
public partial class GOPackageItem : SimpleShape
{
    public MeshInstance3D packageItemMesh { get; set; }
    public CollisionShape3D packageItemCollider { get; set; }
    public int itemTypeID;

    public readonly static Dictionary<int, (MeshInstance3D, CollisionShape3D)> packageItemDictionary = new()
    {
        { 0, (null,null) },
        { 1, (null,null)}
    };

    public override void _Ready()
    {
        base._Ready();
    }

    public override bool InitFromData(GameObjectConstructorData data)
    {
        if (base.InitFromData(data))
        {
            itemTypeID = (int)data.paramList[0];
            return true;
        }
        return false;
    }
}

