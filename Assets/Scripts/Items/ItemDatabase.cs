using System.Collections.Generic;
using System.Linq;

public class ItemDatabase : Singleton<ItemDatabase>
{
    public List<Item> AllItems = new();

    protected override void Awake()
    {
        base.Awake();
        LoadItems();
    }

    private void LoadItems()
    {
        var loader = new ItemLoader();
        AllItems = loader.LoadItems();
    }

    public Item GetItemByName(string name)
    {
        return AllItems.FirstOrDefault(i => i.Name == name);
    }

    public List<Item> GetItemsByIds(int[] ids)
    {
        return AllItems.FindAll(item => ids.Contains(item.Id));
    }

    public Item GetItemById(int id)
    {
        return AllItems.FirstOrDefault(i => i.Id == id);
    }
}
