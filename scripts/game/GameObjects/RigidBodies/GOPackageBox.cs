using System;
using System.Collections.Generic;
using Godot;
using MessagePack;

[GlobalClass]
public partial class GOPackageBox : SimpleShape
{
    public int orderNumber;

    public override void _Ready()
    {
        base._Ready();
        this.CollisionLayer = 1 << 1; //2
        this.CollisionMask = (1 << 0) | (1 << 1) | (1 << 3) | (1 << 4);//1,2,4,5
    }

    public void ApplyLabel()
    {
        //use orderNumber to get our order info and label ourselves
        GD.Print("LABEL APPLIED");
    }

    public override bool InitFromData(GameObjectConstructorData data)
    {
        if (base.InitFromData(data))
        {
            orderNumber = (int)data.paramList[0];
            return true;
        }
        return false;
    }
}

