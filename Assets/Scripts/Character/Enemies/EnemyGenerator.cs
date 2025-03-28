using UnityEngine;

public class EnemyGenerator : MonoBehaviour
{
    public EnemyType[] enemies; // Assign in Inspector
    public GameObject patrolPointPrefab; // Assign in Inspector
    [System.Serializable]
    public struct EnemyType
    {
        public GameObject enemyPrefab;
        public float spawnChance; // Probability %
    }
    public Transform spawnPoint; // Where the enemy appears

    void Start()
    {
        SpawnEnemy();
    }
    void SpawnEnemy()
    {
        float totalChance = 0f;
        foreach (var enemy in enemies)
        {
            totalChance += enemy.spawnChance;
        }

        float randomValue = Random.Range(0f, totalChance);
        float currentChance = 0f;

        foreach (var enemy in enemies)
        {
            currentChance += enemy.spawnChance;
            if (randomValue <= currentChance)
            {
                if (enemy.enemyPrefab != null)
                {
                    GameObject spawnedEnemy = Instantiate(enemy.enemyPrefab, spawnPoint.position, Quaternion.identity);
                    GameObject patrolPoint = Instantiate(patrolPointPrefab, spawnPoint.position, Quaternion.identity);
                    // Generate patrol points and assign them to the enemy
                    Enemy enemyScript = spawnedEnemy.GetComponent<Enemy>();
                    if (enemyScript != null)
                    {
                        enemyScript.patrolPoints = new Transform[] { patrolPoint.transform };

                    }
                }
                return;
            }
        }
    }
    void Update()
    {
        
    }
}
