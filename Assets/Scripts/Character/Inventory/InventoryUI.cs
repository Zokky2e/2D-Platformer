using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The inventory window (with the equipment panel, EquipmentUI). It keeps no copy of the inventory: every
// redraw reads InventorySystem.Instance, and it redraws on each change, when it opens and after each click.
public class InventoryUI : MonoBehaviour
{
    private bool isOpen = false;
    public UIDocument uiDocument;
    private VisualElement inventoryPanel;
    private VisualElement inventoryContainer;
    public EquipmentUI equipmentUI;
    private ScrollView items;
    private Label gold;
    private Button closeButton;
    private ItemTooltip tooltip;
    public static InventoryUI Instance { get; private set; }

    private void Awake()
    {
        // Keep the first UI; Level0 brings its own copy every time it is reloaded
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (equipmentUI == null)
            equipmentUI = GetComponentInChildren<EquipmentUI>();

        var root = uiDocument.rootVisualElement;
        inventoryPanel = root;
        inventoryContainer = root.Q<VisualElement>("InventoryContainer");
        items = inventoryContainer.Q<ScrollView>("Items");
        closeButton = root.Q<Button>("ExitButton");
        gold = root.Q<Label>("Gold");
        inventoryPanel.style.display = DisplayStyle.None;
        closeButton.clicked += ToggleInventory;
        tooltip = new ItemTooltip(inventoryContainer);
        InventorySystem.Instance.onInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (Instance == this && InventorySystem.HasInstance)
            InventorySystem.Instance.onInventoryChanged -= Refresh;
    }

    // Update is called once per frame
    void Update()
    {
        if (!PauseMenu.GameIsPaused && !isOpen && GameInput.InventoryPressed)
        {
            ToggleInventory();
        }

        if (isOpen && GameInput.CancelPressed)
        {
            ToggleInventory();
        }

    }

    private void ToggleInventory()
    {
        isOpen = inventoryPanel.style.display == DisplayStyle.None;

        inventoryPanel.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;
        if (isOpen)
            Refresh(); // Never show what was drawn before, whatever happened while the window was closed
        else
            tooltip.Hide();

        Time.timeScale = isOpen ? 0f : 1f;
        PauseMenu.GameIsPaused = isOpen;
        StartCoroutine(DelayUIFlagClear());
    }

    IEnumerator DelayUIFlagClear()
    {
        yield return null; // wait one frame
        CoreUI.IsUIOpen = isOpen;
    }

    // Redraws gold, items and equipment from the systems' current state
    public void Refresh()
    {
        if (items == null)
            return; // Not set up yet (Start)
        InventorySystem inventory = InventorySystem.Instance;
        gold.text = inventory.gold.ToString();
        items.Clear();
        List<Item> shown = new List<Item>(inventory.items); // Each slot keeps the item it was drawn with
        VisualElement grid = ItemGrid.Build(shown, 16, tooltip,
            item => item.Price + " G",
            UpdateTooltipPosition,
            (itemSlot, index) =>
            {
                itemSlot.style.backgroundColor = ItemGrid.SlotColor;
                OnItemClick(shown[index]);
            });
        items.Add(grid);
        if (equipmentUI != null)
            equipmentUI.Refresh();
    }

    private void UpdateTooltipPosition(Vector2 mousePosition)
    {
        float tooltipWidth = tooltip.Width;
        float tooltipHeight = tooltip.Height;
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float offset = 40f;
        // Default position (to the right of the cursor)
        float newX = mousePosition.x - offset;
        float newY = mousePosition.y - offset * 2;

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

    public void OnItemClick(Item item)
    {
        if (EquipmentSystem.IsEquippable(item))
            EquipmentSystem.Instance.EquipItem(item);
        else
            InventorySystem.Instance.UseItem(item);
        Refresh(); // The change events redraw too; this keeps the window right even if a listener failed
    }
}
