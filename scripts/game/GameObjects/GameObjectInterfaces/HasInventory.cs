using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public interface HasInventory
{
    public Inventory inventory { get; set; }
    public IsInventoryItem equipped { get; set; }
    public void Pickup(IsInventoryItem item);
    public void EquipNext();
    public void EquipPrevious();
    public void DropEquipped();
    public void Equip(InventoryGroupCategory category,int index = 0);

}
