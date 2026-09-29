using System.Collections.Generic;

public class ItemSystem : Singleton<ItemSystem>
{

    protected override void Awake()
    {
        base.Awake();
    }

    private List<Item> GetItemsFromDatabase(int[] itemIds)
    {

        if (itemIds.Length == 0)
        {
            return new List<Item>();
        }

        List<Item> items = ItemDatabase.Instance.GetItemsByIds(itemIds);
        // Ids missing from items.json (e.g. in an older save) would put null items in the inventory
        int missing = items.RemoveAll(item => item == null);
        if (missing > 0)
            UnityEngine.Debug.LogWarning($"{missing} item id(s) not found in items.json were skipped");
        return items;
    }

    public void AddToPlayerInventory(int[] itemIds)
    {
        List<Item> items = GetItemsFromDatabase(itemIds);
        if (items.Count == 0) { return; }
        foreach (Item item in items)
        {
            InventorySystem.Instance.AddItem(item);
        }
    }

    public void AddAndEquipOnPlayer(int[] itemIds)
    {
        List<Item> items = GetItemsFromDatabase(itemIds);
        if (items.Count == 0) { return; }
        foreach (Item item in items) 
        {
            EquipmentSystem.Instance.EquipItem(item);
        }
    }

}