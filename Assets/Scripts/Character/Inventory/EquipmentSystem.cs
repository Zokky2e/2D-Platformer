using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public class EquipmentSystem : MonoBehaviour
{
    public static EquipmentSystem Instance;
    public Item EquippedWeapon;
    public Item EquippedArmor;
    public Item EquippedAccessory;

    public Hero player;
    public event Action OnEquipmentChanged;

    private void Awake()
    {
        player = FindAnyObjectByType<Hero>();
        // Ensure only one instance exists
        if (Instance == null)
        {
            Instance = this;
            StartCoroutine(ApplyInitialStats());
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private IEnumerator ApplyInitialStats()
    {
        while (player.stats == null && player.Health == null)
        {
            yield return null; // Wait for next frame
        }
        if (EquippedWeapon != null)
            EquippedWeapon.ApplyEffects(player.stats, player.Health);
        if (EquippedArmor != null)
            EquippedArmor.ApplyEffects(player.stats, player.Health);
        if (EquippedAccessory != null)
            EquippedAccessory.ApplyEffects(player.stats, player.Health);
    }

    public void EquipItem(Item item)
    {
        if (item.Type == ItemType.Weapon)
        {
            Swap(ref EquippedWeapon, item);
        }
        else if (item.Type == ItemType.Armor)
        {
            Swap(ref EquippedArmor, item);
        }
        else if (item.Type == ItemType.Accessory)
        {
            Swap(ref EquippedAccessory, item);
        }
        item.ApplyEffects(player.stats, player.Health);
    }

    public void UnequipItem(ItemType type)
    {
        Item equippedItem = null;
        if (type == ItemType.Weapon && EquippedWeapon != null)
        {
            InventorySystem.Instance.AddItem(EquippedWeapon);
            equippedItem = EquippedWeapon;
            EquippedWeapon = null;
        }
        else if (type == ItemType.Armor && EquippedArmor != null)
        {
            InventorySystem.Instance.AddItem(EquippedArmor);
            equippedItem = EquippedArmor;
            EquippedArmor = null;
        }
        else if (type == ItemType.Accessory && EquippedAccessory != null)
        {
            InventorySystem.Instance.AddItem(EquippedAccessory);
            equippedItem = EquippedAccessory;
            EquippedAccessory = null;
        }

        OnEquipmentChanged?.Invoke();
        if (equippedItem != null)
        {
            equippedItem.RemoveEffects(player.stats, player.Health);
        }
    }

    private void Swap(ref Item equippedSlot, Item newItem)
    {
        if (equippedSlot != null)
        {
            InventorySystem.Instance.AddItem(equippedSlot);
        }
        equippedSlot = newItem;
        InventorySystem.Instance.RemoveItem(newItem);
        OnEquipmentChanged?.Invoke();
    }
}
