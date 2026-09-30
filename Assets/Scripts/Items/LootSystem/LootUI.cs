using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The loot window for an open chest. Take moves the selected item into the inventory, Take All empties the
// chest and closes the window.
public class LootUI : MonoBehaviour
{
    private bool isOpen = false;
    public UIDocument uiDocument;
    private LootChest lootChest;
    private ScrollView loot;
    private VisualElement lootPanel;
    private VisualElement lootContainer;
    private Button closeButton;
    private Button takeSelectedButton;
    private Button takeAllButton;

    private ItemTooltip tooltip;
    private int selectedItem = -1;
    private VisualElement selectedItemSlot;
    public static LootUI Instance { get; private set; }

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
        var root = uiDocument.rootVisualElement;
        lootPanel = root;
        lootContainer = root.Q<VisualElement>("LootContainer");
        loot = lootContainer.Q<ScrollView>("LootItems");
        closeButton = root.Q<Button>("ExitButton");
        lootPanel.style.display = DisplayStyle.None;
        closeButton.clicked += ToggleLootInventory;
        takeSelectedButton = root.Q<Button>("TakeSelectedButton");
        takeAllButton = root.Q<Button>("TakeAllButton");
        takeSelectedButton.clicked += OnTakeSelectedClicked;
        takeAllButton.clicked += OnTakeAllClicked;

        tooltip = new ItemTooltip(lootContainer);
        RefreshLoot();
    }

    // Update is called once per frame
    void Update()
    {
        if (isOpen && GameInput.CancelPressed)
        {
            ToggleLootInventory();
        }

    }

    public void ToggleLootInventory()
    {
        isOpen = lootPanel.style.display == DisplayStyle.None;

        lootPanel.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;

        if (isOpen)
        {
            ClearSelection();
            RefreshLoot();
        }
        else
        {
            tooltip.Hide();
            if (lootChest != null && lootChest.Loot.Count > 0)
                lootChest.CloseChest(); // Emptied chests stay open
        }
        Time.timeScale = isOpen ? 0f : 1f;
        PauseMenu.GameIsPaused = isOpen;
        StartCoroutine(DelayUIFlagClear());
    }

    IEnumerator DelayUIFlagClear()
    {
        yield return null; // wait one frame
        CoreUI.IsUIOpen = isOpen;
    }

    private void RefreshLoot()
    {
        if (loot == null)
            return; // Not set up yet (Start)
        loot.Clear();
        selectedItemSlot = null;
        if (lootChest != null)
            loot.Add(UpdateItemsUI(lootChest.Loot));
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        bool hasLoot = lootChest != null && lootChest.Loot.Count > 0;
        takeAllButton.SetEnabled(hasLoot);
        takeSelectedButton.SetEnabled(hasLoot && selectedItem >= 0 && selectedItem < lootChest.Loot.Count);
    }

    private void ClearSelection()
    {
        selectedItem = -1;
        selectedItemSlot = null;
    }

    private void OnTakeSelectedClicked()
    {
        if (lootChest == null || selectedItem < 0 || selectedItem >= lootChest.Loot.Count)
            return;

        Item item = lootChest.Loot[selectedItem];
        lootChest.Loot.RemoveAt(selectedItem);
        lootChest.SaveContents();
        InventorySystem.Instance.AddItem(item);
        ClearSelection();

        if (lootChest.Loot.Count == 0)
            ToggleLootInventory(); // Nothing left to take
        else
            RefreshLoot();
    }

    private void OnTakeAllClicked()
    {
        if (lootChest == null)
            return;

        InventorySystem inventory = InventorySystem.Instance;
        foreach (Item item in lootChest.Loot)
        {
            inventory.AddItem(item, notify: false);
        }
        lootChest.Loot.Clear();
        lootChest.SaveContents();
        inventory.NotifyChanged();
        ClearSelection();
        ToggleLootInventory();
    }

    private ScrollView UpdateItemsUI(List<Item> items)
    {
        VisualElement grid = ItemGrid.Build(items, 4, tooltip,
            item => "Sell: " + ShopSystem.SellPrice(item) + " G",
            UpdateTooltipPosition,
            OnItemSlotClick,
            index => index == selectedItem);
        if (selectedItem >= 0 && selectedItem < items.Count)
            selectedItemSlot = grid.ElementAt(selectedItem); // The redrawn slot, for recoloring later
        return ItemGrid.WrapInScrollView(grid);
    }
    private void OnItemSlotClick(VisualElement itemSlot, int index)
    {
        if (selectedItemSlot != null)
            selectedItemSlot.style.backgroundColor = ItemGrid.SlotColor;
        if (index == selectedItem && selectedItem != -1)
        {
            ClearSelection();
        }
        else
        {
            selectedItemSlot = itemSlot;
            selectedItem = index;
            itemSlot.style.backgroundColor = ItemGrid.SelectedColor;
        }
        UpdateButtons();
    }

    private void UpdateTooltipPosition(Vector2 mousePosition)
    {
        float tooltipWidth = tooltip.Width;
        float tooltipHeight = tooltip.Height;
        float screenWidth = lootPanel.resolvedStyle.width;
        float screenHeight = lootPanel.resolvedStyle.height;
        float offset = 40;
        // Default position (to the right of the cursor)
        float newX = mousePosition.x - screenWidth / 4;
        float newY = mousePosition.y - screenHeight / 2 + offset * 4;

        // Check right boundary
        if (newX + tooltipWidth > screenWidth)
        {
            newX = mousePosition.x - tooltipWidth; // Move to the left
        }

        // Check bottom boundary
        if (newY + tooltipHeight > screenHeight)
        {
            newY = mousePosition.y - tooltipHeight; // Move up
        }

        tooltip.MoveTo(newX, newY);
    }

    public void SetLootChest(LootChest lootChest)
    {
        this.lootChest = lootChest;
    }
}
