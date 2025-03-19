using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class EquipmentUI : MonoBehaviour
{
    public UIDocument uiDocument;
    private EquipmentSystem equipment;
    private TemplateContainer equipmentContainer;
    private VisualElement weaponSlot;
    private VisualElement armorSlot;
    private VisualElement accessorySlot;
    public Sprite defaultWeaponSprite;
    public Sprite defaultArmorSprite;
    public Sprite defaultAccessorySprite;
    private VisualElement tooltip;
    private Label tooltipName;
    private Label tooltipDescription;

    private void OnEnable()
    {
        StartCoroutine(WaitForEquipmentSystem());
    }

    private IEnumerator WaitForEquipmentSystem()
    {
        // Wait until the InventorySystem instance is ready
        while (EquipmentSystem.Instance == null)
        {
            Debug.Log("Equipment is null");
            yield return null; // Wait for next frame
        }
        Debug.Log("Equipment is not null");
        equipment = EquipmentSystem.Instance; // Find inventory
        equipment.OnEquipmentChanged += () =>
        {
            Debug.Log("Equipment UI Updated!");
            UpdateUI(); // Listen for changes
        };
        var root = uiDocument.rootVisualElement;

        // Find the "Loadout" VisualElement
        var loadoutContainer = root.Q<VisualElement>("Loadout");

        if (loadoutContainer != null)
        {
            equipmentContainer = loadoutContainer.Q<TemplateContainer>("EquipmentContainer");
            weaponSlot = equipmentContainer.Q<VisualElement>("Weapon");
            armorSlot = equipmentContainer.Q<VisualElement>("Armor");
            accessorySlot = equipmentContainer.Q<VisualElement>("Accessory");
        }

        // Listen for equipment changes
        EquipmentSystem.Instance.OnEquipmentChanged += UpdateUI;

        // Initialize UI
        SetupTooltip(); // Initialize tooltip setups
        UpdateUI();
    }

    private void OnDisable()
    {
        EquipmentSystem.Instance.OnEquipmentChanged -= UpdateUI;
    }

    private void UpdateUI()
    {
        UpdateSlot(weaponSlot, EquipmentSystem.Instance.EquippedWeapon, defaultWeaponSprite);
        UpdateSlot(armorSlot, EquipmentSystem.Instance.EquippedArmor, defaultArmorSprite);
        UpdateSlot(accessorySlot, EquipmentSystem.Instance.EquippedAccessory, defaultAccessorySprite);
    }

    private void UpdateSlot(VisualElement slot, Item item, Sprite defaultSprite)
    {
        var sprite = item != null ? item.Sprite : defaultSprite;
        if (slot != null)
        {
            slot.style.backgroundImage = new StyleBackground(sprite); 
            slot.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100)); // Fit the element

        }

        if (item != null)
        {
            slot.RegisterCallback<MouseEnterEvent>(evt =>
            {
                slot.style.backgroundColor = new Color(1, 1, 1, 0.3f); // Lighten background on hover
                item.AdjustDescription();
                tooltipName.text = item.Name;
                tooltipDescription.text = item.Description;
                tooltip.style.visibility = Visibility.Visible;
                UpdateTooltipPosition(evt.mousePosition); // Update tooltip position
            });

            slot.RegisterCallback<MouseMoveEvent>(evt =>
            {
                UpdateTooltipPosition(evt.mousePosition);
            });
            slot.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                slot.style.backgroundColor = new Color(0, 0, 0, 0.1f); // Light transparent slot background
                tooltip.style.visibility = Visibility.Hidden;
            });
            slot.RegisterCallback<ClickEvent>(evt =>
            {
                slot.style.backgroundColor = new Color(0, 0, 0, 0.1f);
                tooltip.style.visibility = Visibility.Hidden;
                OnEquipedItemClicked(slot, item);
            });
        }
        else
        {
            slot.UnregisterCallback<MouseEnterEvent>(evt => { });
            slot.UnregisterCallback<MouseMoveEvent>(evt => { });
            slot.UnregisterCallback<MouseLeaveEvent>(evt => { });
            slot.UnregisterCallback<ClickEvent>(evt => { });
        }
    }

    private void OnEquipedItemClicked(VisualElement slot, Item item)
    {
        if (item != null) 
        {
            equipment.UnequipItem(item.Type);
        }
    }

    private void SetupTooltip()
    {
        tooltip = new Label();
        tooltip.style.position = Position.Absolute;
        tooltip.style.backgroundColor = new Color(0, 0, 0, 0.8f);
        tooltip.style.color = Color.white;
        tooltip.style.paddingLeft = 10;
        tooltip.style.paddingRight = 10;
        tooltip.style.paddingTop = 5;
        tooltip.style.paddingBottom = 5;
        tooltip.style.fontSize = 24;
        tooltip.style.maxWidth = 500;
        tooltip.style.visibility = Visibility.Hidden;

        // Create the item name label
        tooltipName = new Label();
        tooltipName.style.unityFontStyleAndWeight = FontStyle.Bold;
        tooltipName.style.fontSize = 28;
        tooltipName.style.color = Color.white;
        tooltipName.style.marginBottom = 5; // Space between name and description
        tooltipName.style.whiteSpace = WhiteSpace.Normal;
        tooltipName.style.overflow = Overflow.Hidden;
        tooltipName.style.textOverflow = TextOverflow.Clip;

        // Create the item description label
        tooltipDescription = new Label();
        tooltipDescription.style.fontSize = 22;
        tooltipDescription.style.color = Color.white;
        tooltipDescription.style.whiteSpace = WhiteSpace.Normal;
        tooltipDescription.style.overflow = Overflow.Hidden;
        tooltipDescription.style.textOverflow = TextOverflow.Clip;

        // Add labels to the tooltip container
        tooltip.Add(tooltipName);
        tooltip.Add(tooltipDescription);

        equipmentContainer.Add(tooltip); // Add tooltip to the inventory UI
    }

    private void UpdateTooltipPosition(Vector2 mousePosition)
    {
        float tooltipWidth = tooltip.resolvedStyle.width;
        float tooltipHeight = tooltip.resolvedStyle.height;
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

        // Apply position
        tooltip.style.left = newX;
        tooltip.style.top = newY;
    }
}
