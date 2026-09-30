using Cainos.PixelArtPlatformer_VillageProps;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LootChest : MonoBehaviour
{
    private Interactable interactable; // Reference to interactable component

    public Chest Chest;
    public LootInventory LootInventory;

    public List<Item> Loot;

    // Chests placed in a scene keep their contents between visits and saves (in WorldStateManager);
    // chests in generated dungeon rooms are new every run and roll fresh loot
    private string persistenceKey;

    private void Start()
    {
        interactable = gameObject.AddComponent<Interactable>();
        interactable.onInteract = OpenChest; // Assign interaction behavior

        if (GetComponentInParent<Room>() == null)
            persistenceKey = $"Chest_{gameObject.scene.name}_{Mathf.RoundToInt(transform.position.x)}_{Mathf.RoundToInt(transform.position.y)}";
        string saved = persistenceKey != null ? WorldStateManager.Instance.GetString(persistenceKey) : null;
        if (saved != null)
        {
            int[] ids = saved.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
            Loot = ItemDatabase.Instance.GetItemsByIds(ids).Where(item => item != null).ToList();
            if (Loot.Count == 0)
                Chest.IsOpened = true; // Emptied on an earlier visit
        }
        else
        {
            Loot = LootInventory.GetLoot();
            SaveContents(); // So coming back to the scene doesn't roll new loot
        }
    }

    // Records what's left in a placed chest; LootUI calls this when items are taken
    public void SaveContents()
    {
        if (persistenceKey != null)
            WorldStateManager.Instance.SetString(persistenceKey, string.Join(",", Loot.Select(item => item.Id)));
    }

    public void CloseChest()
    {
        Chest.IsOpened = false;
    }

    private void OpenChest()
    {
        Chest.IsOpened = true;
        StartCoroutine(OpenChestCoroutine());
    }

    private IEnumerator OpenChestCoroutine()
    {
        if (Chest.IsOpened) 
        {
            yield return null;
        }
        if (Loot != null && Loot.Count > 0)
        {
            yield return new WaitForSeconds(0.5f);
            LootUI lootUI = LootUI.Instance;
            if (lootUI != null)
            {
                lootUI.SetLootChest(this);
                lootUI.ToggleLootInventory();
            }
        }
        yield break;
    }
}