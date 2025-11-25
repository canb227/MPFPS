using System.Collections.Generic;
using Godot;
using Godot.Collections;
using MessagePack;

[GlobalClass]
public partial class GOOrderMonitor : GOBaseStaticBody
{
    [Export] public MarginContainer MonitorScreen { get; set; }
    [Export] public Label orderNumberLabel { get; set; }
    [Export] public HBoxContainer packageItems { get; set; }
    [Export] public Label addressLabel { get; set; }
    [Export] public Label orderStatusLabel { get; set; }
    [Export] public ColorRect backgroundColor { get; set; }
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
        GameModeManager.OnOrderPacked += ShowOrderAsPacked;
        GameModeManager.OnOrderLabelled += ShowOrderAsLabelled;
        GameModeManager.OnOrderReadyToDeliver += ShowOrderAsReadyToDeliver;
        GameModeManager.OnOrderFinished += ShowOrderAsFinished;

    }

    public void ShowOrderAsPacked(int orderNumber)
    {
        if(this.orderNumber-1 == orderNumber)
        {
            orderStatusLabel.Text = "\n\n\nPackage Has Been Created...\nAwaiting Labelling...";
            backgroundColor.Color = new(0.044f, 0.044f, 0.0f, 0.25f);
        }

    }
    public void ShowOrderAsLabelled(int orderNumber)
    {
        if (this.orderNumber-1 == orderNumber)
        {
            orderStatusLabel.Text = "\n\nPackage Has Been Created...\nPackage Has Been Labelled...\nAwaiting Package Deposit At Shipping...";
            backgroundColor.Color = new(0.081f, 0.001f, 0.133f, 0.25f);
        }
    }
    public void ShowOrderAsReadyToDeliver(int orderNumber)
    {
        if (this.orderNumber-1 == orderNumber)
        {
            orderStatusLabel.Text = "\n\nPackage Has Been Created...\nPackage Has Been Labelled...\nPackage Has Been Deposited...\nAwaiting For Package to Be Delivered...";
            backgroundColor.Color = new(0.0f, 0.041f, 0.141f, 0.25f);
        }
    }
    public void ShowOrderAsFinished(int orderNumber)
    {
        if (this.orderNumber-1 == orderNumber)
        {
            orderStatusLabel.Text = "\n\n\nORDER COMPLETED!";
            orderCompletedImage.Visible = true;
            backgroundColor.Color = new(0.0f, 0.187f, 0.015f, 0.25f);
        }
        //if this is the previous order finishing then we can start displaying
        if(this.orderNumber-2 == orderNumber)
        {
            MonitorScreen.Visible = true;
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            GameModeManager.OnPackageOrdersUpdated -= UpdateDisplayedOrder;
            GameModeManager.OnOrderPacked -= ShowOrderAsPacked;
            GameModeManager.OnOrderLabelled -= ShowOrderAsLabelled;
            GameModeManager.OnOrderReadyToDeliver -= ShowOrderAsReadyToDeliver;
            GameModeManager.OnOrderFinished -= ShowOrderAsFinished;
        }
    }

    public void UpdateDisplayedOrder()
    {
        orderCompletedImage.Visible = false;
        orderNumberLabel.Text = "Order #" + orderNumber;
        if (Global.gameState.gameModeManager.packageOrders.Count < orderNumber)
        {
            Logging.Log("Trying to update order of a monitor that doesn't exist (packageOrders.Count < orderNumber [for this monitor]).", "GOOrderMonitor");
            MonitorScreen.Visible = false;
            return;
        }
        MonitorScreen.Visible = false;
        PackageOrderInfo orderInfo = Global.gameState.gameModeManager.packageOrders[orderNumber-1];
        if (orderInfo.OrderIsFinished())
        {
            Logging.Log("Trying to assign order to a monitor that has already been finished.", "GOOrderMonitor");
        }
        
        foreach(var child in packageItems.GetChildren())
        {
            child.Free();
        }
        foreach (GameObjectType item in orderInfo.neededPackageItems)
        {
            VBoxContainer itemContainer = new();
            //itemContainer.CustomMinimumSize = new Vector2(128, 0);
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