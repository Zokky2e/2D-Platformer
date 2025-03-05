
using System.Collections;
using UnityEngine;

public class FadeTransition : MonoBehaviour
{
    public static FadeTransition Instance { get; private set; }
    [SerializeField] private Animator fadeAnimator;  // Reference to the fade Animator
    private void Awake()
    {
        // Ensure only one instance exists, and it persists across scenes if needed
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Keep this object across scenes
        }
    }
    public IEnumerator FadeAndExecute(System.Action action)
    {
        // Trigger fade animation to black
        fadeAnimator.SetBool("FadeToBlack", true);

        // Wait for the fade effect
        yield return new WaitForSeconds(1f);

        // Execute the passed action (respawn, load scene, etc.)
        action?.Invoke();

        // Wait after the action (optional)
        yield return new WaitForSeconds(1f);

        // Trigger fade animation back to normal
        fadeAnimator.SetBool("FadeToBlack", false);
    }
}
