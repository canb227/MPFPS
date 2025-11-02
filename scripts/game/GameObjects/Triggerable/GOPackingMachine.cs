using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class GOPackingMachine : GOTrap
{
    [Export] Area3D ItemsForPackingArea;
    [Export] Marker3D PackageOutputMarker;

    public override void _Ready()
    {
        base._Ready();
        animationPlayer.Play("packageFailed");
    }


    public bool AttemptPacking()
    {
        if (Global.Lobby.bIsLobbyHost)
        {
            // Get all overlapping bodies in the Area3D
            var bodies = ItemsForPackingArea.GetOverlappingBodies();
            // Filter to only GOPackageItem
            var items = bodies.OfType<GOPackageItem>().ToList();
            if (items.Count != 0)
            {
                //CheckContents(items);
                if (CheckExactContents(items))
                {
                    return true;
                }
            }

            // --- No order matched: eject everything ---
            RPCManager.RPC(this, "PlayAnimation", ["packageFailed"]);

            // Gather rigidbodies inside the area
            var rigidBodies = ItemsForPackingArea.GetOverlappingBodies()
                                                .OfType<RigidBody3D>()
                                                .ToList();

            // Kick them out after half a second
            EjectAfterDelay(rigidBodies, this, delaySeconds: 0.3f, ejectPower: 20f);

            return false;

        }
        return false;
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void PlayAnimation(string animationName)
    {
        animationPlayer.Play(animationName);
    }

    //we call this as lobby host
    private async void EjectAfterDelay(List<RigidBody3D> rbs, Node3D machine, float delaySeconds = 0.3f, float ejectPower = 20f)
    {
        await ToSignal(GetTree().CreateTimer(delaySeconds), SceneTreeTimer.SignalName.Timeout);

        Vector3 frontDir = GlobalTransform.Basis.Z;
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
    public void RemovePackedItems(List<ulong> itemsToConsume)
    {
        foreach (ulong id in itemsToConsume)
        {
            GOPackageItem item = (GOPackageItem)Global.gameState.GameObjects[id];
            item.Visible = false;
            item.packageItemCollider.Disabled = true;      
        }
    }
    
    private bool CheckExactContents(List<GOPackageItem> items)
    {
        // Collect the item types present
        var presentTypes = items.Select(i => i.itemType).ToList();

        for (int orderNumber = 0; orderNumber < Global.gameState.gameModeManager.packageOrders.Count; orderNumber++)
        {
            var order = Global.gameState.gameModeManager.packageOrders[orderNumber];

            // --- NEW: Check counts first ---
            if (presentTypes.Count != order.neededPackageItems.Count)
                continue; // can't be an exact match if counts differ

            // --- NEW: Check exact match of types (ignoring order) ---
            var needed = new List<GameObjectType>(order.neededPackageItems);
            bool exactMatch = true;

            foreach (var type in presentTypes)
            {
                if (needed.Contains(type))
                {
                    needed.Remove(type); // consume one
                }
                else
                {
                    exactMatch = false; // found an extra item
                    break;
                }
            }

            if (exactMatch && needed.Count == 0 && !Global.gameState.gameModeManager.packageOrders[orderNumber].isPacked)
            {
                // ✅ Exact match found
                var itemsToConsume = items.Select(i => i.id).ToList();

                RPCManager.RPC(this, "RemovePackedItems", [itemsToConsume.ToList()]);

                GameObjectConstructorData data = new(GameObjectType.Package);
                data.paramList.Add(orderNumber);
                data.spawnTransform = PackageOutputMarker.GlobalTransform;
                Global.gameState.Auth_SpawnObject(GameObjectType.Package, data);
                RPCManager.RPC(Global.gameState.gameModeManager, "OrderPacked", [orderNumber]);
                RPCManager.RPC(this, "PlayAnimation", ["packageCreated"]);
                
                return true;
            }
        }
        return false;
    }
}
