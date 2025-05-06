using System.Collections;
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
    private Button closeButton;
    private VisualElement tooltip;
    private Label tooltipName;
    private Label tooltipGold;
    private Label tooltipDescription;
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
        gold = root.Q<Label>("Gold");
        shopPanel.style.display = DisplayStyle.None;
        closeButton.clicked += ToggleShopInventory;
        SetupTooltip(); // Initialize tooltip setups
        UpdateInventoryUI();
    }

    public void ToggleShopInventory()
    {
        isOpen = shopPanel.style.display == DisplayStyle.None;

        shopPanel.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;

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
        shopContainer.Clear(); //Clear shop

        //player section
        //playerItems.Clear(); // Clear old items
        gold.text = InventorySystem.Instance.gold.ToString();
        //UpdateInventoryItemsUI();

        //shop section
        //shopItems.Clear();
        //UpdateShopItemsUI();
    }

    private void UpdateInventoryItemsUI()
    {
        int columns = 4; // Number of columns in the grid
        int rows = 4;
        int totalSlots = Mathf.Max(playerInventory.items.Count, columns * rows);

        // Create scrollable inventory grid
        ScrollView gridScrollView = new ScrollView(ScrollViewMode.Vertical);
        gridScrollView.style.height = Length.Percent(30); // Full height of inventory panel
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
            // Create container for item (sprite + text)
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

            if (i < playerInventory.items.Count)
            {
                Item item = playerInventory.items[i];
                itemImage.style.backgroundImage = new StyleBackground(item.Sprite); // Assign sprite

                itemSlot.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    itemSlot.style.backgroundColor = new Color(1, 1, 1, 0.3f); // Lighten background on hover
                    tooltipName.text = item.Name; // Set item name
                    item.AdjustDescription();
                    tooltipDescription.text = item.Description; // Show item description
                    tooltip.style.visibility = Visibility.Visible;
                    tooltipGold.text = item.Price.ToString() + " G";
                    UpdateTooltipPosition(evt.mousePosition); // Update tooltip position
                });
                // Remove highlight when leaving
                itemSlot.RegisterCallback<MouseLeaveEvent>(evt =>
                {
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
                    itemSlot.style.backgroundColor = new Color(0, 0, 0, 0.1f);
                    tooltip.style.visibility = Visibility.Hidden;
                    OnItemClick(item);
                });
            }

            // Add sprite & text to container
            itemSlot.Add(itemImage);

            // Add to UI
            gridContainer.Add(itemSlot);
        }
        gridScrollView.Add(gridContainer);
        playerItems.Add(gridContainer);
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

    public void OnItemClick(Item item)
    {
        //select item, if in player inventory have a button for sell
        //if in shop inventory have a button for buy
    }
}
