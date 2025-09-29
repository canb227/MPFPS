using System.Collections.Generic;

public interface InventoryGroup
{
    public InventoryGroupCategory category { get; set; }
    public int maxItems { get; set; }
    public List<IsInventoryItem> items { get; set; }

    public IsInventoryItem GetItem();
    public IsInventoryItem GetItemAt(int index);

    public bool CanStoreItem(IsInventoryItem item);
    public bool CanStoreOrReplaceItem(IsInventoryItem item);

    public bool StoreItem(IsInventoryItem item);
    public bool StoreOrReplaceItem(IsInventoryItem item, out IsInventoryItem replaced);

    public IsInventoryItem RemoveItem();
    public IsInventoryItem RemoveItemAt(int index);
}
