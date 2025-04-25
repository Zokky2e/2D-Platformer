using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
}
