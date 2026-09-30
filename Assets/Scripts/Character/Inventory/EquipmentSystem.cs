using System;
using System.Collections.Generic;
using System.Linq;

// Equipment slots, in display and save order. Each has an element with the same name in EquipmentUI.uxml.
// Add new slots at the end.
public enum EquipmentSlot
{
    Weapon,
    Shield,
    Helmet,
    Armor,
    Gloves,
    Accessory1,
    Accessory2
}

// What the hero wears. Items move between the inventory and the slots only through EquipItem and
// UnequipItem, which also apply and remove the item's effects, so the stats always match what is worn.
public class EquipmentSystem : Singleton<EquipmentSystem>
{
    private readonly Dictionary<EquipmentSlot, Item> equipped = new Dictionary<EquipmentSlot, Item>();
    public event Action OnEquipmentChanged;

    private static Hero Player =>
        PersistentPlayerHealth.Instance != null ? PersistentPlayerHealth.Instance.GetComponent<Hero>() : null;

    public Item GetItem(EquipmentSlot slot) => equipped.TryGetValue(slot, out Item item) ? item : null;

    // Everything worn, in slot order
    public IEnumerable<Item> EquippedItems =>
        ((EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot))).Select(GetItem).Where(item => item != null);

    public static bool IsEquippable(Item item) => item != null && item.Type != ItemType.Consumable;

    public static bool IsTwoHanded(Item item) =>
        item != null && item.Type == ItemType.Weapon && WeaponProfile.For(item.WeaponType).TwoHanded;

    // The slot an item goes into. An accessory takes a free accessory slot, or replaces the first one
    public EquipmentSlot SlotFor(Item item)
    {
        switch (item.Type)
        {
            case ItemType.Weapon: return EquipmentSlot.Weapon;
            case ItemType.Shield: return EquipmentSlot.Shield;
            case ItemType.Helmet: return EquipmentSlot.Helmet;
            case ItemType.Armor: return EquipmentSlot.Armor;
            case ItemType.Gloves: return EquipmentSlot.Gloves;
            case ItemType.Accessory:
                if (GetItem(EquipmentSlot.Accessory1) == null)
                    return EquipmentSlot.Accessory1;
                if (GetItem(EquipmentSlot.Accessory2) == null)
                    return EquipmentSlot.Accessory2;
                return EquipmentSlot.Accessory1;
            default:
                throw new ArgumentException($"{item.Name} ({item.Type}) can't be equipped");
        }
    }

    // Moves an item from the inventory into its slot. Whatever was in the slot goes back to the inventory with
    // its effects removed, then the new item's effects are applied. An item that isn't in the inventory is
    // ignored, so a click on an out-of-date window can't equip, and apply, the same item twice.
    public void EquipItem(Item item)
    {
        InventorySystem inventory = InventorySystem.Instance;
        if (!IsEquippable(item) || !inventory.RemoveItem(item, notify: false))
            return;
        EquipmentSlot slot = SlotFor(item);
        MoveToInventory(slot, inventory);
        // Two-handed weapons and shields exclude each other: equipping one puts the other back
        if (IsTwoHanded(item))
            MoveToInventory(EquipmentSlot.Shield, inventory);
        else if (item.Type == ItemType.Shield && IsTwoHanded(GetItem(EquipmentSlot.Weapon)))
            MoveToInventory(EquipmentSlot.Weapon, inventory);
        equipped[slot] = item;
        item.ApplyEffects(Player.stats, Player.Health);
        NotifyChanged(inventory);
    }

    public void UnequipItem(EquipmentSlot slot)
    {
        if (GetItem(slot) == null)
            return;
        MoveToInventory(slot, InventorySystem.Instance);
        NotifyChanged(InventorySystem.Instance);
    }

    // Empties a slot into the inventory, removing the item's effects; the caller raises the events
    private void MoveToInventory(EquipmentSlot slot, InventorySystem inventory)
    {
        Item item = GetItem(slot);
        if (item == null)
            return;
        equipped.Remove(slot);
        item.RemoveEffects(Player.stats, Player.Health);
        inventory.AddItem(item, notify: false);
    }

    // Listeners hear about the change once the slots, the inventory and the stats all agree
    private void NotifyChanged(InventorySystem inventory)
    {
        inventory.NotifyChanged();
        SafeEvent.Invoke(OnEquipmentChanged);
    }
}
