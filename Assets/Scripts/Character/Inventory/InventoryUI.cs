using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryUI : MonoBehaviour
{
    public UIDocument uiDocument;
    private InventorySystem inventory;
    private VisualElement inventoryPanel;
    private VisualElement inventoryContainer;
    private ScrollView items;
    private Button closeButton;
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
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
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
            Debug.Log("Instance is null");
            yield return null; // Wait for next frame
        }
        Debug.Log("Instance is not null");
        inventory = InventorySystem.Instance; // Find inventory
        inventory.onInventoryChanged += () =>
        {
            Debug.Log("Inventory UI Updated!");
            UpdateInventoryUI(); // Listen for changes
        };
            

        var root = uiDocument.rootVisualElement;
        inventoryPanel = root;
        inventoryContainer = root.Q<VisualElement>("InventoryContainer");
        items = inventoryContainer.Q<ScrollView>("Items");
        closeButton = root.Q<Button>("ExitButton");
        inventoryPanel.style.display = DisplayStyle.None;
        closeButton.clicked += ToggleInventory;

        UpdateInventoryUI();
    }

    private void ToggleInventory()
    {
        bool isOpening = inventoryPanel.style.display == DisplayStyle.None;

        inventoryPanel.style.display = isOpening ? DisplayStyle.Flex : DisplayStyle.None;

        Time.timeScale = isOpening ? 0f : 1f;
    }
    private void OnDisable()
    {
        inventory.onInventoryChanged -= UpdateInventoryUI;
    }

    private void UpdateInventoryUI()
    {
        //inventoryContainer.Clear(); //Clear inventory
        items.Clear(); // Clear old items

        UpdateInventoryItemsUI();
    }

    private void UpdateInventoryItemsUI()
    {
        int columns = 4; // Number of columns in the grid
        int rows = 4;
        int totalSlots = Mathf.Max(inventory.items.Count, columns * rows);

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

        Label tooltip = new Label(); // Tooltip for item descriptions
        tooltip.style.position = Position.Absolute;
        tooltip.style.backgroundColor = new Color(0, 0, 0, 0.8f);
        tooltip.style.color = Color.white;
        tooltip.style.paddingLeft = 5;
        tooltip.style.paddingRight = 5;
        tooltip.style.paddingTop = 2;
        tooltip.style.paddingBottom = 2;
        tooltip.style.fontSize = 24;
        tooltip.style.visibility = Visibility.Hidden;
        inventoryContainer.Add(tooltip); // Add tooltip to the inventory UI

        for (int i = 0; i < totalSlots; i++)
        {
            // Create container for item (sprite + text)
            VisualElement itemSlot = new VisualElement();
            itemSlot.style.flexDirection = FlexDirection.Column;
            itemSlot.style.alignItems = Align.Center;
            itemSlot.style.width = 140;
            itemSlot.style.height = 140; // Extra height to fit text
            itemSlot.style.marginRight = 10; // Spacing between columns
            itemSlot.style.marginBottom = 10; // Spacing between rows
            itemSlot.style.alignItems = Align.Center; // Center text
            itemSlot.style.backgroundColor = new Color(0, 0, 0, 0.1f); // Light transparent slot background

            // Create image for item
            VisualElement itemImage = new VisualElement();
            itemImage.style.width = 120;
            itemImage.style.height = 120;
            itemImage.style.alignSelf = Align.Center; // Center image

            // Create label for item name
            Label itemLabel = new Label();
            itemLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            itemLabel.style.fontSize = 24;
            itemLabel.style.color = Color.white;

            if (i < inventory.items.Count)
            {
                Item item = inventory.items[i];
                itemImage.style.backgroundImage = new StyleBackground(item.Sprite); // Assign sprite
                itemLabel.text = item.Name; // Assign name

                itemSlot.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    Debug.Log("Showing description: " + item.Description);
                    itemSlot.style.backgroundColor = new Color(1, 1, 1, 0.3f); // Lighten background on hover
                    tooltip.text = item.Description; // Show item description
                    tooltip.style.visibility = Visibility.Visible;
                    tooltip.style.left = evt.mousePosition.x;
                    tooltip.style.top = evt.mousePosition.y - 15;
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
                    tooltip.style.left = evt.mousePosition.x;
                    tooltip.style.top = evt.mousePosition.y - 15;
                });
                // Add click event to use the item
                itemSlot.RegisterCallback<ClickEvent>(evt =>
                {
                    itemSlot.style.backgroundColor = new Color(0, 0, 0, 0.1f);
                    tooltip.style.visibility = Visibility.Hidden;
                    UseItem(item); 
                });
            }

            // Add sprite & text to container
            itemSlot.Add(itemImage);
            itemSlot.Add(itemLabel);

            // Add to UI
            gridContainer.Add(itemSlot);
        }
        gridScrollView.Add(gridContainer);
        items.Add(gridContainer);
    }

    private void UseItem(Item item)
    {
        item.UseItem();
        inventory.RemoveItem(item); // Remove after use
    }
}
