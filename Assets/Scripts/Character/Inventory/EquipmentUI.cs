using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class EquipmentUI : MonoBehaviour
{
    public UIDocument uiDocument;
    private EquipmentSystem equipment;
    private TemplateContainer equipmentContainer;
    private VisualElement weaponSlot;
    private VisualElement shieldSlot;
    private VisualElement armorSlot;
    private VisualElement accessorySlot;
    public Sprite defaultWeaponSprite;
    public Sprite defaultShieldSprite;
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
            yield return null; // Wait for next frame
        }
        equipment = EquipmentSystem.Instance; // Find inventory
        equipment.OnEquipmentChanged += () =>
        {
            UpdateUI(); // Listen for changes
        };
        var root = uiDocument.rootVisualElement;

        // Find the "Loadout" VisualElement
        var loadoutContainer = root.Q<VisualElement>("Loadout");

        if (loadoutContainer != null)
        {
            equipmentContainer = loadoutContainer.Q<TemplateContainer>("EquipmentContainer");
            weaponSlot = equipmentContainer.Q<VisualElement>("Weapon");
            shieldSlot = equipmentContainer.Q<VisualElement>("Shield");
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
        if (EquipmentSystem.Instance != null) 
        { 
            EquipmentSystem.Instance.OnEquipmentChanged -= UpdateUI;
        }
    }

    private void UpdateUI()
    {
        UpdateSlot(weaponSlot, EquipmentSystem.Instance.EquippedWeapon, defaultWeaponSprite);
        UpdateSlot(shieldSlot, EquipmentSystem.Instance.EquippedShield, defaultShieldSprite);
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

        slot.UnregisterCallback<MouseEnterEvent, Item>(OnMouseEnter);
        slot.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        slot.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
        slot.UnregisterCallback<ClickEvent, Item>(OnItemClick);
        if (item != null)
        {
            slot.RegisterCallback<MouseEnterEvent, Item>(OnMouseEnter, item); // Pass item via lambda
            slot.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            slot.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            slot.RegisterCallback<ClickEvent, Item>(OnItemClick, item);
        }
        else
        {
            // If no item, reset the slot background
            slot.style.backgroundColor = new Color(0, 0, 0, 0.1f);
        }
    }

    private void OnEquipedItemClicked(VisualElement slot, Item item)
    {
        if (item != null) 
        {
            equipment.UnequipItem(item.Type);
        }
    }
    private void OnMouseEnter(MouseEnterEvent evt, Item item)
    {
        var slot = evt.target as VisualElement;
        slot.style.backgroundColor = new Color(1, 1, 1, 0.3f); // Lighten background on hover
        item.AdjustDescription(); // Ensure description updates dynamically
        tooltipName.text = item.Name;
        tooltipDescription.text = item.Description;
        tooltip.style.visibility = Visibility.Visible;
        UpdateTooltipPosition(evt.mousePosition);
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        UpdateTooltipPosition(evt.mousePosition);
    }

    private void OnMouseLeave(MouseLeaveEvent evt)
    {
        var slot = evt.target as VisualElement;
        slot.style.backgroundColor = new Color(0, 0, 0, 0.1f); // Reset background when leaving

        tooltip.style.visibility = Visibility.Hidden;
    }

    private void OnItemClick(ClickEvent evt, Item item)
    {
        var slot = evt.target as VisualElement;
        slot.style.backgroundColor = new Color(0, 0, 0, 0.1f); // Reset background after clicking

        tooltip.style.visibility = Visibility.Hidden;
        OnEquipedItemClicked(slot, item);
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
