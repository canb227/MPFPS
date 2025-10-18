using System.Collections.Generic;
using Godot;
using Godot.Collections;
using MessagePack;

[GlobalClass]
public partial class GOOrderMonitor : GOBaseStaticBody
{
    [Export] public Label orderNumberLabel { get; set; }
    [Export] public HBoxContainer packageItems { get; set; }
    [Export] public Label addressLabel { get; set; }
    [Export] public TextureRect orderCompletedImage { get; set; }
    [Export] public int orderNumber { get; set; }

    public override string GenerateStateString()
    {
        return $"I am the monitor showing order number: {orderNumber}";
    }
    public override byte[] GenerateStateUpdate()
    {
        return new byte[0];
    }

    public override void PerFrameAuth(double delta)
    {
        
    }
    public override void PerFrameLocal(double delta) 
    {
        
    }
    public override void PerFrameShared(double delta) 
    {
        
    }
    public override void PerTickAuth(double delta)  
    {
        
    }
    public override void PerTickLocal(double delta)   
    {
        
    }
    public override void PerTickShared(double delta)   
    {
        
    }
    public override void ProcessStateUpdate(byte[] update)    
    {
        
    }
    public override void _Ready()
    {
        base._Ready();
        GameModeManager.OnPackageOrdersUpdated += UpdateDisplayedOrder;
    }

    public void UpdateDisplayedOrder()
    {
        orderCompletedImage.Visible = false;
        orderNumberLabel.Text = "Order #" + orderNumber;
        PackageOrderInfo orderInfo = Global.gameState.gameModeManager.packageOrders[orderNumber];
        if (orderInfo.OrderIsFinished())
        {
            Logging.Warn("Trying to assign order to a monitor that has already been finished.", "GOOrderMonitor");
        }
        
        foreach(var child in packageItems.GetChildren())
        {
            child.Free();
        }
        foreach (GameObjectType item in orderInfo.neededPackageItems)
        {
            VBoxContainer itemContainer = new();
            itemContainer.CustomMinimumSize = new Vector2(128, 0);
            TextureRect texture = new();
            texture.Texture = (Texture2D)ResourceLoader.Load(GOPackageItem.ItemIconDictionary[item]);
            texture.ExpandMode = TextureRect.ExpandModeEnum.FitHeight;
            texture.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            itemContainer.AddChild(texture);
            Label label = new();
            label.Text = GOPackageItem.ItemDisplayNameDictionary[item];
            itemContainer.AddChild(label);

            packageItems.AddChild(itemContainer);
            Control spacer = new();
            spacer.CustomMinimumSize = new Vector2(8, 0);
            packageItems.AddChild(spacer);
        }
        
        addressLabel.Text = orderInfo.addressNumber + " " + orderInfo.addressStreet + " " + orderInfo.addressSuffix;
    }

    public void SetDisplayedOrder(int orderNumber)
    {
        this.orderNumber = orderNumber;
        UpdateDisplayedOrder();
    }
    
    public void OrderCompleted()
    {
        orderCompletedImage.Visible = true;
    }
    
}