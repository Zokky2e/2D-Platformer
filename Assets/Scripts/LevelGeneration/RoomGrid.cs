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
    public List<Vector2Int> enemyRoomPositions = new List<Vector2Int>();
    public List<Vector2Int> lootRoomPositions = new List<Vector2Int>();
    // Assign these prefabs in the Unity Inspector
    public GameObject enemyRoomPrefab;
    public GameObject lootRoomPrefab;
    public GameObject corridorPrefab;
    public GameObject parkourPrefab;
    public GameObject startRoomPrefab;
    public GameObject bossRoomPrefab;
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
        GenerateCorridorsAndParkours();
        GenerateEnemyAndLootRooms();
        EnsureParkourInEachRow();
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

        int roomsPlaced = 0;
        int enemyRoomsPlaced = 0;
        int lootRoomsPlaced = 0;

        // Place Enemy and Loot rooms
        for (int x = 0; x < gridWidth; x++) // Avoid first and last columns
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (levelGrid[x, y] != null) continue; // Skip already filled positions

                RoomType rotype = GetLootOrEnemyRoomType(enemyRoomsPlaced < enemyCountRoom, lootRoomsPlaced < lootCountRoom);
                levelGrid[x, y] = new Room(rotype, new Vector2Int(x, y));
                if (rotype == RoomType.Enemy)
                {
                    enemyRoomsPlaced++;
                }
                else
                {
                    lootRoomsPlaced++;
                }

                roomsPlaced++;

                if (enemyRoomsPlaced == enemyCountRoom && lootRoomsPlaced == lootCountRoom) // We have placed enough rooms
                    break;
            }
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

                GameObject prefab = GetPrefabForRoomType(room.Type);
                Vector3 position = new Vector3(x * roomSize, y * roomSize, 0);
                Instantiate(prefab, position, Quaternion.identity);
            }
        }
    }
    GameObject GetPrefabForRoomType(RoomType type)
    {
        switch (type)
        {
            case RoomType.Enemy: return enemyRoomPrefab;
            case RoomType.Loot: return lootRoomPrefab;
            case RoomType.Corridor: return corridorPrefab;
            case RoomType.Parkour: return parkourPrefab;
            case RoomType.Boss: return bossRoomPrefab;
            default: return startRoomPrefab;
        }
    }
}
