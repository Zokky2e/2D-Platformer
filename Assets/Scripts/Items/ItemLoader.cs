using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ItemLoader
{
    public ItemLoader()
    {
        
    }
    Item CreateRuntimeItem(ItemData data)
    {
        Item item = ScriptableObject.CreateInstance<RuntimeItem>();
        item.name = data.name;

        var so = (RuntimeItem)item;
        so.SetData(data);
        return item;
    }

    public List<Item> LoadItems()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "items.json");
        string json = File.ReadAllText(path);

        var itemContainer = JsonConvert.DeserializeObject<ItemContainer>(json);
        List<Item> loadedItems = new();
        foreach (var data in itemContainer.items)
        {
            Item item = CreateRuntimeItem(data);
            loadedItems.Add(item);
        }

        return loadedItems;
    }
}

[System.Serializable]
public class ItemContainer
{
    public List<ItemData> items;
}
