using System.Collections.Generic;

public class InventorySystem : Singleton<InventorySystem>
{
    public List<Item> items = new List<Item>(); // List of items
    public int gold = 50;
    public delegate void OnInventoryChanged();
    private Hero player;
    public event OnInventoryChanged onInventoryChanged;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Awake()
    {
        player = FindAnyObjectByType<Hero>();
        base.Awake();
    }

    public void AddItem(Item newItem)
    {
        items.Add(newItem);
        onInventoryChanged?.Invoke(); // Update UI when item is added
    }

    public void RemoveItem(Item item)
    {
        items.Remove(item);
        onInventoryChanged?.Invoke(); // Update UI when item is removed
    }

    public void UpdateGold(int gold)
    {
        gold += gold;  //positive to add, negative to remove
        onInventoryChanged?.Invoke(); // Update UI when gold edited
    }

    public void UseItem(Item item) 
    {
        item.UseItem(player.stats, player.Health);
        RemoveItem(item); // Remove after use
    }
}
