using System;
using System.Collections.Generic;

public class ShopSystem : Singleton<ShopSystem>
{

    protected override void Awake()
    {
        base.Awake();
    }

    public bool SellItem(int i)
    {
        var inventorySystem = InventorySystem.Instance;

        if (i < 0 || i >= inventorySystem.items.Count)
            return false;

        var item = inventorySystem.items[i];
        if (item == null) { return false; }
        if (!item.IsSellable) { return false; }

        inventorySystem.UpdateGold((int)Math.Floor(item.Price * 0.6f));
        inventorySystem.RemoveItem(item);
        return true;
    }

    public bool BuyItem(ref ShopInventory shopInventory, int i)
    {
        if (shopInventory == null) { return false; }
        var items = shopInventory.items;
        var inventorySystem = InventorySystem.Instance;

        if (i < 0 || i >= items.Count)
            return false;

        var item = items[i];
        if (item == null) { return false; }
        if (inventorySystem.gold == 0) { return false; }
        if (inventorySystem.gold < item.Price) { return false; }

        inventorySystem.UpdateGold(-item.Price);
        inventorySystem.AddItem(item);
        return true;
    }
}