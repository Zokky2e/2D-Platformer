using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameRespawn : Singleton<GameRespawn>
{
    public float threshold = -10f;
    private Transform playerRespawn;
    private Vector3 startingPosition;
    private Health playerHealth;
    public static GameRespawn Instance;

    protected override void Awake()
    {
        base.Awake();
        playerHealth = GetComponent<Health>();
        startingPosition = transform.position;
        playerRespawn = null;
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

        PersistentPlayerHealth.Instance.AddHealth(PersistentPlayerHealth.Instance.MaxHealth);
        Vector3 respawnPosition = (playerRespawn != null) ? playerRespawn.position : startingPosition;
        transform.position = respawnPosition;
    }

}
