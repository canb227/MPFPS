using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class Hands : GOBaseInventoryItem
{
    public override InventoryGroupCategory category { get; set; } = InventoryGroupCategory.Hands; 
    public override bool droppable { get; set; } = false;

    public override void HandleInput(ActionFlags actionFlags)
    {
        
    }

    public override void OnDropped(ulong byID)
    {
        base.OnDropped(byID);
    }

    public override void OnEquipped(ulong byID)
    {
        base.OnEquipped(byID);
    }

    public override void OnPickup(ulong byID)
    {
        base.OnPickup(byID);
    }

    public override void OnUnequipped(ulong byID)
    {
        base.OnUnequipped(byID) ;
    }

}

