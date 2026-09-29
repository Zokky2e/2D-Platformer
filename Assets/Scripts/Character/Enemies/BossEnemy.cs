using UnityEngine;

// Turns an Enemy into a dungeon boss. It gets stronger with each dungeon level, keeps its room's exit
// locked until it dies, then pays out gold and reveals the room's reward chest. The exit and the chest are
// found in the same Room, so the boss must be placed inside a room prefab (see bossRoom).
[RequireComponent(typeof(Enemy), typeof(Health), typeof(CharacterStats))]
public class BossEnemy : MonoBehaviour
{
    public string bossName = "Bandit Chief";

    [Header("Added per dungeon level after the first")]
    public float healthPerLevel = 75f;
    public float damagePerLevel = 4f;
    public float armorPerLevel = 2f;

    [Header("Reward")]
    public int goldReward = 30;
    public int goldPerLevel = 20;

    private Health health;
    private LevelTransition exit;
    private LootChest rewardChest;
    private int extraLevels;

    private void Start()
    {
        health = GetComponent<Health>();
        CharacterStats stats = GetComponent<CharacterStats>();

        extraLevels = Mathf.Max(0, DungeonManager.Instance.DungeonLevel - 1);
        health.baseHealth += healthPerLevel * extraLevels;
        stats.baseDamage += damagePerLevel * extraLevels;
        stats.baseArmor += armorPerLevel * extraLevels;
        health.SetHealth(health.MaxHealth);
        health.Died += OnBossDied;

        Room room = GetComponentInParent<Room>();
        if (room != null)
        {
            exit = room.GetComponentInChildren<LevelTransition>(true);
            rewardChest = room.GetComponentInChildren<LootChest>(true);
        }
        if (exit != null)
            exit.Lock($"The way out is sealed. Defeat the {bossName} first.");
        if (rewardChest != null)
            rewardChest.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (health != null)
            health.Died -= OnBossDied;
    }

    private void OnBossDied()
    {
        if (exit != null)
            exit.Unlock();
        if (rewardChest != null)
            rewardChest.gameObject.SetActive(true);

        int gold = goldReward + goldPerLevel * extraLevels;
        InventorySystem.Instance.UpdateGold(gold);
        string chestLine = rewardChest != null ? " and left its treasure chest behind" : "";
        DialogSystem.Instance.ShowDialog(bossName,
            $"The {bossName} is defeated! It dropped {gold} gold{chestLine}.\nThe way back to the village is open.", null);
    }
}
