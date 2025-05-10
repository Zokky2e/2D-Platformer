using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class LootItemData
{
    public List<int> itemIds;
    public int showChance;
}

[CreateAssetMenu(menuName = "Loot/LootInventory")]
public class LootInventory : ScriptableObject
{
    public List<LootItemData> itemsData;

    public List<Item> GetLoot()
    {
        List<Item> items = new List<Item>();
        float totalChance = 0f;
        foreach (var loot in itemsData)
        {
            totalChance += loot.showChance;
        }

        float randomValue = Random.Range(0f, totalChance);
        float currentChance = 0f;

        foreach (var loot in itemsData)
        {
            currentChance += loot.showChance;
            if (randomValue <= currentChance)
            {
                items = ItemDatabase.Instance.GetItemsByIds(loot.itemIds.ToArray());
            }
        }
        return items;
    }
}