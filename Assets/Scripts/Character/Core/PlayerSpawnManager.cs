using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    void Start()
    {
        string lastExit = PlayerPrefs.GetString("LastExit", "");
        if (!string.IsNullOrEmpty(lastExit))
        {
            GameObject spawnPoint = GameObject.Find(lastExit);
            if (spawnPoint != null)
            {
                transform.position = spawnPoint.transform.position;
            }
        }
    }
}
