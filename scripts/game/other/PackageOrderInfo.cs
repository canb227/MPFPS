using Godot;
using System.Collections.Generic;
using MessagePack;

[MessagePackObject]
public class PackageOrderInfo
{
    //stuff about address, what items, what box?, etc
    [Key(0)]
    public string addressNumber;
    [Key(1)]
    public string addressStreet;
    [Key(2)]
    public string addressSuffix;
    [Key(3)]
    public List<GameObjectType> neededPackageItems;
    [Key(4)]
    public bool isFinished;
    [Key(5)]
    public bool waitingForDelivery;
    [Key(6)]
    public bool isPacked;
    public PackageOrderInfo()
    {
        
    }

    public PackageOrderInfo(string addressNumber, string addressStreet, string addressSuffix, List<GameObjectType> neededPackageItems)
    {
        this.addressNumber = addressNumber;
        this.addressStreet = addressStreet;
        this.addressSuffix = addressSuffix;
        this.neededPackageItems = neededPackageItems;
    }

    public void OrderFinished()
    {
        isFinished = true;
        Global.gameState.gameModeManager.SetNumFinishedOrders(Global.gameState.gameModeManager.GetNumFinishedOrders() + 1);
    }

    public bool OrderIsFinished()
    {
        return isFinished;
    }


}