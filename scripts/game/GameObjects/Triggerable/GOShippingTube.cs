using Godot;
using System.Collections.Generic;
using System.Linq;

[GlobalClass]
public partial class GOShippingTube : GOTrap
{
    [Export] Area3D ItemsForShipping;

    public override void _Ready()
    {
        base._Ready();
        animationPlayer.Play("shipmentFail");
    }

    public void ProcessShipping()
    {
        if (Global.Lobby.bIsLobbyHost)
        {
            // Get all overlapping bodies in the Area3D
            var bodies = ItemsForShipping.GetOverlappingBodies();
            // Filter to only GOPackageItem
            var items = bodies.OfType<GOPackageBox>().ToList();
            if (items.Count == 1)
            {
                //check if its a good package
                GOPackageBox item = items[0];
                PackageOrderInfo info = Global.gameState.gameModeManager.packageOrders[item.orderNumber];
                if (item.labelApplied && !info.waitingForDelivery && !info.isFinished)
                {
                    RPCManager.RPC(this, "PackageShipped", [item.id]);
                    return;
                }
            }

            // --- Invalid Contents: eject everything ---
            RPCManager.RPC(this, "PlayAnimation", ["shipmentFail"]);
            // Gather rigidbodies inside the area
            
            // Kick them out after half a second
            RPCManager.RPC(this, "EjectAfterDelay", []);
        }
    }
    
    public void PackageShipped(ulong itemID)
    {
        var obj = Global.gameState.GameObjects[itemID];
        if(obj is GOPackageBox item)
        {
            Global.gameState.gameModeManager.OrderReadyToShip(item.orderNumber);
            RemoveShippedItems(item.id);
            PlayAnimation("shipmentSuccess");
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void PlayAnimation(string animationName)
    {
        animationPlayer.Play(animationName);
    }
    
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    private async void EjectAfterDelay()
    {
        float delaySeconds = 0.3f;
        float ejectPower = 20f;
        var rbs = ItemsForShipping.GetOverlappingBodies()
                                                .OfType<RigidBody3D>()
                                                .ToList();

        await ToSignal(GetTree().CreateTimer(delaySeconds), SceneTreeTimer.SignalName.Timeout);

        Vector3 frontDir = GlobalTransform.Basis.Z;
        // Add upward bias
        float angleUp = Mathf.DegToRad(10f);
        Vector3 ejectDir = (frontDir * Mathf.Cos(angleUp) + Vector3.Up * Mathf.Sin(angleUp)).Normalized();

        foreach (var rb in rbs)
        {
            if (rb == null) continue;
            rb.LinearVelocity = Vector3.Zero;
            rb.ApplyCentralImpulse(ejectDir * ejectPower);
        }
    }
    
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void RemoveShippedItems(ulong itemToConsume)
    {
        GOPackageBox item = (GOPackageBox)Global.gameState.GameObjects[itemToConsume];
        item.Visible = false;
        item.packageCollision.Disabled = true;      
    }
}