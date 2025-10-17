using System;
using System.Collections.Generic;
using Godot;
using MessagePack;

[GlobalClass]
public partial class GOPackageItem : SimpleShape
{
    [Export] public MeshInstance3D packageItemMesh { get; set; }
    [Export] public CollisionShape3D packageItemCollider { get; set; }
    [Export] public GameObjectType itemType { get; set; }
    [Export] public Texture2D icon { get; set; }
    [Export] public String displayName { get; set; }

    public static Dictionary<GameObjectType, string> ItemIconDictionary = new()
    {

    };

    public static Dictionary<GameObjectType, string> ItemDisplayNameDictionary = new()
    {

    };

    

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

