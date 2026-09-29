using UnityEngine;
using UnityEngine.SceneManagement;

public class GameRespawn : Singleton<GameRespawn>
{
    public float threshold = -200f;
    private Vector3 startingPosition;
    private Health playerHealth;

    // Last activated checkpoint, kept as scene + position rather than the checkpoint object so it
    // survives scene changes and save/load. It only applies while that scene is loaded.
    public string CheckpointScene { get; private set; }
    public Vector3 CheckpointPosition { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        if (IsDuplicate) return;
        playerHealth = GetComponent<Health>();
        startingPosition = transform.position;
    }

    public void SetPlayerRespawn(Transform newRespawnPoint)
    {
        SetCheckpoint(SceneManager.GetActiveScene().name, newRespawnPoint.position);
    }

    public void SetCheckpoint(string sceneName, Vector3 position)
    {
        CheckpointScene = sceneName;
        CheckpointPosition = position;
    }

    // Moves the player to the scene's "EntryPoint" and makes it the respawn position for this scene
    // unless a checkpoint here was activated
    public bool MoveToEntryPoint()
    {
        GameObject entryPoint = GameObject.Find("EntryPoint");
        if (entryPoint == null)
            return false;
        startingPosition = entryPoint.transform.position;
        transform.position = startingPosition;
        return true;
    }

    //Check if player dropped out of bounds, killing player gets the respawn button on screen
    void FixedUpdate()
    {
        if (transform.position.y < threshold)
        {
            playerHealth.Kill(); // Blocking, rolling or i-frames must not keep the player falling forever
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
        bool hasCheckpointHere = CheckpointScene == SceneManager.GetActiveScene().name;
        Vector3 respawnPosition = hasCheckpointHere ? CheckpointPosition : startingPosition;
        transform.position = respawnPosition;
    }

}
