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
    { GameObjectType.BakingSoda, "res://assets/ui/icons/BakingSoda.png" },
    { GameObjectType.Book, "res://assets/ui/icons/Book.png" },
    { GameObjectType.CarBattery, "res://assets/ui/icons/CarBattery.png" },
    { GameObjectType.GlassBottle, "res://assets/ui/icons/GlassBottle.png" },
    { GameObjectType.HandSaw, "res://assets/ui/icons/HandSaw.png" },
    { GameObjectType.JerryCan, "res://assets/ui/icons/JerryCan.png" },
    { GameObjectType.SawBlade, "res://assets/ui/icons/SawBlade.png" },
    { GameObjectType.Tire, "res://assets/ui/icons/Tire.png"},
};

public static Dictionary<GameObjectType, string> ItemDisplayNameDictionary = new()
{
    { GameObjectType.BakingSoda, "Baking Soda" },
    { GameObjectType.Book, "Book" },
    { GameObjectType.CarBattery, "Car Battery" },
    { GameObjectType.GlassBottle, "Glass Bottle" },
    { GameObjectType.HandSaw, "Hand Saw" },
    { GameObjectType.JerryCan, "Jerry Can" },
    { GameObjectType.SawBlade, "Saw Blade" },
    { GameObjectType.Tire, "Tire"},
};

    

    public override void _Ready()
    {
        base._Ready();
        this.CollisionLayer = 1 << 1; //2
        this.CollisionMask = (1 << 0) | (1 << 1) | (1 << 3) | (1 << 4);//1,2,4,5
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

