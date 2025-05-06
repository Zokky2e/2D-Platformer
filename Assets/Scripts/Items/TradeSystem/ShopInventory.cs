using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class ShopItemData
{
    public int itemId;
    public int quantity; // or use -1 for infinite
}

[CreateAssetMenu(menuName = "Shop/ShopInventory")]
public class ShopInventory : ScriptableObject
{
    public List<ShopItemData> itemsData;

    public List<Item> items;

    public void SetItems()
    {
        items = ItemDatabase.Instance.GetItemsByIds(itemsData.Select(i => i.itemId).ToArray());
    }
}