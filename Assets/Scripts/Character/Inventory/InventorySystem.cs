using System.Collections.Generic;
using UnityEngine;
public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance; // Singleton
    public List<Item> items = new List<Item>(); // List of items
    public delegate void OnInventoryChanged();
    public event OnInventoryChanged onInventoryChanged;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    private void Awake()
    {
        // Ensure only one instance exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }
    // Update is called once per frame
    void Update()
    {
        
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
}
