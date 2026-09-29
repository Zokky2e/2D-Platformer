using UnityEngine;
class RespawnCheckpoint : MonoBehaviour
{
    private Interactable interactable;
    public string title = "";
    [TextArea(3, 5)] public string description = "Checkpoint saved!";
    private void Start()
    {
        // Add an Interactable component and set up interaction
        interactable = gameObject.AddComponent<Interactable>();
        interactable.onInteract = SetPlayerRespawn;
    }

    private void SetPlayerRespawn()
    {
        GameRespawn.Instance.SetPlayerRespawn(transform);
        SaveSystem.Instance.Save();
        DialogSystem.Instance.ShowDialog(title, description, null);
    }
}
