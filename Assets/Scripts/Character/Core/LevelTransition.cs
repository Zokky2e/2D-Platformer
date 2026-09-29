using UnityEngine;

// Exit trigger: loads nextSceneName when the player walks in. FadeTransition runs the transition,
// since this object is destroyed by the scene load.
public class LevelTransition : MonoBehaviour
{
    public string nextSceneName; // Set this in the Inspector

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            FadeTransition.Instance.LoadScene(nextSceneName); // Ignored while a transition is running
    }
}
