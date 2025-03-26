using System;
using System.Collections;
using UnityEngine;

public class EquipmentSystem : Singleton<EquipmentSystem>
{
    public Item EquippedWeapon;
    public Item EquippedShield;
    public Item EquippedArmor;
    public Item EquippedAccessory;

    private Hero player;
    public event Action OnEquipmentChanged;

    protected override void Awake()
    {
        player = FindAnyObjectByType<Hero>();
        base.Awake();
        StartCoroutine(ApplyInitialStats());
    }

    private IEnumerator ApplyInitialStats()
    {
        while (player.stats == null && player.Health == null)
        {
            yield return null; // Wait for next frame
        }
        if (EquippedWeapon != null)
            EquippedWeapon.ApplyEffects(player.stats, player.Health);
        if (EquippedShield != null)
            EquippedShield.ApplyEffects(player.stats, player.Health);
        if (EquippedArmor != null)
            EquippedArmor.ApplyEffects(player.stats, player.Health);
        if (EquippedAccessory != null)
            EquippedAccessory.ApplyEffects(player.stats, player.Health);
    }

    public void EquipItem(Item item)
    {
        switch(item.Type)
        {
            case ItemType.Weapon:
                Swap(ref EquippedWeapon, item);
                break;
            case ItemType.Shield:
                Swap(ref EquippedShield, item);
                break;
            case ItemType.Armor:
                Swap(ref EquippedArmor, item);
                break;
            case ItemType.Accessory:
                Swap(ref EquippedAccessory, item);
                break;
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
        else if (type == ItemType.Shield && EquippedShield != null)
        {
            InventorySystem.Instance.AddItem(EquippedShield);
            equippedItem = EquippedShield;
            EquippedShield = null;
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
