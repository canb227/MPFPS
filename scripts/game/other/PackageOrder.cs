using Godot;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;

public class PackageOrderInfo
{
    //stuff about address, what items, what box?, etc
    private bool isFinished;

    public void OrderFinished()
    {
        isFinished = true;
        Global.gameState.gameModeManager.SetNumFinishedOrders(Global.gameState.gameModeManager.GetNumFinishedOrders()+1);
    }
}