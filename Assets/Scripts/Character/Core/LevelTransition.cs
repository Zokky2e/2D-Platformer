using UnityEngine;

// Exit trigger: loads nextSceneName when the player walks in. FadeTransition runs the transition,
// since this object is destroyed by the scene load.
public class LevelTransition : MonoBehaviour
{
    public string nextSceneName; // Set this in the Inspector

    private string lockedMessage; // Shown instead of leaving while locked, e.g. until a boss is defeated
    private GameObject barrier; // Solid wall while locked, so the player can't walk past the exit

    public bool IsLocked => lockedMessage != null;

    public void Lock(string message)
    {
        lockedMessage = message;
        if (barrier == null)
        {
            barrier = new GameObject("LockedBarrier") { layer = gameObject.layer };
            barrier.transform.SetParent(transform, false);
            BoxCollider2D wall = barrier.AddComponent<BoxCollider2D>();
            // Floor-to-ceiling in a dungeon corridor, and behind the trigger's center so the player
            // touches the trigger (and gets the message) before the wall
            wall.size = new Vector2(0.5f, 3f);
            wall.offset = new Vector2(0.25f, 0f);
        }
        barrier.SetActive(true);
    }

    public void Unlock()
    {
        lockedMessage = null;
        if (barrier != null)
            barrier.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;
        if (IsLocked)
            DialogSystem.Instance.ShowDialog("", lockedMessage, null);
        else
            FadeTransition.Instance.LoadScene(nextSceneName); // Ignored while a transition is running
    }
}
