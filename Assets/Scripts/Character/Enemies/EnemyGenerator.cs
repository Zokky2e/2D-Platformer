using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Spawns one enemy at spawnPoint, picked by weight from the entries unlocked at the current dungeon level.
// An entry without a prefab is a chance of no enemy.
public class EnemyGenerator : MonoBehaviour
{
    public EnemyType[] enemies; // Assign in Inspector
    public GameObject patrolPointPrefab; // Assign in Inspector
    [System.Serializable]
    public struct EnemyType
    {
        public GameObject enemyPrefab;
        public float spawnChance; // Weight, relative to the other unlocked entries
        public int minDungeonLevel; // Only spawns from this dungeon level on
    }
    public Transform spawnPoint; // Where the enemy appears

    void Start()
    {
        SpawnEnemy();
    }

    void SpawnEnemy()
    {
        int level = DungeonManager.Instance.DungeonLevel;
        List<EnemyType> available = enemies.Where(enemy => enemy.minDungeonLevel <= level).ToList();
        float totalChance = available.Sum(enemy => enemy.spawnChance);

        float randomValue = Random.Range(0f, totalChance);
        float currentChance = 0f;

        foreach (var enemy in available)
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
}
