using System.Linq;
using UnityEditor;
using UnityEngine;

// Play-mode shortcuts for testing content that can't be obtained in-game yet
public static class DebugMenu
{
    // items.json ids of the weapons with on-hit bleed, poison or burn
    private static readonly int[] StatusEffectWeapons = { 0, 2, 5, 7, 10, 13 };

    [MenuItem("Tools/Debug/Give Status Effect Weapons")]
    private static void GiveStatusEffectWeapons()
    {
        ItemSystem.Instance.AddToPlayerInventory(StatusEffectWeapons);
        Debug.Log("Added the bleed, poison and burn weapons to the inventory");
    }

    [MenuItem("Tools/Debug/Give Status Effect Weapons", true)]
    private static bool CanGiveStatusEffectWeapons() => Application.isPlaying;

    [MenuItem("Tools/Debug/Give All Items")]
    private static void GiveAllItems()
    {
        int[] ids = ItemDatabase.Instance.AllItems.Select(item => item.Id).ToArray();
        ItemSystem.Instance.AddToPlayerInventory(ids);
        Debug.Log($"Added all {ids.Length} items to the inventory");
    }

    [MenuItem("Tools/Debug/Give All Items", true)]
    private static bool CanGiveAllItems() => Application.isPlaying;

    [MenuItem("Tools/Debug/Give 100 Gold")]
    private static void GiveGold()
    {
        InventorySystem.Instance.UpdateGold(100);
    }

    [MenuItem("Tools/Debug/Give 100 Gold", true)]
    private static bool CanGiveGold() => Application.isPlaying;

    [MenuItem("Tools/Debug/Spawn Enemy/Bandit")]
    private static void SpawnBandit() => SpawnEnemy("Bandit");

    [MenuItem("Tools/Debug/Spawn Enemy/Goblin")]
    private static void SpawnGoblin() => SpawnEnemy("Goblin");

    [MenuItem("Tools/Debug/Spawn Enemy/Mushroom")]
    private static void SpawnMushroom() => SpawnEnemy("Mushroom");

    [MenuItem("Tools/Debug/Spawn Enemy/Skeleton")]
    private static void SpawnSkeleton() => SpawnEnemy("Skeleton");

    [MenuItem("Tools/Debug/Spawn Enemy/Bandit", true)]
    [MenuItem("Tools/Debug/Spawn Enemy/Goblin", true)]
    [MenuItem("Tools/Debug/Spawn Enemy/Mushroom", true)]
    [MenuItem("Tools/Debug/Spawn Enemy/Skeleton", true)]
    private static bool CanSpawnEnemy() => Application.isPlaying && PersistentPlayerHealth.Instance != null;

    // Drops an enemy from Prefabs/Enemies a few units in front of the player
    private static void SpawnEnemy(string prefabName)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Enemies/{prefabName}.prefab");
        if (prefab == null)
        {
            Debug.LogWarning($"No enemy prefab at Assets/Prefabs/Enemies/{prefabName}.prefab");
            return;
        }
        Hero hero = PersistentPlayerHealth.Instance.GetComponent<Hero>();
        Vector3 position = hero.transform.position + new Vector3(hero.FacingDirection * 3f, 0.5f, 0f);
        Object.Instantiate(prefab, position, Quaternion.identity);
    }
}
