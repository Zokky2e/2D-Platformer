
using System.Collections;
using UnityEngine;

public class FadeTransition : Singleton<FadeTransition>
{
    [SerializeField] private Animator fadeAnimator;  // Reference to the fade Animator
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

    public void FadeBack()
    {
        StartCoroutine(FadeBackCoroutine());
    }

    private IEnumerator FadeBackCoroutine()
    {
        yield return new WaitForSeconds(0.5f);  // Optional delay before fading back
        Debug.Log("Fade From Black");
        fadeAnimator.SetBool("FadeToBlack", false);
    }
}
