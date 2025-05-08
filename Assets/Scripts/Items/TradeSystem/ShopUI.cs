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
    private VisualElement tooltip;
    private Label tooltipName;
    private Label tooltipGold;
    private Label tooltipDescription;
    public string shopKeeperName;
    private (bool isPlayerInventory , int itemId) selectedItem = (false, -1);
    private VisualElement selectedItemSlot;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if (this.GetComponent<InventoryUI>() != null && this.GetComponent<InventoryUI>() != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        Label tooltip = new Label(); // Tooltip for item descriptions
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
        SetupTooltip(); // Initialize tooltip setups
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
        int columns = 4; // Number of columns in the grid
        int rows = 4;
        int totalSlots = Mathf.Max(items.Count, columns * rows);

        // Create scrollable inventory grid
        ScrollView gridScrollView = new ScrollView(ScrollViewMode.Vertical);
        gridScrollView.style.width = Length.Percent(100);
        gridScrollView.style.overflow = Overflow.Hidden; // Prevents content overflow
        gridScrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;

        // Ensure scrolling works with the mouse wheel
        gridScrollView.RegisterCallback<WheelEvent>(evt =>
        {
            gridScrollView.scrollOffset += new Vector2(0, evt.delta.y * 0.25f); // Adjust speed if needed
            evt.StopPropagation();
        });
        VisualElement gridContainer = new VisualElement();
        gridContainer.style.flexDirection = FlexDirection.Row;
        gridContainer.style.flexWrap = Wrap.Wrap; // Allow wrapping into multiple rows
        gridContainer.style.justifyContent = Justify.FlexStart; // Align left
        gridContainer.style.alignItems = Align.Center; // Center items vertically
        gridContainer.style.paddingBottom = 10;
        gridContainer.style.width = Length.Percent(100);
        gridContainer.style.height = Length.Percent(100);

        for (int i = 0; i < totalSlots; i++)
        {
            int currentIndex = i;
            VisualElement itemSlot = new VisualElement();
            itemSlot.style.flexDirection = FlexDirection.Column;
            itemSlot.style.alignItems = Align.Center;
            itemSlot.style.width = 120;
            itemSlot.style.height = 120; // Extra height to fit text
            itemSlot.style.marginRight = 10; // Spacing between columns
            itemSlot.style.marginBottom = 10; // Spacing between rows
            itemSlot.style.alignItems = Align.Center; // Center text
            itemSlot.style.backgroundColor = new Color(0, 0, 0, 0.1f); // Light transparent slot background

            // Create image for item
            VisualElement itemImage = new VisualElement();
            itemImage.style.width = 120;
            itemImage.style.height = 120;
            itemImage.style.alignSelf = Align.Center; // Center image

            if (i < items.Count)
            {
                Item item = items[currentIndex]; // use the captured index
                itemImage.style.backgroundImage = new StyleBackground(item.Sprite); // Assign sprite

                itemSlot.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    itemSlot.style.backgroundColor = new Color(1, 1, 1, 0.3f); // Lighten background on hover
                    tooltipName.text = item.Name; // Set item name
                    item.AdjustDescription();
                    tooltipDescription.text = item.Description; // Show item description
                    tooltip.style.visibility = Visibility.Visible;
                    string goldValue = isPlayerInventory ? "Sell: " + MathF.Floor((item.Price * 0.6f)).ToString() : "Buy: " + item.Price.ToString();
                    tooltipGold.text = goldValue + " G";
                    UpdateTooltipPosition(evt.mousePosition); // Update tooltip position
                });
                // Remove highlight when leaving
                itemSlot.RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    if ((isPlayerInventory, currentIndex) == selectedItem)
                        itemSlot.style.backgroundColor = new Color(0.5f, 1, 1, 0.3f); // Lighten background on hover
                    else
                        itemSlot.style.backgroundColor = new Color(0, 0, 0, 0.1f); // Light transparent slot background

                    tooltip.style.visibility = Visibility.Hidden;
                });
                // Move tooltip with mouse
                itemSlot.RegisterCallback<MouseMoveEvent>(evt =>
                {
                    UpdateTooltipPosition(evt.mousePosition); // Update tooltip position
                });
                // Add click event to use the item
                itemSlot.RegisterCallback<ClickEvent>(evt =>
                {
                    OnItemSlotClick(isPlayerInventory, itemSlot, currentIndex); // i need to send the current index value but it ends up sending the last index value
                    SetEnabledButtons();
                    tooltip.style.visibility = Visibility.Hidden;
                });
            }

            // Add sprite & text to container
            itemSlot.Add(itemImage);

            // Add to UI
            gridContainer.Add(itemSlot);
        }
        gridScrollView.Add(gridContainer);
        return gridScrollView;
    }
    private void OnItemSlotClick(bool isPlayerInventory, VisualElement itemSlot, int index)
    {
        //debugging here shows index is always 16 which is the last i value possible but i need the index to be the value at method register
        if ((isPlayerInventory, index) == selectedItem && selectedItem.itemId != -1)
        {
            itemSlot.style.backgroundColor = new Color(0, 0, 0, 0.1f);
            selectedItem = (false, -1);
            selectedItemSlot = null;
        }
        else
        {
            if (selectedItemSlot != null)
                selectedItemSlot.style.backgroundColor = new Color(0, 0, 0, 0.1f);
            selectedItemSlot = itemSlot;
            selectedItem = (isPlayerInventory, index);
            itemSlot.style.backgroundColor = new Color(0.5f, 1, 1, 0.3f); // Lighten background on hover
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

        // Create the item gold label
        tooltipGold = new Label();
        tooltipGold.style.fontSize = 22;
        tooltipGold.style.color = Color.yellow;
        tooltipGold.style.marginBottom = 5; // Space between name and description
        tooltipGold.style.whiteSpace = WhiteSpace.Normal;
        tooltipGold.style.overflow = Overflow.Hidden;
        tooltipGold.style.textOverflow = TextOverflow.Clip;

        // Create the item description label
        tooltipDescription = new Label();
        tooltipDescription.style.fontSize = 22;
        tooltipDescription.style.color = Color.white;
        tooltipDescription.style.whiteSpace = WhiteSpace.Normal;
        tooltipDescription.style.overflow = Overflow.Hidden;
        tooltipDescription.style.textOverflow = TextOverflow.Clip;

        // Add labels to the tooltip container
        tooltip.Add(tooltipName);
        tooltip.Add(tooltipGold);
        tooltip.Add(tooltipDescription);

        shopContainer.Add(tooltip); // Add tooltip to the inventory UI
    }
    private void UpdateTooltipPosition(Vector2 mousePosition)
    {
        float tooltipWidth = tooltip.resolvedStyle.width;
        float tooltipHeight = tooltip.resolvedStyle.height;
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

        // Apply position
        tooltip.style.left = newX;
        tooltip.style.top = newY;
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
        //select item, if in player inventory have a button for sell
        //if in shop inventory have a button for buy
        Debug.Log($"Selling {playerInventory.items[selectedItem.itemId].Name}");
    }
    
    public void OnBuyButtonClicked()
    {
        //select item, if in player inventory have a button for sell
        //if in shop inventory have a button for buy
        Debug.Log($"Buying {shopInventory.items[selectedItem.itemId].Name}");
    }

    public void SetShopInventory(ShopInventory shopInventory)
    {
        this.shopInventory = shopInventory;
    }
}
