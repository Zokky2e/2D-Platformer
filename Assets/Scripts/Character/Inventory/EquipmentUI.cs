using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class EquipmentUI : MonoBehaviour
{
    public UIDocument uiDocument;
    private EquipmentSystem equipment;
    private VisualElement weaponSlot;
    private VisualElement armorSlot;
    private VisualElement accessorySlot;
    private Label weaponLabel;
    private Label armorLabel;
    private Label accessoryLabel;
    public Sprite defaultWeaponSprite;
    public Sprite defaultArmorSprite;
    public Sprite defaultAccessorySprite;

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
            var equipmentContainer = loadoutContainer.Q<TemplateContainer>("EquipmentContainer");
            weaponSlot = equipmentContainer.Q<VisualElement>("Weapon");
            armorSlot = equipmentContainer.Q<VisualElement>("Armor");
            accessorySlot = equipmentContainer.Q<VisualElement>("Accessory");
            weaponLabel = equipmentContainer.Q<Label>("WeaponName");
            armorLabel = equipmentContainer.Q<Label>("ArmorName");
            accessoryLabel = equipmentContainer.Q<Label>("AccessoryName");
        }

        // Listen for equipment changes
        EquipmentSystem.Instance.OnEquipmentChanged += UpdateUI;

        // Initialize UI
        UpdateUI();
    }

    private void OnDisable()
    {
        EquipmentSystem.Instance.OnEquipmentChanged -= UpdateUI;
    }

    private void UpdateUI()
    {
        UpdateSlot(weaponSlot, weaponLabel, EquipmentSystem.Instance.EquippedWeapon, defaultWeaponSprite);
        UpdateSlot(armorSlot, armorLabel, EquipmentSystem.Instance.EquippedArmor, defaultArmorSprite);
        UpdateSlot(accessorySlot, accessoryLabel, EquipmentSystem.Instance.EquippedAccessory, defaultAccessorySprite);
    }

    private void UpdateSlot(VisualElement slot, Label label, Item item, Sprite defaultSprite)
    {
        var sprite = item != null ? item.Sprite : defaultSprite;
        if (slot != null)
        {
            slot.style.backgroundImage = new StyleBackground(sprite); 
            slot.style.backgroundSize = new BackgroundSize(Length.Percent(100), Length.Percent(100)); // Fit the element

        }

        if (label != null) 
        {
            if (item != null)
            {
                label.text = item.Name;
            }
            else
            {
                label.text = string.Empty;
            }
        }


    }
}
