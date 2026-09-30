using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The equipment panel of the inventory window. Every EquipmentSlot has an element with the same name in
// EquipmentUI.uxml, whose background image is the empty-slot icon. A filled slot shows the item's icon and
// tooltip, and clicking it unequips the item. The callbacks are registered once and look up the slot's item
// when they fire, so they can never act on an item the slot no longer holds.
public class EquipmentUI : MonoBehaviour
{
    public UIDocument uiDocument;
    private readonly Dictionary<EquipmentSlot, VisualElement> slots = new Dictionary<EquipmentSlot, VisualElement>();
    private readonly Dictionary<EquipmentSlot, StyleBackground> emptyIcons = new Dictionary<EquipmentSlot, StyleBackground>();
    private ItemTooltip tooltip;
    private bool isSetUp;

    private void Start()
    {
        var root = uiDocument.rootVisualElement;
        VisualElement equipmentContainer = root.Q<VisualElement>("Loadout")?.Q<TemplateContainer>("EquipmentContainer");
        if (equipmentContainer == null)
        {
            Debug.LogError("EquipmentUI: the inventory UXML has no Loadout/EquipmentContainer");
            return;
        }
        tooltip = new ItemTooltip(equipmentContainer);

        foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
        {
            VisualElement element = equipmentContainer.Q<VisualElement>(slot.ToString());
            if (element == null)
            {
                Debug.LogError($"EquipmentUI: EquipmentUI.uxml has no element named '{slot}'");
                continue;
            }
            slots[slot] = element;
            emptyIcons[slot] = element.style.backgroundImage; // The icon set in the UXML
            EquipmentSlot captured = slot;
            element.RegisterCallback<MouseEnterEvent>(evt => OnSlotEnter(captured, evt.mousePosition));
            element.RegisterCallback<MouseMoveEvent>(evt => UpdateTooltipPosition(evt.mousePosition));
            element.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                element.style.backgroundColor = ItemGrid.SlotColor;
                tooltip.Hide();
            });
            element.RegisterCallback<ClickEvent>(evt => OnSlotClick(captured));
        }

        EquipmentSystem.Instance.OnEquipmentChanged += Refresh;
        isSetUp = true;
        Refresh();
    }

    private void OnDestroy()
    {
        if (isSetUp && EquipmentSystem.HasInstance)
            EquipmentSystem.Instance.OnEquipmentChanged -= Refresh;
    }

    // Shows each slot's item, or its empty-slot icon
    public void Refresh()
    {
        if (!isSetUp)
            return;
        foreach (KeyValuePair<EquipmentSlot, VisualElement> pair in slots)
        {
            Item item = EquipmentSystem.Instance.GetItem(pair.Key);
            VisualElement slot = pair.Value;
            slot.style.backgroundImage = item != null ? new StyleBackground(item.Sprite) : emptyIcons[pair.Key];
            slot.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100)); // Fit the element
            slot.style.backgroundColor = ItemGrid.SlotColor;
        }
    }

    private void OnSlotEnter(EquipmentSlot slot, Vector2 mousePosition)
    {
        Item item = EquipmentSystem.Instance.GetItem(slot);
        if (item == null)
            return;
        slots[slot].style.backgroundColor = ItemGrid.HoverColor;
        tooltip.Show(item, item.Price + " G");
        UpdateTooltipPosition(mousePosition);
    }

    private void OnSlotClick(EquipmentSlot slot)
    {
        tooltip.Hide();
        EquipmentSystem.Instance.UnequipItem(slot);
        // The change events redraw both panels; this keeps them right even if a listener failed
        if (InventoryUI.Instance != null)
            InventoryUI.Instance.Refresh();
        else
            Refresh();
    }

    private void UpdateTooltipPosition(Vector2 mousePosition)
    {
        float tooltipWidth = tooltip.Width;
        float tooltipHeight = tooltip.Height;

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float offset = 100f;
        // Default position (to the right of the cursor)
        float newX = mousePosition.x - offset * 2;
        float newY = mousePosition.y - offset;

        // Check right boundary
        if (newX + tooltipWidth > screenWidth)
        {
            newX = mousePosition.x - tooltipWidth - offset * 3; // Move to the left
        }

        // Check bottom boundary
        if (newY + tooltipHeight > screenHeight)
        {
            newY = mousePosition.y - tooltipHeight - offset * 3; // Move up
        }

        tooltip.MoveTo(newX, newY);

    }
}
