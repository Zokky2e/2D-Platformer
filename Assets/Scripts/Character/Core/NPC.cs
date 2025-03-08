using Unity.VisualScripting;
using UnityEngine;

public enum NPCAction
{
    None = 0,
    Attention,
    Information,
    Trade
}

public class NPC : MonoBehaviour
{
    public bool facingToRight = true;
    private SpriteRenderer spriteRenderer;
    private Health health;

    [Header("NPC Quest/Dialogue State")]
    public bool isTrader = false;
    public NPCAction currentAction = NPCAction.None;
    public string npcName = "NPC Name";
    [TextArea(3, 5)] public string npcDialog = "Hello, adventurer!";
    [Header("Indicators")]
    [SerializeField] private GameObject exclamationMarkPrefab;
    [SerializeField] private GameObject questionMarkPrefab;
    [SerializeField] private GameObject traderMarkPrefab;
    private GameObject activeIndicator; // To store the currently active indicator
    private Interactable interactable; // Reference to interactable component
    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>(); // Get the SpriteRenderer component
        health = GetComponent<Health>();
        
        interactable = gameObject.AddComponent<Interactable>();
        interactable.onInteract = InteractWithNPC; // Assign interaction behavior

        UpdateIndicator();
        UpdateInteractableState();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            Transform player = collision.transform;

            if (player.position.x < transform.position.x && facingToRight)
            {
                Flip();
            }
            else if (player.position.x > transform.position.x && !facingToRight)
            {
                Flip();
            }
        }
    }
    private void Flip()
    {
        if (health && health.currentHealth <= 0)
        {
            // Prevent flipping if NPC is dead
            return;
        }
        facingToRight = !facingToRight;
        spriteRenderer.flipX = !spriteRenderer.flipX;
    }

    // Function to update the NPC's indicator based on the action state
    public void UpdateIndicator()
    {
        // Destroy the current indicator if it exists
        if (activeIndicator != null)
        {
            Destroy(activeIndicator);
        }

        // Spawn the correct indicator based on currentAction
        switch (currentAction)
        {
            case NPCAction.Attention:
                activeIndicator = Instantiate(exclamationMarkPrefab, transform.position + new Vector3(0, 0.5f, 0), Quaternion.identity, transform);
                break;

            case NPCAction.Information:
                activeIndicator = Instantiate(questionMarkPrefab, transform.position + new Vector3(0, 0.5f, 0), Quaternion.identity, transform);
                break;

            case NPCAction.Trade:
                activeIndicator = Instantiate(traderMarkPrefab, transform.position + new Vector3(0, 0.5f, 0), Quaternion.identity, transform);
                break;

            case NPCAction.None:
            default:
                activeIndicator = null;
                break;
        }
        UpdateInteractableState(); // Update interactability when indicator updates
    }

    // Method to change the NPC action dynamically
    public void SetAction(NPCAction newAction)
    {
        currentAction = newAction;
        UpdateIndicator();
    }
    private void UpdateInteractableState()
    {
        // Enable/disable interactable based on the NPC's current action
        bool isInteractable = currentAction != NPCAction.None;

        if (interactable != null)
        {
            interactable.SetInteractableState(isInteractable);
        }
    }
    private void InteractWithNPC()
    {
        if (currentAction == NPCAction.Attention)
        {
            Debug.Log("NPC: Here is your quest!");
            DialogSystem.Instance.ShowDialog(npcName, npcDialog);
        }
        else if (currentAction == NPCAction.Information)
        {
            Debug.Log("NPC: I have some information for you.");
            DialogSystem.Instance.ShowDialog(npcName, npcDialog);
        }
        else if (currentAction == NPCAction.Trade)
        {
            Debug.Log("NPC: I have something to trade!");
        }
        if (isTrader)
        {
            currentAction = NPCAction.Trade;
            UpdateIndicator();
        }
    }
}
