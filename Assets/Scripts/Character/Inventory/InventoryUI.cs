using UnityEngine;
using UnityEngine.UIElements;

public class InventoryUI : MonoBehaviour
{
    public UIDocument uiDocument;
    private InventorySystem inventory;
    private VisualElement inventoryPanel;
    private VisualElement inventoryContainer;
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
        inventory = InventorySystem.Instance; // Find inventory
        inventory.onInventoryChanged += UpdateInventoryUI; // Listen for changes

        var root = uiDocument.rootVisualElement;
        inventoryPanel = root;
        inventoryContainer = root.Q<VisualElement>("InventoryContainer");
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
        inventoryContainer.Clear(); // Clear old items

        foreach (var item in inventory.items)
        {
            Button itemButton = new Button();
            itemButton.text = item.Name;
            itemButton.clicked += () => UseItem(item);
            inventoryContainer.Add(itemButton);
        }
    }

    private void UseItem(Item item)
    {
        Debug.Log("Using item: " + item.Name);
        inventory.RemoveItem(item); // Remove after use
    }
}
