using UnityEngine;

[AutoCreatedSingleton]
public class DungeonManager : Singleton<DungeonManager>
{
    public int DungeonLevel = 0; // Tracks the dungeon level
    public int DungeonSize = 8;
    public int BaseDungeonSize = 8;
    public int EnemyRoomBaseCount = 4;
    public int LootRoomBaseCount = 3;

    // How many enemy and loot rooms a dungeon aims for (DungeonGenerator steers the room mix toward these):
    // two more enemy rooms every level, one more loot room every second level, capped at 40% and 20% of the
    // dungeon's size so the targets stay reachable once the dungeon stops growing
    public int EnemyRoomTarget => Mathf.Min(EnemyRoomBaseCount + 2 * Mathf.Max(0, DungeonLevel - 1), DungeonSize * 2 / 5);
    public int LootRoomTarget => Mathf.Min(LootRoomBaseCount + Mathf.Max(0, DungeonLevel - 1) / 2, DungeonSize / 5);
    public void RegenerateDungeon()
    {
        DungeonLevel++; // Increase dungeon level when regenerating
        GenerateDungeon();
    }

    void GenerateDungeon()
    {
        if (DungeonLevel < 5)
        {
            DungeonSize = BaseDungeonSize + BaseDungeonSize *DungeonLevel;
        }
    }
}
