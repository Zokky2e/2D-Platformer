using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTransition : MonoBehaviour
{
    public string nextSceneName; // Set this in the Inspector
    public Transform spawnPoint; // Name of the spawn point in next scene

    private bool isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D other)  // Use Collider for 3D
    {
        if (!isTransitioning && other.CompareTag("Player"))  // Ensure the Player has a "Player" tag
        {
            isTransitioning = true; // Only fade and load once
            StartCoroutine(FadeTransition.Instance.FadeAndExecute(() => StartCoroutine(LoadNextScene())));
        }
    }

    private IEnumerator LoadNextScene()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(nextSceneName);
        // Now that the scene is loaded, wait a small delay before fading back
        FadeTransition.Instance.FadeBack();
        Debug.Log("Im loading scene");
        yield return new WaitForSeconds(0.2f);

        Debug.Log("Im Calling back fade");
        // Fade back after loading
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Move the persistent player, not the new scene's own hero (which is being removed)
        GameRespawn.Instance.MoveToEntryPoint();

        SceneManager.sceneLoaded -= OnSceneLoaded; // Unsubscribe after setting position
        WorldStateManager.Instance.Save();
    }
}
