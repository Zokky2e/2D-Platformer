using System;
using System.Collections.Generic;
using UnityEngine;

public class LootChest : MonoBehaviour
{
    private Interactable interactable; // Reference to interactable component

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
    }
}