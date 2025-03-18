using System;
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
        // Ensure only one instance exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        player = FindAnyObjectByType<Hero>();
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
            EquippedWeapon = null;
        }
        else if (type == ItemType.Armor && EquippedArmor != null)
        {
            InventorySystem.Instance.AddItem(EquippedArmor);
            EquippedArmor = null;
        }
        else if (type == ItemType.Accessory && EquippedAccessory != null)
        {
            InventorySystem.Instance.AddItem(EquippedAccessory);
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
