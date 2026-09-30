using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// The shop window: the player's items on one side, the shop's stock on the other. Like InventoryUI it keeps
// no copy of either list: it redraws from InventorySystem.Instance and the ShopInventory on each change,
// when it opens and after each purchase or sale. Buying and selling act on the selected item, not a slot
// index, so an out-of-date slot can't buy the wrong thing.
public class ShopUI : MonoBehaviour
{
    private bool isOpen = false;
    public UIDocument uiDocument;
    private ShopInventory shopInventory;
    private ScrollView playerItems;
    private ScrollView shopItems;
    private VisualElement shopPanel;
    private VisualElement shopContainer;
    private Label gold;
    private Label shopKeeper;
    private Button closeButton;
    private Button sellButton;
    private Button buyButton;
    private ItemTooltip tooltip;
    public string shopKeeperName;
    // The selected slot (for the highlight) and the item it showed (for the action)
    private (bool isPlayerInventory, int index) selectedSlot = (false, -1);
    private Item selectedItem;
    private VisualElement selectedSlotElement;
    public static ShopUI Instance { get; private set; }

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
        shopPanel = root;
        shopContainer = root.Q<VisualElement>("ShopContainer");
        playerItems = shopContainer.Q<ScrollView>("PlayerItems");
        shopItems = shopContainer.Q<ScrollView>("ShopItems");
        closeButton = root.Q<Button>("ExitButton");
        sellButton = root.Q<Button>("SellButton");
        buyButton = root.Q<Button>("BuyButton");
        gold = root.Q<Label>("Gold");
        shopKeeper = root.Q<Label>("ShopKeeper");
        shopPanel.style.display = DisplayStyle.None;
        closeButton.clicked += ToggleShopInventory;
        sellButton.clicked += OnSellButtonClicked;
        buyButton.clicked += OnBuyButtonClicked;
        tooltip = new ItemTooltip(shopContainer);
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
        if (isOpen && GameInput.CancelPressed)
        {
            ToggleShopInventory();
        }

    }

    public void ToggleShopInventory()
    {
        isOpen = shopPanel.style.display == DisplayStyle.None;

        shopPanel.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;

        if (isOpen)
        {
            ClearSelection();
            Refresh();
        }
        else
            tooltip.Hide();
        shopKeeper.text = shopKeeperName;
        Time.timeScale = isOpen ? 0f : 1f;
        PauseMenu.GameIsPaused = isOpen;
        StartCoroutine(DelayUIFlagClear());
    }

    IEnumerator DelayUIFlagClear()
    {
        yield return null; // wait one frame
        CoreUI.IsUIOpen = isOpen;
    }

    // Redraws both grids, the gold and the buttons from the current inventory and stock
    private void Refresh()
    {
        if (playerItems == null)
            return; // Not set up yet (Start)
        InventorySystem inventory = InventorySystem.Instance;
        List<Item> stock = shopInventory != null ? shopInventory.items : new List<Item>();

        // Drop a selection whose slot now holds something else (sold, bought out, or moved)
        if (selectedItem != null)
        {
            List<Item> selectedList = selectedSlot.isPlayerInventory ? inventory.items : stock;
            if (selectedSlot.index >= selectedList.Count || selectedList[selectedSlot.index] != selectedItem)
                ClearSelection();
        }

        gold.text = inventory.gold.ToString();
        playerItems.Clear();
        playerItems.Add(BuildGrid(inventory.items, true));
        shopItems.Clear(); // Also when the last item sold out
        if (shopInventory != null)
            shopItems.Add(BuildGrid(stock, false));
        SetEnabledButtons();
    }

    private ScrollView BuildGrid(List<Item> items, bool isPlayerInventory)
    {
        List<Item> shown = new List<Item>(items); // Each slot keeps the item it was drawn with
        VisualElement grid = ItemGrid.Build(shown, 16, tooltip,
            item => isPlayerInventory
                ? "Sell: " + ShopSystem.SellPrice(item) + " G"
                : "Buy: " + item.Price + " G" + StockText(item),
            UpdateTooltipPosition,
            (itemSlot, index) => OnItemSlotClick(isPlayerInventory, itemSlot, index, shown[index]),
            index => (isPlayerInventory, index) == selectedSlot);
        if (selectedItem != null && selectedSlot.isPlayerInventory == isPlayerInventory)
            selectedSlotElement = grid.ElementAt(selectedSlot.index); // The redrawn slot, for recoloring later
        return ItemGrid.WrapInScrollView(grid);
    }

    // Selecting only recolors slots, so the grids (and their scroll position) aren't rebuilt
    private void OnItemSlotClick(bool isPlayerInventory, VisualElement itemSlot, int index, Item item)
    {
        if (selectedSlotElement != null)
            selectedSlotElement.style.backgroundColor = ItemGrid.SlotColor;
        if ((isPlayerInventory, index) == selectedSlot)
        {
            ClearSelection();
        }
        else
        {
            selectedSlot = (isPlayerInventory, index);
            selectedItem = item;
            selectedSlotElement = itemSlot;
            itemSlot.style.backgroundColor = ItemGrid.SelectedColor;
        }
        SetEnabledButtons();
    }

    private void ClearSelection()
    {
        selectedSlot = (false, -1);
        selectedItem = null;
        selectedSlotElement = null;
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

    private void SetEnabledButtons()
    {
        bool fromPlayer = selectedItem != null && selectedSlot.isPlayerInventory;
        bool fromShop = selectedItem != null && !selectedSlot.isPlayerInventory;
        sellButton.SetEnabled(fromPlayer && selectedItem.IsSellable);
        buyButton.SetEnabled(fromShop && InventorySystem.Instance.gold >= selectedItem.Price);
    }

    public void OnSellButtonClicked()
    {
        if (selectedItem == null || !selectedSlot.isPlayerInventory)
            return;
        Debug.Log($"Selling {selectedItem.Name}");
        if (ShopSystem.Instance.SellItem(selectedItem))
            ClearSelection();
        Refresh();
    }

    public void OnBuyButtonClicked()
    {
        if (selectedItem == null || selectedSlot.isPlayerInventory)
            return;
        Debug.Log($"Buying {selectedItem.Name}");
        if (ShopSystem.Instance.BuyItem(shopInventory, selectedItem))
            ClearSelection();
        Refresh();
    }

    private string StockText(Item item)
    {
        int index = shopInventory.items.IndexOf(item);
        int left = index >= 0 ? shopInventory.StockLeft(index) : 0;
        return left > 0 ? $" ({left} left)" : "";
    }

    public void SetShopInventory(ShopInventory shopInventory)
    {
        this.shopInventory = shopInventory;
    }
}
