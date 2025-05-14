using Cainos.PixelArtPlatformer_VillageProps;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootChest : MonoBehaviour
{
    private Interactable interactable; // Reference to interactable component

    public Chest Chest;
    public LootInventory LootInventory;

    public List<Item> Loot;

    private void Start()
    {
        interactable = gameObject.AddComponent<Interactable>();
        interactable.onInteract = OpenChest; // Assign interaction behavior
        Loot = LootInventory.GetLoot();
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
            LootUI lootUI = FindAnyObjectByType<LootUI>();
            if (lootUI != null)
            {
                lootUI.SetLootChest(this);
                lootUI.ToggleLootInventory();
            }
        }
        yield break;
    }
}