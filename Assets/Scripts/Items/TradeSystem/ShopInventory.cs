using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShopItemData
{
    public int itemId;
    public int quantity; // -1 for unlimited stock
}

// What a shop sells. Stock left is quantity minus what the player has bought, which is counted in
// WorldStateManager so it's saved with the game and the asset itself never changes at runtime.
[CreateAssetMenu(menuName = "Shop/ShopInventory")]
public class ShopInventory : ScriptableObject
{
    public List<ShopItemData> itemsData;

    // Items currently in stock (rebuilt by SetItems), with the entry each one came from
    [NonSerialized] public List<Item> items = new();
    [NonSerialized] private List<ShopItemData> stockEntries = new();

    public void SetItems()
    {
        items = new List<Item>();
        stockEntries = new List<ShopItemData>();
        foreach (ShopItemData entry in itemsData)
        {
            Item item = ItemDatabase.Instance.GetItemById(entry.itemId);
            if (item != null && StockLeft(entry) != 0)
            {
                items.Add(item);
                stockEntries.Add(entry);
            }
        }
    }

    // -1 means unlimited; 0 for an index that isn't in the stock (any more)
    public int StockLeft(int index) =>
        index >= 0 && index < stockEntries.Count ? StockLeft(stockEntries[index]) : 0;

    private int StockLeft(ShopItemData entry) =>
        entry.quantity < 0 ? -1 : Mathf.Max(0, entry.quantity - WorldStateManager.Instance.GetInt(BoughtKey(entry)));

    public void RecordPurchase(int index)
    {
        ShopItemData entry = stockEntries[index];
        if (entry.quantity >= 0)
        {
            string key = BoughtKey(entry);
            WorldStateManager.Instance.SetInt(key, WorldStateManager.Instance.GetInt(key) + 1);
        }
        SetItems();
    }

    private string BoughtKey(ShopItemData entry) => $"Shop_{name}_{entry.itemId}_Bought";
}
