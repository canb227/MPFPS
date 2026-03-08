using System;
using System.Collections.Generic;
using Godot;
using MessagePack;

[GlobalClass]
public partial class GOPackageBox : SimpleShape
{
    [Export] MeshInstance3D packageMesh { get; set; }
    [Export] public CollisionShape3D packageCollision { get; set; }
    [Export] ViewportTexture viewportTexture { get; set; }
    [Export] Label addressLabel { get; set; }
    [Export] HBoxContainer packageItems { get; set; }
    [Export] Area3D packageTipArea {get; set;}
    public int orderNumber = -1;
    public bool labelApplied = false;

    public override void _Ready()
    {
        base._Ready();
        this.CollisionLayer = 1 << 1; //2
        this.CollisionMask = (1 << 0) | (1 << 1) | (1 << 3) | (1 << 4);//1,2,4,5
               BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node body)
    {
        if (body is GOBasePlayerCharacter bpc)
        {
            bpc.AddPackageTip(this);
        }
    }

    private void OnBodyExited(Node body)
    {
        if (body is GOBasePlayerCharacter bpc)
        {
            bpc.RemovePackageTip(this);
        }
    }

    public void AddPackedItems()
    {
        PackageOrderInfo orderInfo = Global.gameState.gameModeManager.packageOrders[orderNumber];
        foreach (GameObjectType item in orderInfo.neededPackageItems)
        {
            VBoxContainer itemContainer = new();
            itemContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            TextureRect texture = new();
            texture.Texture = (Texture2D)ResourceLoader.Load(GOPackageItem.ItemIconDictionary[item]);
            texture.ExpandMode = TextureRect.ExpandModeEnum.FitHeight;
            texture.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            itemContainer.AddChild(texture);
            Label label = new();
            label.Text = GOPackageItem.ItemDisplayNameDictionary[item];
            label.LabelSettings = GD.Load<LabelSettings>("res://scenes/ui/hud/MonitorFontTiny.tres");
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            itemContainer.AddChild(label);

            packageItems.AddChild(itemContainer);
            Control spacer = new();
            spacer.CustomMinimumSize = new Vector2(8, 0);
            packageItems.AddChild(spacer);
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void ApplyLabel()
    {
        PackageOrderInfo orderInfo = Global.gameState.gameModeManager.packageOrders[orderNumber];
        addressLabel.Text = orderInfo.addressNumber + " " + orderInfo.addressStreet + " " + orderInfo.addressSuffix;
                
        var mat1 = new StandardMaterial3D();
        mat1.AlbedoTexture = GD.Load<Texture2D>("res://assets/models/props/cardboardbox_label.png");
        packageMesh.SetSurfaceOverrideMaterial(0, mat1);

        // --- Material 2: Viewport texture ---
        var mat2 = new StandardMaterial3D();
        mat2.ResourceLocalToScene = true;
        mat2.AlbedoTexture = viewportTexture;
        packageMesh.SetSurfaceOverrideMaterial(1, mat2);
        labelApplied = true;
    }

    public override bool InitFromData(GameObjectConstructorData data)
    {
        if (base.InitFromData(data))
        {
            orderNumber = (int)data.paramList[0];
            AddPackedItems();
            return true;
        }
        return false;
    }
}

