using System.Collections.Generic;
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
    public List<ShopItemData> items;
}