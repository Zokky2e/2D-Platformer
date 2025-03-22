using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.MaterialProperty;
using static UnityEngine.Rendering.DebugUI.Table;

public class RoomGrid : MonoBehaviour
{
    public int gridWidth = 5;
    public int gridHeight = 5;
    public float roomSize = 12f; // Set room size for positioning
    public Room[,] levelGrid;
    public Vector2Int startRoomPos;
    public Vector2Int bossRoomPos;
    // Assign these prefabs in the Unity Inspector
    public List<GameObject> enemyRoomPrefabVarients;
    public List<GameObject> lootRoomPrefabVarients;
    public List<GameObject> corridorPrefabVarients;
    public List<GameObject> parkourPrefabVarients;
    public GameObject startRoomPrefab;
    public GameObject bossRoomPrefab;
    public GameObject emptyRoomPrefab;
    public void Start()
    {
        Initialize();
    }
    public void Initialize()
    {
        levelGrid = new Room[gridWidth, gridHeight];

        // Start room and boss room
        startRoomPos = new Vector2Int(0, Random.Range(1, gridHeight - 1));
        levelGrid[startRoomPos.x, startRoomPos.y] = new Room(RoomType.Start, startRoomPos);
        bossRoomPos = new Vector2Int(gridWidth - 1, Random.Range(0, gridHeight));
        while (bossRoomPos.y == startRoomPos.y) // Ensure the boss room is not in the same row as the start room
        {
            bossRoomPos = new Vector2Int(gridWidth - 1, Random.Range(0, gridHeight));
        }
        levelGrid[bossRoomPos.x, bossRoomPos.y] = new Room(RoomType.Boss, bossRoomPos);

        // Place the remaining rooms (corridors and parkours) first
        GenerateEnemyAndLootRooms();
        GenerateCorridorsAndParkours();
        EnsureParkourInEachRow();
        AddDirectionBooleans();
        // Instantiate the actual GameObjects
        InstantiateRooms();
    }

    void GenerateCorridorsAndParkours()
    {
        for (int x = 1; x < gridWidth - 1; x++) // Avoid first and last columns for corridors/parkours
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (levelGrid[x, y] != null) continue; // Skip already filled positions

                // Decide randomly whether to place a corridor or parkour room
                RoomType roomType = GetRandomRoomType();
                levelGrid[x, y] = new Room(roomType, new Vector2Int(x, y));
            }
        }
    }
    void GenerateEnemyAndLootRooms()
    {
        int totalRooms = gridWidth * gridHeight - 2; //minus the start and boss
        int enemyCountRoom = Mathf.FloorToInt(totalRooms / 25f) + 4; // 4 enemies by default, increase by 1 per 5 tiles
        int lootCountRoom = Mathf.FloorToInt(totalRooms / 25f) + 3; // 3 loot rooms by default, increase by 1 per 5 tiles

        int enemyRoomsPlaced = 0;
        int lootRoomsPlaced = 0;

        // Place Enemy and Loot rooms
        while (enemyRoomsPlaced < enemyCountRoom || lootRoomsPlaced < lootCountRoom)
        {
            int x = Random.Range(0, gridWidth);
            int y = Random.Range(0, gridHeight);
            if (levelGrid[x, y] != null) continue; // Skip already filled positions

            RoomType rotype = GetLootOrEnemyRoomType(enemyRoomsPlaced < enemyCountRoom, lootRoomsPlaced < lootCountRoom);

            if (rotype == RoomType.Enemy && x + 1 < gridWidth && levelGrid[x + 1, y]?.Type == RoomType.Boss)
            {
                continue; // Skip this placement and try again
            }

            levelGrid[x, y] = new Room(rotype, new Vector2Int(x, y));

            if (rotype == RoomType.Enemy) enemyRoomsPlaced++;
            else lootRoomsPlaced++;
        }
    }

    private RoomType GetLootOrEnemyRoomType(bool canBeEnemy, bool canBeLoot)
    {
        if (canBeEnemy && canBeLoot)
        {
            // Randomly choose between Enemy and Loot for the remaining rooms
            return Random.value < 0.5f ? RoomType.Enemy: RoomType.Loot;
        }
        else if (canBeEnemy)
            return RoomType.Enemy;
        else
            return RoomType.Loot;
    }

    void EnsureParkourInEachRow()
    {
        for (int y = 0; y < gridHeight; y++) // For each row
        {
            bool hasParkour = false;

            // Check if this row already has a parkour room
            for (int x = 1; x < gridWidth - 1; x++) // Avoid first and last columns
            {
                if (levelGrid[x, y]?.Type == RoomType.Parkour)
                {
                    hasParkour = true;
                    break;
                }
            }

            // If the row does not contain a parkour room, add one
            if (!hasParkour)
            {
                // Find an empty spot in the row to place the parkour room
                for (int x = 1; x < gridWidth - 1; x++) // Avoid first and last columns
                {
                    if (levelGrid[x, y] != null && levelGrid[x, y].Type == RoomType.Corridor)
                    {
                        levelGrid[x, y] = new Room(RoomType.Parkour, new Vector2Int(x, y));
                        break; // Stop once we've placed the parkour room
                    }
                }
            }
        }
    }

    RoomType GetRandomRoomType()
    {
        // Randomly choose between corridor and parkour for the remaining rooms
        return Random.value < 0.5f ? RoomType.Corridor : RoomType.Parkour;
    }

    void InstantiateRooms()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Room room = levelGrid[x, y];
                if (room == null) continue;

                GameObject prefab = GetPrefabForRoomType(room);
                Vector3 position = new Vector3(x * roomSize, y * roomSize, 0);
                Instantiate(prefab, position, Quaternion.identity);
            }
        }
    }
    GameObject GetPrefabForRoomType(Room room)
    {
        switch (room.Type)
        {
            case RoomType.Enemy: return GetRoomPrefabLR(room, prefabVariants: enemyRoomPrefabVarients);
            case RoomType.Loot: return GetRoomPrefabLR(room, prefabVariants: lootRoomPrefabVarients);
            case RoomType.Corridor: return GetRoomPrefabBT(room, prefabVariants: corridorPrefabVarients);
            case RoomType.Parkour: return GetRoomPrefabBT(room, prefabVariants: parkourPrefabVarients);
            case RoomType.Boss: return bossRoomPrefab;
            case RoomType.Start: return startRoomPrefab;
            default: return emptyRoomPrefab;
        }
    }
    private GameObject GetRoomPrefabLR(Room room, List<GameObject> prefabVariants)
    {
        if (room.HasLeftExit && room.HasRightExit)
        {
            return prefabVariants[0];
        }
        else if(room.HasLeftExit)
        {
            return prefabVariants[1];
        }
        else
        {
            return prefabVariants[2];
        }
    }
    private GameObject GetRoomPrefabBT(Room room, List<GameObject> prefabVariants)
    {
        if (room.HasBottomExit && room.HasTopExit)
        {
            return prefabVariants[0];
        }
        else if (room.HasTopExit)
        {
            return prefabVariants[1];
        }
        else if (room.HasBottomExit) 
        {
            return prefabVariants[2];
        }
        else
        {
            return prefabVariants[3];
        }
    }
    private void AddDirectionBooleans()
    {
        //in this function i need to go around the grid, and for each room in the grid check if it has
        //left right top or bottom neighbour and if so switch the required boolean
        //public bool HasTopExit, HasBottomExit, HasLeftExit, HasRightExit;
        List<RoomType> corridorNeighbors = new List<RoomType>() { RoomType.Parkour };
        List<RoomType> parkourNeighbors = new List<RoomType>() { RoomType.Corridor, RoomType.Parkour };
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Room room = levelGrid[x, y];
                if (room == null)
                {
                    levelGrid[x, y] = new Room(RoomType.Empty, new Vector2Int(x, y));
                    room = levelGrid[x, y];
                }

                // Check for neighboring rooms and set exits accordingly
                if (x > 0 && levelGrid[x - 1, y] != null) // Left neighbor
                    room.HasLeftExit = true;
                if (x < gridWidth - 1 && levelGrid[x + 1, y] != null) // Right neighbor
                    room.HasRightExit = true;
                if (
                        y > 0 && 
                        levelGrid[x, y - 1] != null && 
                        IsNeighbourLE(room, levelGrid[x, y - 1], room.Type == RoomType.Corridor ? corridorNeighbors : parkourNeighbors)
                    ) // Bottom neighbor
                    room.HasBottomExit = true;
                if (
                        y < gridHeight - 1 && 
                        levelGrid[x, y + 1] != null && 
                        IsNeighbourLE(room, levelGrid[x, y + 1], room.Type == RoomType.Corridor ? corridorNeighbors : parkourNeighbors)
                    ) // Top neighbor
                    room.HasTopExit = true;
            }
        }
    }

    private bool IsNeighbourLE(Room target, Room neighbor, List<RoomType> neighborRoomTypes)
    {
        if ((target.Type == RoomType.Corridor || target.Type == RoomType.Parkour) && !neighborRoomTypes.Contains(neighbor.Type))
        {
            return false;
        }
        return true;
    }

}
