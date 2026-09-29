
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FadeTransition : Singleton<FadeTransition>
{
    private const float FadeDuration = 1f;       // Length of the fade to black
    private const float SceneSettleDelay = 0.5f; // Stay black this long after a scene load

    private static readonly int FadeToBlack = Animator.StringToHash("FadeToBlack");

    [SerializeField] private Animator fadeAnimator;  // Reference to the fade Animator

    public bool IsTransitioning { get; private set; }

    public IEnumerator FadeAndExecute(System.Action action)
    {
        // Trigger fade animation to black
        fadeAnimator.SetBool(FadeToBlack, true);

        // Wait for the fade effect
        yield return new WaitForSeconds(FadeDuration);

        // Execute the passed action (respawn, etc.)
        action?.Invoke();

        // Wait after the action (optional)
        yield return new WaitForSeconds(1f);

        // Trigger fade animation back to normal
        fadeAnimator.SetBool(FadeToBlack, false);
    }

    // Fades to black, loads the scene, moves the player to its "EntryPoint", saves and fades back.
    // Runs here because this object survives the load, unlike the exit that started it.
    public void LoadScene(string sceneName)
    {
        if (IsTransitioning)
            return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' is not in the build settings.");
            return;
        }
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        IsTransitioning = true;
        fadeAnimator.SetBool(FadeToBlack, true);
        yield return new WaitForSeconds(FadeDuration);

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(sceneName);
        yield return new WaitForSeconds(SceneSettleDelay);

        fadeAnimator.SetBool(FadeToBlack, false);
        IsTransitioning = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        // Move the persistent player, not the new scene's own hero (which is being removed)
        GameRespawn.Instance.MoveToEntryPoint();
        SaveSystem.Instance.Save();
    }
}
