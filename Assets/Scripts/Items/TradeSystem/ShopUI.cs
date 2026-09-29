using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ShopUI : MonoBehaviour
{
    private bool isOpen = false;
    public UIDocument uiDocument;
    private InventorySystem playerInventory;
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
    private (bool isPlayerInventory , int itemId) selectedItem = (false, -1);
    private VisualElement selectedItemSlot;
    private static ShopUI instance;

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
            ToggleShopInventory();
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
        shopPanel = root;
        shopContainer = root.Q<VisualElement>("ShopContainer");
        playerItems = shopContainer.Q<ScrollView>("PlayerItems");
        shopItems = shopContainer.Q<ScrollView>("ShopItems");
        closeButton = root.Q<Button>("ExitButton");
        sellButton = root.Q<Button>("SellButton");
        buyButton = root.Q<Button>("BuyButton");
        sellButton.SetEnabled(false);
        buyButton.SetEnabled(false);
        gold = root.Q<Label>("Gold");
        shopKeeper = root.Q<Label>("ShopKeeper");
        shopPanel.style.display = DisplayStyle.None;
        closeButton.clicked += ToggleShopInventory;
        sellButton.clicked += OnSellButtonClicked;
        buyButton.clicked += OnBuyButtonClicked;
        tooltip = new ItemTooltip(shopContainer);
        UpdateInventoryUI();
    }

    public void ToggleShopInventory()
    {
        isOpen = shopPanel.style.display == DisplayStyle.None;

        shopPanel.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;

        if(isOpen)
            UpdateInventoryUI();
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

    private void OnDisable()
    {
        if (playerInventory != null)
            playerInventory.onInventoryChanged -= UpdateInventoryUI;
    }

    private void UpdateInventoryUI()
    {
        //player section
        playerItems.Clear(); // Clear old items
        gold.text = InventorySystem.Instance.gold.ToString();
        playerItems.Add(UpdateItemsUI(playerInventory.items, true)); 

        //shop section
        if (shopInventory?.items?.Count > 0)
        {
            shopItems.Clear();
            shopItems.Add(UpdateItemsUI(shopInventory.items));
        }
    }

    private ScrollView UpdateItemsUI(List<Item> items, bool isPlayerInventory = false)
    {
        VisualElement grid = ItemGrid.Build(items, 16, tooltip,
            item => (isPlayerInventory ? "Sell: " + ((int)MathF.Floor(item.Price * 0.6f)) : "Buy: " + item.Price) + " G",
            UpdateTooltipPosition,
            (itemSlot, index) =>
            {
                OnItemSlotClick(isPlayerInventory, itemSlot, index);
                SetEnabledButtons();
            },
            index => (isPlayerInventory, index) == selectedItem);
        return ItemGrid.WrapInScrollView(grid);
    }
    private void OnItemSlotClick(bool isPlayerInventory, VisualElement itemSlot, int index)
    {
        if ((isPlayerInventory, index) == selectedItem && selectedItem.itemId != -1)
        {
            itemSlot.style.backgroundColor = ItemGrid.SlotColor;
            selectedItem = (false, -1);
            selectedItemSlot = null;
        }
        else
        {
            if (selectedItemSlot != null)
                selectedItemSlot.style.backgroundColor = ItemGrid.SlotColor;
            selectedItemSlot = itemSlot;
            selectedItem = (isPlayerInventory, index);
            itemSlot.style.backgroundColor = ItemGrid.SelectedColor;
        }
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
        if (selectedItem.itemId != -1)
        {
            if (selectedItem.isPlayerInventory)
            {
                sellButton.SetEnabled(playerInventory.items[selectedItem.itemId].IsSellable);
                buyButton.SetEnabled(false);
            }
            else
            {
                sellButton.SetEnabled(false);
                buyButton.SetEnabled(true);
            }
        }
        else
        {
            sellButton.SetEnabled(false);
            buyButton.SetEnabled(false);

        }
    }

    public void OnSellButtonClicked()
    {
        var shopSystem = ShopSystem.Instance;
        //select item, if in player inventory have a button for sell
        //if in shop inventory have a button for buy
        Debug.Log($"Selling {playerInventory.items[selectedItem.itemId].Name}");
        bool isSold = shopSystem.SellItem(selectedItem.itemId);
        if (isSold) selectedItem = (false, -1); 
        SetEnabledButtons();
    }
    
    public void OnBuyButtonClicked()
    {
        var shopSystem = ShopSystem.Instance;
        //select item, if in player inventory have a button for sell
        //if in shop inventory have a button for buy
        Debug.Log($"Buying {shopInventory.items[selectedItem.itemId].Name}");
        bool isBought = shopSystem.BuyItem(ref shopInventory, selectedItem.itemId);
        if (isBought) selectedItem = (false, -1); 
        SetEnabledButtons();
    }

    public void SetShopInventory(ShopInventory shopInventory)
    {
        this.shopInventory = shopInventory;
    }
}
