using Godot;
using System.Collections.Generic;

public interface IsInventoryItem
{
    public InventoryGroupCategory category { get; set; }

    public ulong inInventoryOf { get; set; }
    public ulong equippedBySteamID { get; set; }

    public bool droppable { get; set; }

    public Node3D firstPersonScene { get; set; }
    public Node3D thirdPersonScene { get; set; }

    public void OnPickup(ulong bySteamID);
    public void OnDropped(ulong bySteamID);
    public void OnEquipped(ulong bySteamID);
    public void OnUnequipped(ulong bySteamID);
    public void HandleInput(ActionFlags actionFlags);
}
