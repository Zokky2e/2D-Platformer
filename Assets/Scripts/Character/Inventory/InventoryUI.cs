using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryUI : MonoBehaviour
{
    private bool isOpen = false;
    public UIDocument uiDocument;
    private InventorySystem inventory;
    private VisualElement inventoryPanel;
    private VisualElement inventoryContainer;
    public EquipmentUI equipmentUI;
    private ScrollView items;
    private Label gold;
    private Button closeButton;
    private ItemTooltip tooltip;
    private static InventoryUI instance;

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
    void Start()
    {
        equipmentUI = GetComponentInChildren<EquipmentUI>();

    }

    // Update is called once per frame
    void Update()
    {
        if (!PauseMenu.GameIsPaused && !isOpen && Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        if (isOpen && Input.GetKeyDown(KeyCode.Escape)) 
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
            yield return null; // Wait for next frame
        }
        inventory = InventorySystem.Instance; // Find inventory
        inventory.onInventoryChanged += () =>
        {
            UpdateInventoryUI(); // Listen for changes
        };
            

        var root = uiDocument.rootVisualElement;
        inventoryPanel = root;
        inventoryContainer = root.Q<VisualElement>("InventoryContainer");
        items = inventoryContainer.Q<ScrollView>("Items");
        closeButton = root.Q<Button>("ExitButton");
        gold = root.Q<Label>("Gold");
        inventoryPanel.style.display = DisplayStyle.None;
        closeButton.clicked += ToggleInventory;
        tooltip = new ItemTooltip(inventoryContainer);
        UpdateInventoryUI();
    }

    private void ToggleInventory()
    {
        isOpen = inventoryPanel.style.display == DisplayStyle.None;

        inventoryPanel.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;

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
        if (inventory != null)
            inventory.onInventoryChanged -= UpdateInventoryUI;
    }

    private void UpdateInventoryUI()
    {
        //inventoryContainer.Clear(); //Clear inventory
        items.Clear(); // Clear old items
        gold.text = InventorySystem.Instance.gold.ToString();
        UpdateInventoryItemsUI();
    }

    private void UpdateInventoryItemsUI()
    {
        VisualElement grid = ItemGrid.Build(inventory.items, 16, tooltip,
            item => item.Price + " G",
            UpdateTooltipPosition,
            (itemSlot, index) =>
            {
                itemSlot.style.backgroundColor = ItemGrid.SlotColor;
                OnItemClick(inventory.items[index]);
            });
        items.Add(grid);
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
        if (item.Type != ItemType.Consumable)
        {
            EquipmentSystem.Instance.EquipItem(item);
        }
        else
        {
            UseItem(item);
        }
    }
    private void UseItem(Item item)
    {
        inventory.UseItem(item);
    }
}
