using System.Collections.Generic;
using UnityEngine;

public class RoomGrid : MonoBehaviour
{
    public DungeonManager dungeonManager;
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
        dungeonManager = DungeonManager.Instance;
        dungeonManager.RegenerateDungeon();
        Initialize();
    }
    public void Initialize()
    {
        levelGrid = new Room[dungeonManager.GridWidth, dungeonManager.GridHeight];
        GenerateStartAndBossRooms();
        //NEW logic
        GenerateDungeonPath();
        ExpandWithSidePaths();
        AssignRoomTypesDynamically();
        ValidateConnectivity();

        //GenerateDungeonPath();

        //OLD logic
        //GenerateEnemyAndLootRooms();
        GenerateCorridorsAndParkours();
        AddDirectionBooleans();
        //// Place the remaining rooms (corridors and parkours) first
        //EnsureParkourInEachRow();
        // Instantiate the actual GameObjects
        InstantiateRooms();
        //SpawnPlayer();
    }
    

    private void GenerateStartAndBossRooms()
    {
        int gridWidth = dungeonManager.GridWidth;
        int gridHeight = dungeonManager.GridHeight;
        // Start room and boss room
        startRoomPos = new Vector2Int(0, Random.Range(1, gridHeight - 1));
        levelGrid[startRoomPos.x, startRoomPos.y] = new Room(RoomType.Start, startRoomPos);
        levelGrid[startRoomPos.x, startRoomPos.y].HasRightExit = true;
        bossRoomPos = new Vector2Int(gridWidth - 1, Random.Range(0, gridHeight));
        while (bossRoomPos.y == startRoomPos.y) // Ensure the boss room is not in the same row as the start room
        {
            bossRoomPos = new Vector2Int(gridWidth - 1, Random.Range(0, gridHeight));
        }
        levelGrid[bossRoomPos.x, bossRoomPos.y] = new Room(RoomType.Boss, bossRoomPos);
        levelGrid[bossRoomPos.x, bossRoomPos.y].HasLeftExit = true;
    }

    private void InitializeOneRoom(int x, int y)
    {
        Room room = levelGrid[x, y];
        if (room == null) return;

        GameObject prefab = GetPrefabForRoomType(room);
        Vector3 position = new Vector3(x * roomSize, y * roomSize, 0);
        Instantiate(prefab, position, Quaternion.identity);
    }

    void GenerateDungeonPath()
    {
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        stack.Push(startRoomPos);
        visited.Add(startRoomPos);

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Pop();
            Room currentRoom = levelGrid[current.x, current.y];
            List<Vector2Int> neighbors = GetNeighbors(currentRoom.GridPosition, visited);
            Debug.Log(neighbors.Count);
            if (neighbors.Count > 0)
            {
                stack.Push(current);

                Vector2Int chosenNeighbor = neighbors[Random.Range(0, neighbors.Count)];
                visited.Add(chosenNeighbor);

                // Ensure connectivity
                ConnectRooms(current, chosenNeighbor);

                stack.Push(chosenNeighbor);
                //InitializeOneRoom(chosenNeighbor.x , chosenNeighbor.y);
            }
        }
    }

    void ExpandWithSidePaths()
    {
        foreach (var room in levelGrid)
        {
            if (room == null || room.Type == RoomType.Start || room.Type == RoomType.Boss) continue;
            if (Random.value < 0.3f)
            {
                List<Vector2Int> sidePaths = GetNeighbors(room.GridPosition);
                if (sidePaths.Count > 0)
                {
                    Vector2Int chosenPath = sidePaths[Random.Range(0, sidePaths.Count)];
                    ConnectRooms(room.GridPosition, chosenPath);
                }
            }
        }
    }

    void ValidateConnectivity()
    {
        HashSet<Vector2Int> reachableRooms = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(startRoomPos);
        reachableRooms.Add(startRoomPos);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (Vector2Int neighbor in GetNeighbors(current))
            {
                if (!reachableRooms.Contains(neighbor))
                {
                    reachableRooms.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        foreach (var room in levelGrid)
        {
            if (room != null && !reachableRooms.Contains(room.GridPosition))
            {
                room.Type = RoomType.Corridor;
            }
        }
    }

    private void AssignRoomTypesDynamically()
    {
        GenerateEnemyAndLootRooms();
    }
    void ConnectRooms(Vector2Int from, Vector2Int to)
    {
        if (levelGrid[from.x, from.y] == null)
        {
            levelGrid[from.x, from.y] = new Room(RoomType.Empty, from);
        }
        Room roomA = levelGrid[from.x, from.y];
        if (levelGrid[to.x, to.y] == null)
        {
            levelGrid[to.x, to.y] = new Room(RoomType.Empty, to);
        }
        Room roomB = levelGrid[to.x, to.y];

        if (from.x < to.x) // Right connection
        {
            roomA.HasRightExit = true;
            roomB.HasLeftExit = true;
        }
        else if (from.x > to.x) // Left connection
        {
            roomA.HasLeftExit = true;
            roomB.HasRightExit = true;
        }
        else if (from.y < to.y) // Up connection
        {
            roomA.HasTopExit = true;
            roomB.HasBottomExit = true;
        }
        else if (from.y > to.y) // Down connection
        {
            roomA.HasBottomExit = true;
            roomB.HasTopExit = true;
        }
    }
    void EnsureParkourInEachRow()
    {
        for (int y = 0; y < dungeonManager.GridHeight; y++)
        {
            bool hasParkour = false;
            List<Vector2Int> corridorPositions = new List<Vector2Int>();

            for (int x = 1; x < dungeonManager.GridWidth - 1; x++)
            {
                if (levelGrid[x, y]?.Type == RoomType.Parkour)
                    hasParkour = true;
                else if (levelGrid[x, y]?.Type == RoomType.Corridor)
                    corridorPositions.Add(new Vector2Int(x, y));
            }

            if (!hasParkour && corridorPositions.Count > 0)
            {
                Vector2Int chosenPosition = corridorPositions[Random.Range(0, corridorPositions.Count)];
                levelGrid[chosenPosition.x, chosenPosition.y] = new Room(RoomType.Parkour, chosenPosition);
            }
        }
    }

    List<Vector2Int> GetNeighbors(Vector2Int pos, HashSet<Vector2Int> visited = null)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        int x = pos.x, y = pos.y;

        Vector2Int[] directions = { new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(0, 1) };

        foreach (var dir in directions)
        {
            Vector2Int newPos = pos + dir;
            if (newPos.x >= 0 && newPos.x < dungeonManager.GridWidth && newPos.y >= 0 && newPos.y < dungeonManager.GridHeight)
            {
                if (visited == null || !visited.Contains(newPos))
                {
                    neighbors.Add(newPos);
                }
            }
        }
        return neighbors;
    }

    private List<Vector2Int> GetNeighborsOld(Room current, HashSet<Vector2Int> visited = null)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        int x = current.GridPosition.x;
        int y = current.GridPosition.y;

        bool checkVisited = visited != null; // Check if we should filter visited nodes
        void TryAddNeighbor(int nx, int ny)
        {
            if (nx >= 0 && nx < dungeonManager.GridWidth && ny >= 0 && ny < dungeonManager.GridHeight)
            {
                Room neighborRoom = levelGrid[nx, ny];
                if (neighborRoom != null) // Ensure both rooms have exits
                {
                    if (!checkVisited || !visited.Contains(new Vector2Int(nx, ny)))
                    {
                        neighbors.Add(neighborRoom.GridPosition);
                    }
                }
            }
        }

        // Check and add valid neighbors
        if (x - 1 >= 0)
        {
            Room neighborRoom = levelGrid[x - 1, y];
            if (neighborRoom == null)
            {
                neighborRoom = new Room(RoomType.Empty, new Vector2Int(x - 1, y));
                levelGrid[x - 1, y] = neighborRoom;
            }
            if (neighborRoom != null && neighborRoom.Type == RoomType.Empty)
            {
                ConnectRooms(current.GridPosition, new Vector2Int(x - 1, y));
            }
            TryAddNeighbor(x - 1, y); // Left
        }
        if (x + 1 < dungeonManager.GridWidth)
        {
            Room neighborRoom = levelGrid[x + 1, y];
            if (neighborRoom == null)
            {
                neighborRoom = new Room(RoomType.Empty, new Vector2Int(x + 1, y)); 
                levelGrid[x + 1, y] = neighborRoom;
            }
            if (neighborRoom != null && neighborRoom.Type == RoomType.Empty)
            {
                ConnectRooms(current.GridPosition, new Vector2Int(x + 1, y));
            }
            TryAddNeighbor(x + 1, y); // Right
        }
        if (y - 1 >= 0)
        {
            Room neighborRoom = levelGrid[x, y - 1];
            if (neighborRoom == null)
            {
                neighborRoom = new Room(RoomType.Empty, new Vector2Int(x, y - 1));
                levelGrid[x, y - 1] = neighborRoom;
            }
            if (neighborRoom != null && neighborRoom.Type == RoomType.Empty)
            {
                ConnectRooms(current.GridPosition, new Vector2Int(x, y - 1));
            }
            TryAddNeighbor(x, y - 1); // Bottom
        }
        if (y + 1 < dungeonManager.GridHeight)
        {
            Room neighborRoom = levelGrid[x, y + 1];
            if (neighborRoom == null)
            {
                neighborRoom = new Room(RoomType.Empty, new Vector2Int(x, y + 1));
                levelGrid[x, y + 1] = neighborRoom;
            }
            if (neighborRoom != null && neighborRoom.Type == RoomType.Empty)
            {
                ConnectRooms(current.GridPosition, new Vector2Int(x, y + 1));
            }
            TryAddNeighbor(x, y + 1); // Top
        }

        return neighbors;
    }
    private int GetTotalRoomCount()
    {
        int count = 0;
        foreach (var room in levelGrid)
        {
            if (room != null && room.Type != RoomType.Empty)
                count++;
        }
        return count;
    }

    bool IsDungeonConnected()
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(startRoomPos);
        visited.Add(startRoomPos);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            Room room = levelGrid[current.x, current.y];

            foreach (Vector2Int neighbor in GetNeighbors(room.GridPosition))
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return visited.Count == GetTotalRoomCount();
    }
    void GenerateCorridorsAndParkours()
    {
        int gridWidth = dungeonManager.GridWidth;
        int gridHeight = dungeonManager.GridHeight;
        for (int x = 1; x < gridWidth - 1; x++) // Avoid first and last columns for corridors/parkours
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (levelGrid[x, y] == null || levelGrid[x, y].Type == RoomType.Empty) 
                { 
                    // Decide randomly whether to place a corridor or parkour room
                    RoomType roomType = GetRandomRoomType();
                    levelGrid[x, y] = new Room(roomType, new Vector2Int(x, y));
                }
                // Skip already filled positions
            }
        }
    }
    void GenerateEnemyAndLootRooms()
    {
        int dungeonLevel = dungeonManager.DungeonLevel;
        int gridWidth = dungeonManager.GridWidth;
        int gridHeight = dungeonManager.GridHeight;
        int baseEnemyRooms = dungeonManager.EnemyRoomBaseCount;
        int baseLootRooms = dungeonManager.LootRoomBaseCount;
        int totalRooms = gridWidth * gridHeight - 2; //minus the start and boss
        int extraRooms = Mathf.Max(0, totalRooms - 20); // Only count rooms beyond the first 25
        int enemyCountRoom = (baseEnemyRooms + dungeonLevel / 2)+ Mathf.FloorToInt((extraRooms / 5)); // Increase by 1 per 5 extra rooms
        int lootCountRoom = (baseLootRooms + dungeonLevel / 3) + Mathf.FloorToInt((extraRooms / 5));  // Same logic for loot

        int enemyRoomsPlaced = 0;
        int lootRoomsPlaced = 0;

        // Place Enemy and Loot rooms
        while (enemyRoomsPlaced < enemyCountRoom || lootRoomsPlaced < lootCountRoom)
        {
            int x = Random.Range(0, gridWidth);
            int y = Random.Range(0, gridHeight);
            if (levelGrid[x, y] == null || levelGrid[x, y].Type == RoomType.Empty)
            {
                RoomType rotype = GetLootOrEnemyRoomType(enemyRoomsPlaced < enemyCountRoom, lootRoomsPlaced < lootCountRoom);

                if (rotype == RoomType.Enemy && x + 1 < gridWidth && levelGrid[x + 1, y]?.Type == RoomType.Boss)
                {
                    continue; // Skip this placement and try again
                }

                levelGrid[x, y] = new Room(rotype, new Vector2Int(x, y));

                if (rotype == RoomType.Enemy) enemyRoomsPlaced++;
                else lootRoomsPlaced++;
            } // Skip already filled positions
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

    void EnsureParkourInEachRowOld()
    {
        int gridWidth = dungeonManager.GridWidth;
        int gridHeight = dungeonManager.GridHeight;
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
        int gridWidth = dungeonManager.GridWidth;
        int gridHeight = dungeonManager.GridHeight;
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
        int gridWidth = dungeonManager.GridWidth;
        int gridHeight = dungeonManager.GridHeight;
        //in this function i need to go around the grid, and for each room in the grid check if it has
        //left right top or bottom neighbour and if so switch the required boolean
        //public bool HasTopExit, HasBottomExit, HasLeftExit, HasRightExit;
        List<RoomType> corridorNeighbors = new List<RoomType>() { RoomType.Enemy, RoomType.Loot };
        List<RoomType> parkourNeighbors = new List<RoomType>() { RoomType.Enemy, RoomType.Loot };
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
                        levelGrid[x, y - 1] != null
                        &&
                        IsNeighbourLE(room, levelGrid[x, y - 1], room.Type == RoomType.Corridor ? corridorNeighbors : parkourNeighbors)
                    ) // Bottom neighbor
                    room.HasBottomExit = true;
                if (
                        y < gridHeight - 1 && 
                        levelGrid[x, y + 1] != null
                        &&
                        IsNeighbourLE(room, levelGrid[x, y + 1], room.Type == RoomType.Corridor ? corridorNeighbors : parkourNeighbors)
                    ) // Top neighbor
                    room.HasTopExit = true;
            }
        }
    }

    private bool IsNeighbourLE(Room target, Room neighbor, List<RoomType> neighborRoomTypes)
    {
        if ((target.Type == RoomType.Corridor || target.Type == RoomType.Parkour) && neighborRoomTypes.Contains(neighbor.Type))
        {
            return false;
        }
        return true;
    }

    private void SpawnPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player not found in the scene!");
            return;
        }
        GameObject entryPoint = GameObject.Find("EntryPoint");
        if (entryPoint == null)
        {
            Debug.LogError("EntryPoint not found in the scene!");
            return;
        }
        player.transform.position = entryPoint.transform.position;
    }
}
