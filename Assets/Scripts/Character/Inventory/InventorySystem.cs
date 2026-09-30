using System;
using System.Collections.Generic;

public class InventorySystem : Singleton<InventorySystem>
{
    public List<Item> items = new List<Item>(); // List of items
    public int gold = 50;
    public event Action onInventoryChanged; // Raised after items or gold change

    private static Hero Player =>
        PersistentPlayerHealth.Instance != null ? PersistentPlayerHealth.Instance.GetComponent<Hero>() : null;

    // notify: false lets a caller make several changes and raise the event once at the end (NotifyChanged)
    public void AddItem(Item newItem, bool notify = true)
    {
        items.Add(newItem);
        if (notify)
            NotifyChanged();
    }

    // Returns false if the item isn't in the inventory
    public bool RemoveItem(Item item, bool notify = true)
    {
        bool removed = items.Remove(item);
        if (removed && notify)
            NotifyChanged();
        return removed;
    }

    public void UpdateGold(int amount)
    {
        gold += amount;  //positive to add, negative to remove
        NotifyChanged();
    }

    public void UseItem(Item item)
    {
        if (!items.Contains(item))
            return; // Already used, for example clicked in a window that hadn't redrawn yet
        item.UseItem(Player.stats, Player.Health);
        RemoveItem(item); // Remove after use
    }

    public void NotifyChanged() => SafeEvent.Invoke(onInventoryChanged);
}
