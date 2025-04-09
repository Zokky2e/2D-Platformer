using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameRespawn : Singleton<GameRespawn>
{
    public float threshold = -200f;
    private Transform playerRespawn;
    private Vector3 startingPosition;
    private Health playerHealth;

    protected override void Awake()
    {
        base.Awake();
        playerHealth = GetComponent<Health>();
        startingPosition = transform.position;
        playerRespawn = null;
    }
    
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
