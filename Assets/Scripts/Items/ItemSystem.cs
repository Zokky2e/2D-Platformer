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

        return ItemDatabase.Instance.GetItemsByIds(itemIds);
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