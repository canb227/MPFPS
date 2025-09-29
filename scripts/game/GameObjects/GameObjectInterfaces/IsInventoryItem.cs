using Godot;
using System.Collections.Generic;

public interface IsInventoryItem
{
    public InventoryGroupCategory category { get; set; }

    public ulong inInventoryOf { get; set; }
    public ulong equippedBy { get; set; }

    public bool droppable { get; set; }

    public Node3D firstPersonScene { get; set; }
    public Node3D thirdPersonScene { get; set; }

    public void OnPickup(ulong byID);
    public void OnDropped(ulong byID);
    public void OnEquipped(ulong byID);
    public void OnUnequipped(ulong byID);
    public void HandleInput(ActionFlags actionFlags);
}
