using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTransition : MonoBehaviour
{
    public string nextSceneName; // Set this in the Inspector
    public string spawnPointName; // Name of the spawn point in next scene
    public Animator fadeAnimator;  // Attach a UI Animator for fade effect

    private void OnTriggerEnter2D(Collider2D other)  // Use Collider for 3D
    {
        if (other.CompareTag("Player"))  // Ensure the Player has a "Player" tag
        {
            StartCoroutine(LoadNextScene());
        }
    }

    IEnumerator LoadNextScene()
    {
        fadeAnimator.SetBool("FadeToBlack", true); // Trigger fade animation
        yield return new WaitForSeconds(1f); // Wait for fade effect
        PlayerPrefs.SetString("LastExit", spawnPointName); // Save exit info
        SceneManager.LoadScene(nextSceneName);
        yield return new WaitForSeconds(1f); // Wait for fade effect
        fadeAnimator.SetBool("FadeToBlack", false); // Trigger fade animation
    }
}
