using UnityEngine;

public class GameRespawn : Singleton<GameRespawn>
{
    public float threshold = -200f;
    private Transform playerRespawn;
    private Vector3 startingPosition;
    private Health playerHealth;

    protected override void Awake()
    {
        base.Awake();
        if (IsDuplicate) return;
        playerHealth = GetComponent<Health>();
        startingPosition = transform.position;
        playerRespawn = null;
    }

    public void SetPlayerRespawn(Transform newRespawnPoint)
    {
        playerRespawn = newRespawnPoint;
    }

    // Moves the player to the scene's "EntryPoint" and makes it the fallback respawn position,
    // since checkpoints from the previous scene no longer exist
    public bool MoveToEntryPoint()
    {
        GameObject entryPoint = GameObject.Find("EntryPoint");
        if (entryPoint == null)
            return false;
        startingPosition = entryPoint.transform.position;
        playerRespawn = null;
        transform.position = startingPosition;
        return true;
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
