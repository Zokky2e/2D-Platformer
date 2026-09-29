using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LootUI : MonoBehaviour
{
    private bool isOpen = false;
    public UIDocument uiDocument;
    private LootChest lootChest;
    private InventorySystem playerInventory;
    private ScrollView loot;
    private VisualElement lootPanel;
    private VisualElement lootContainer;
    private Button closeButton;
    private ItemTooltip tooltip;
    private int selectedItem = -1;
    private VisualElement selectedItemSlot;
    private static LootUI instance;

    private void Awake()
    {
        // Keep the first UI; Level0 brings its own copy every time it is reloaded
        if (instance != null && instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleLootInventory();
        }

    }
    private void OnEnable()
    {
        StartCoroutine(WaitForInventorySystem());
    }

    private IEnumerator WaitForInventorySystem()
    {
        // Wait until the InventorySystem instance is ready
        while (InventorySystem.Instance == null)
        {
            yield return null; // Wait for next frame
        }
        playerInventory = InventorySystem.Instance; // Find inventory
        playerInventory.onInventoryChanged += () =>
        {
            UpdateInventoryUI(); // Listen for changes
        };

        var root = uiDocument.rootVisualElement;
        lootPanel = root;
        lootContainer = root.Q<VisualElement>("LootContainer");
        loot = lootContainer.Q<ScrollView>("LootItems");
        closeButton = root.Q<Button>("ExitButton");
        lootPanel.style.display = DisplayStyle.None;
        closeButton.clicked += ToggleLootInventory;
        tooltip = new ItemTooltip(lootContainer);
        UpdateInventoryUI();
    }

    public void ToggleLootInventory()
    {
        isOpen = lootPanel.style.display == DisplayStyle.None;

        lootPanel.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;

        if (isOpen)
            UpdateInventoryUI();
        else
            lootChest.CloseChest();
        Time.timeScale = isOpen ? 0f : 1f;
        PauseMenu.GameIsPaused = isOpen;
        StartCoroutine(DelayUIFlagClear());
    }

    IEnumerator DelayUIFlagClear()
    {
        yield return null; // wait one frame
        CoreUI.IsUIOpen = isOpen;
    }

    private void OnDisable()
    {
        if (playerInventory != null)
            playerInventory.onInventoryChanged -= UpdateInventoryUI;
    }

    private void UpdateInventoryUI()
    {

        //shop section
        if (lootChest?.Loot?.Count > 0)
        {
            loot.Clear();
            loot.Add(UpdateItemsUI(lootChest.Loot));
        }
    }

    private ScrollView UpdateItemsUI(List<Item> items)
    {
        VisualElement grid = ItemGrid.Build(items, 4, tooltip,
            item => "Sell: " + ((int)MathF.Floor(item.Price * 0.6f)) + " G",
            UpdateTooltipPosition,
            OnItemSlotClick,
            index => index == selectedItem);
        return ItemGrid.WrapInScrollView(grid);
    }
    private void OnItemSlotClick(VisualElement itemSlot, int index)
    {
        if (index == selectedItem && selectedItem != -1)
        {
            itemSlot.style.backgroundColor = ItemGrid.SlotColor;
            selectedItem =  -1;
            selectedItemSlot = null;
        }
        else
        {
            if (selectedItemSlot != null)
                selectedItemSlot.style.backgroundColor = ItemGrid.SlotColor;
            selectedItemSlot = itemSlot;
            selectedItem = index;
            itemSlot.style.backgroundColor = ItemGrid.SelectedColor;
        }
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
