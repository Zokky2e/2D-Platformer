using UnityEngine;

public class DungeonManager : Singleton<DungeonManager>
{
    public int DungeonLevel = 0; // Tracks the dungeon level
    public int BaseGridWidth = 4;
    public int BaseGridHeight = 4;
    public int GridWidth = 4;
    public int GridHeight = 4;
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
            GridWidth = BaseGridWidth + DungeonLevel;  // Increase size with level
            GridHeight = BaseGridHeight + DungeonLevel;
        }
    }
}
