using System.Collections.Generic;
using Godot;
using Godot.Collections;
using MessagePack;

[GlobalClass]
public partial class GOOrderMonitor : GOBaseStaticTriggerable
{
    [Export] public Label orderNumberLabel { get; set; }
    [Export] public HBoxContainer packageItems { get; set; }
    [Export] public Label addressLabel { get; set; }
    [Export] public TextureRect orderCompletedImage { get; set; }
    public int orderNumber { get; set; }


    public override void _Ready()
    {
        base._Ready();

        //viewportLabel.Text = addressTextOptions[textOptionsIndex];
    }


    // public string addressNumber;

    // public string addressStreet;

    // public string addressSuffix;

    // public List<GameObjectType> neededPackageItems;

    // private bool isFinished;
    public void SetDisplayedOrder(int orderNumber)
    {
        orderCompletedImage.Visible = false;
        this.orderNumber = orderNumber;
        orderNumberLabel.Text = "Order #" + orderNumber;
        PackageOrderInfo orderInfo = Global.gameState.gameModeManager.packageOrders[orderNumber];
        if (orderInfo.OrderIsFinished())
        {
            Logging.Warn("Trying to assign order to a monitor that has already been finished.", "GOOrderMonitor");
        }
        addressLabel.Text = orderInfo.addressNumber + " " + orderInfo.addressStreet + " " + orderInfo.addressSuffix;
        
        foreach(var child in packageItems.GetChildren())
        {
            child.Free();
        }
        foreach(GameObjectType item in orderInfo.neededPackageItems)
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
        }
    }
    
    public void OrderCompleted()
    {
        orderCompletedImage.Visible = true;
    }


    public override void ActivateTriggerEffects(string triggerName, ulong byID)
    {
        //textOptionsIndex = (textOptionsIndex + 1) % addressTextOptions.Count;
        //viewportLabel.Text = addressTextOptions[textOptionsIndex];
    }
    
}