using UnityEngine;

public class DungeonManager : Singleton<DungeonManager>
{
    public int DungeonLevel = 0; // Tracks the dungeon level
    public int DungeonSize = 8;
    public int BaseDungeonSize = 8;
    public int EnemyRoomBaseCount = 4;
    public int LootRoomBaseCount = 3;
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
