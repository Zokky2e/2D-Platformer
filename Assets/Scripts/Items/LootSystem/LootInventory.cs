using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class LootItemData
{
    public List<int> itemIds;
    public int showChance; // Weight against the other groups
    public int minDungeonLevel; // Only rolled from this dungeon level on; 0 also before the first dungeon
}

[CreateAssetMenu(menuName = "Loot/LootInventory")]
public class LootInventory : ScriptableObject
{
    public List<LootItemData> itemsData;

    // Rolls one group of items, weighted by showChance, among the groups unlocked at the current dungeon level
    public List<Item> GetLoot()
    {
        int dungeonLevel = DungeonManager.Instance.DungeonLevel;
        List<LootItemData> available = itemsData.Where(loot => dungeonLevel >= loot.minDungeonLevel).ToList();
        float totalChance = available.Sum(loot => loot.showChance);

        float randomValue = Random.Range(0f, totalChance);
        float currentChance = 0f;

        foreach (var loot in available)
        {
            currentChance += loot.showChance;
            if (randomValue <= currentChance)
            {
                // First group that covers the roll wins
                return ItemDatabase.Instance.GetItemsByIds(loot.itemIds.ToArray()).Where(item => item != null).ToList();
            }
        }
        return new List<Item>();
    }
}
