using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTransition : MonoBehaviour
{
    public string nextSceneName; // Set this in the Inspector
    public Transform spawnPoint; // Name of the spawn point in next scene

    private void OnTriggerEnter2D(Collider2D other)  // Use Collider for 3D
    {
        if (other.CompareTag("Player"))  // Ensure the Player has a "Player" tag
        {
            StartCoroutine(FadeTransition.Instance.FadeAndExecute(LoadNextScene));
        }
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        GameObject spawnPoint = GameObject.Find("EntryPoint"); // Ensure it's named correctly

        if (player != null && spawnPoint != null)
        {
            player.transform.position = spawnPoint.transform.position;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded; // Unsubscribe after setting position
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
}
