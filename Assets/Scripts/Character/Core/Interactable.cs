using System;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Interaction Settings")]
    public GameObject interactionPromptPrefab; // Prefab for the "E" indicator
    private GameObject interactionPromptInstance; // Instance of the "E" prompt
    private bool isInteractable = true;
    
    public Action onInteract; // Action to perform on interaction

    private bool isPlayerNearby = false;
    void Start()
    {
        // If no prefab is assigned, try to load a default one
        if (interactionPromptPrefab == null)
        {
            interactionPromptPrefab = Resources.Load<GameObject>("InteractKey");
        }

        // Spawn the interaction prompt if a valid prefab exists
        if (interactionPromptPrefab != null)
        {
            interactionPromptInstance = Instantiate(interactionPromptPrefab, transform.position + new Vector3(0, 1f, 0), Quaternion.identity, transform);
            interactionPromptInstance.SetActive(false);
        }
    }

    public void SetInteractableState(bool state)
    {
        isInteractable = state;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isInteractable && collision.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (interactionPromptInstance != null)
            {
                interactionPromptInstance.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (isInteractable && collision.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (interactionPromptInstance != null)
            {
                interactionPromptInstance.SetActive(false);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isPlayerNearby && !DialogSystem.Instance.DialogActive && Input.GetKeyDown(KeyCode.E))
        {
            onInteract?.Invoke(); // Execute the assigned interaction action
        }
    }
}
