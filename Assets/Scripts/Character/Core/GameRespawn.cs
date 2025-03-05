using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameRespawn : MonoBehaviour
{
    public float threshold = -10f;
    private Transform playerRespawn;
    private Vector3 startingPosition;
    private Health playerHealth;
    public static GameRespawn Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            playerHealth = GetComponent<Health>();
            startingPosition = transform.position;
            playerRespawn = null;
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //i will have gameobjects on my levels which are clickable and they should get set here as the respawn point
    public void SetPlayerRespawn(Transform newRespawnPoint)
    {

        playerRespawn = newRespawnPoint;
    }

    //Check if player dropped out of bounds, killing player gets the respawn button on screen
    void FixedUpdate()
    {
        if (transform.position.y < threshold)
        {
            playerHealth.TakeDamage(999f);
        }
    }

    //clicking respawn button on screen should cause the player to respawn
    public void RespawnPlayer() 
    {
        if (PersistentPlayerHealth.Instance != null)
        {
            StartCoroutine(FadeTransition.Instance.FadeAndExecute(Respawn));
        }
    }

    private void Respawn()
    {

        PersistentPlayerHealth.Instance.AddHealth(PersistentPlayerHealth.Instance.startingHealth);
        Vector3 respawnPosition = (playerRespawn != null) ? playerRespawn.position : startingPosition;
        transform.position = respawnPosition;
    }

}
