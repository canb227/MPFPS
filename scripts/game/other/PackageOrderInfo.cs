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
    private bool isFinished;

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

    public bool IsOrderFinished()
    {
        return isFinished;
    }


}