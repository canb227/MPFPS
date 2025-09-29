using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class BasicInventory : Inventory
{
    public Dictionary<InventoryGroupCategory, InventoryGroup> groups { get; set; }

    public BasicInventory() 
    {
        foreach (InventoryGroupCategory category in Enum.GetValues(typeof(InventoryGroupCategory)))
        {
            groups.Add(category, new BasicInventoryGroup(category,1));
        }
    }

    public bool CanStoreItem(IsInventoryItem item)
    {
        return groups[item.category].CanStoreItem(item);
    }

    public bool CanStoreOrReplaceItem(IsInventoryItem item)
    {
        return groups[item.category].CanStoreOrReplaceItem(item);
    }

    public InventoryGroup GetGroup(InventoryGroupCategory category)
    {
        return groups[category];
    }

    public IsInventoryItem GetItem(InventoryGroupCategory category)
    {
        return groups[category].GetItem();
    }

    public bool StoreItem(IsInventoryItem item)
    {
        return groups[item.category].StoreItem(item);
    }

    public bool StoreOrReplaceItem(IsInventoryItem item, out IsInventoryItem replaced)
    {
        bool success = groups[item.category].StoreOrReplaceItem(item, out IsInventoryItem _replaced);
        replaced = _replaced;
        return success;
    }
}

public class BasicInventoryGroup : InventoryGroup
{
    public InventoryGroupCategory category { get; set; }
    public int maxItems { get; set; }
    public List<IsInventoryItem> items { get; set; }
    
    public BasicInventoryGroup(InventoryGroupCategory category, int maxItems)
    {
        this.category = category;
        this.maxItems = maxItems;
    }

    public bool CanStoreItem(IsInventoryItem item)
    {
        if (item.category!=category) return false;
        if (items.Count>=maxItems) return false;
        return true;
    }

    public bool CanStoreOrReplaceItem(IsInventoryItem item)
    {
        if (item.category != category) return false;
        return true;
    }

    public IsInventoryItem GetItem()
    {
        if (maxItems>1)
        {
            Logging.Warn($"Use of GetItem() not recommended when using multislotted inventory groups!","Inventory");
        }
        return items[0];
    }

    public IsInventoryItem GetItemAt(int index)
    {
        return items[index];
    }

    public IsInventoryItem RemoveItem()
    {
        if (maxItems > 1)
        {
            Logging.Warn($"Use of RemoveItem() not recommended when using multislotted inventory groups!", "Inventory");
        }
        return RemoveItemAt(0);
    }

    public IsInventoryItem RemoveItemAt(int index)
    {
        return RemoveItemAt(index);
    }

    public bool StoreItem(IsInventoryItem item)
    {
        if (!CanStoreItem(item))
        {
            Logging.Error($"Cannot store this item!", "Invetory");
            return false;
        }
        else
        {
            items.Add(item);
            return true;
        }
    }

    public bool StoreOrReplaceItem(IsInventoryItem item, out IsInventoryItem replaced)
    {
        if (!CanStoreOrReplaceItem(item))
        {
            Logging.Error($"Cannot store or replace this item!", "Invetory");
            replaced = null;
            return false;
        }
        else
        {
            if (items.Count==maxItems)
            {
                for (int i = 0;i<items.Count;i++)
                {
                    if (items[i].droppable)
                    {
                        replaced = items[i];
                        StoreItem(item);
                        return true;
                    }
                }
                replaced = null;
                return false;
            }
            else
            {
                StoreItem(item);
                replaced = null;
                return true;
            }
        }
    }
}

