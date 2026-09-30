using System;

[AutoCreatedSingleton]
public class ShopSystem : Singleton<ShopSystem>
{
    public static int SellPrice(Item item) => (int)Math.Floor(item.Price * 0.6f);

    public bool SellItem(Item item)
    {
        var inventorySystem = InventorySystem.Instance;
        if (item == null || !item.IsSellable)
            return false;
        if (!inventorySystem.RemoveItem(item, notify: false))
            return false; // Not in the inventory any more

        inventorySystem.UpdateGold(SellPrice(item)); // Raises the change event once, after both changes
        return true;
    }

    public bool BuyItem(ShopInventory shopInventory, Item item)
    {
        if (shopInventory == null || item == null)
            return false;
        int index = shopInventory.items.IndexOf(item);
        if (index < 0)
            return false; // Sold out since the window was drawn
        var inventorySystem = InventorySystem.Instance;
        if (inventorySystem.gold < item.Price)
            return false;

        shopInventory.RecordPurchase(index);
        inventorySystem.AddItem(item, notify: false);
        inventorySystem.UpdateGold(-item.Price); // Raises the change event once, after the stock and inventory changed
        return true;
    }
}
