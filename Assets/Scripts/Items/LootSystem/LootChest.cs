using Cainos.PixelArtPlatformer_VillageProps;
using System;
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


    private void OpenChest()
    {
        Chest.IsOpened = !Chest.IsOpened;
        foreach (Item item in Loot)
        {
            Debug.Log(item.Name);
        }
    }
}