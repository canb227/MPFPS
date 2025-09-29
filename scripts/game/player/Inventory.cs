using System.Collections.Generic;

public interface Inventory
{
    public Dictionary<InventoryGroupCategory,InventoryGroup> groups { get; set; }

    public InventoryGroup GetGroup(InventoryGroupCategory category);
    public IsInventoryItem GetItem(InventoryGroupCategory category);

    public bool CanStoreItem(IsInventoryItem item);
    public bool CanStoreOrReplaceItem(IsInventoryItem item);

    public bool StoreItem(IsInventoryItem item);
    public bool StoreOrReplaceItem(IsInventoryItem item, out IsInventoryItem replaced);
}
