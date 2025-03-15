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
        items.Clear(); // Clear old items

        int columns = 4; // Number of columns in the grid
        VisualElement gridContainer = new VisualElement(); 
        gridContainer.style.flexDirection = FlexDirection.Row;
        gridContainer.style.flexWrap = Wrap.Wrap; // Allow wrapping into multiple rows
        gridContainer.style.justifyContent = Justify.FlexStart; // Align left
        gridContainer.style.alignItems = Align.Center; // Center items vertically
        gridContainer.style.paddingBottom = 10;
        foreach (Item item in inventory.items)
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

            // Create image for item
            VisualElement itemImage = new VisualElement();
            itemImage.style.width = 120;
            itemImage.style.height = 120;
            itemImage.style.backgroundColor = Color.black;
            itemImage.style.backgroundImage = new StyleBackground(item.Sprite); // Set sprite

            // Create label for item name
            Label itemLabel = new Label(item.Name);
            itemLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            itemLabel.style.fontSize = 24;
            itemLabel.style.color = Color.white;

            // Add click event to entire item slot
            itemSlot.RegisterCallback<ClickEvent>(evt => UseItem(item));

            // Add sprite & text to container
            itemSlot.Add(itemImage);
            itemSlot.Add(itemLabel);

            // Add to UI
            gridContainer.Add(itemSlot);
        }
        items.Add(gridContainer);
    }


    private void UseItem(Item item)
    {
        Debug.Log("Using item: " + item.Name);
        inventory.RemoveItem(item); // Remove after use
    }
}
