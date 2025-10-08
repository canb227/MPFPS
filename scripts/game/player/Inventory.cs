using System;
using System.Collections.Generic;
public enum InventoryGroupCategory
{
    None,
    Hands,
    Weapon,
    Tool,
    Role,
    Special,
}

public class Inventory
{
    public List<InventoryGroup> groups { get; set; } = new();

    public Inventory()
    {
        foreach (InventoryGroupCategory category in Enum.GetValues(typeof(InventoryGroupCategory)))
        {
            groups.Add(new InventoryGroup(category, 1));
        }
    }

    public InventoryGroup GetGroup(InventoryGroupCategory category)
    {
        foreach (var group in groups)
        {
            if (group.category == category)
            {
                return group;
            }
        }
        return null;
    }

    public bool HasGroup (InventoryGroupCategory category)
    {
        foreach (var group in groups)
        {
            if (group.category == category)
            {
                return true;
            }
        }
        return false;
    }

    public int GetIndex(InventoryGroupCategory currentCategory)
    {
        for (int i = 0; i < groups.Count; i++)
        {
            if (groups[i].category == currentCategory)
            {
                return i;
            }
        }
        return -1;
    }

    public int GetNextIndex(InventoryGroupCategory currentCategory)
    {
        int index = GetIndex(currentCategory);
        while (groups[index] == null || groups[index].items.Count<=0)
        {
            index += 1;
            index %= groups.Count;
        }
        return index;
    }

}

public class InventoryGroup
{
    public InventoryGroupCategory category { get; set; }
    public int maxItems { get; set; } = 1;
    public List<IsInventoryItem> items { get; set; } = new();

    public InventoryGroup(InventoryGroupCategory category, int maxItems = 1)
    {
        this.category = category;
        this.maxItems = maxItems;
    }

    public bool CanStoreItem(IsInventoryItem item)
    {
        if (item.category != category) return false;
        if (items.Count >= maxItems) return false;
        return true;
    }

    public bool CanStoreOrReplaceItem(IsInventoryItem item)
    {
        if (item.category != category) return false;
        return true;
    }

    public IsInventoryItem GetItem()
    {
        if (maxItems > 1)
        {
            Logging.Warn($"Use of GetItem() not recommended when using multislotted inventory groups!", "Inventory");
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
            if (items.Count == maxItems)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].droppable)
                    {
                        replaced = items[i];
                        items[i] = item;
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