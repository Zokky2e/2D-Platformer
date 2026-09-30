using UnityEngine;
using System.Collections.Generic;
using SuperTiled2Unity;
using System;
using System.Linq;
public class DungeonGenerator : MonoBehaviour
{
    
    public Room startTilePrefab;
    public Room bossTilePrefab;
    public Room emptyTilePrefab;
    public List<Node> activeNodes = new List<Node>(); // Open connection points
    [NonSerialized] public List<Tuple<int, int>> occupiedTiles;
    public int numberOfTiles = 10;
    private DungeonManager dungeonManager;
    private bool hasBossRoom;
    // Every room this generator placed, so a dungeon without a boss room (and so without an exit) can be
    // torn down and built again
    private readonly List<GameObject> spawnedRooms = new List<GameObject>();
    private const int MaxBuildAttempts = 5;

    [Header("Room mix")]
    [Tooltip("Weight multiplier for enemy or loot rooms once the dungeon has as many as DungeonManager aims for")]
    [SerializeField] private float overTargetWeight = 0.1f;
    [Tooltip("Weight multiplier for enemy or loot rooms that are still missing when few rooms are left to place")]
    [SerializeField] private float catchUpWeight = 10f;
    private readonly Dictionary<RoomType, int> roomTargets = new Dictionary<RoomType, int>();
    private readonly Dictionary<RoomType, int> roomCounts = new Dictionary<RoomType, int>();
    private int expansionStepsLeft;

    [SerializeField]
    private CameraFollow camera;
    private Vector2 lowestPoint = new Vector2(-11, -11);
    private Vector2 highestPoint = new Vector2(12, 12);
    [SerializeField]
    private RoomGeneration roomGeneration;

    void Start()
    {
    }

    private void Awake()
    {
        hasBossRoom = false;
        dungeonManager = DungeonManager.Instance;
        roomGeneration.InitializeRules();
        GenerateDungeon();
    }

    void GenerateDungeon()
    {
        dungeonManager.RegenerateDungeon();
        numberOfTiles = dungeonManager.DungeonSize;
        // The boss room holds the only exit, and the fill phase can only place it at an open exit facing
        // right. If the layout left none, build a new layout rather than trap the player
        // The room mix is steered toward the targets while building; a layout that still misses them is
        // rebuilt too, but the last attempt is kept if it has an exit
        roomTargets[RoomType.Enemy] = dungeonManager.EnemyRoomTarget;
        roomTargets[RoomType.Loot] = dungeonManager.LootRoomTarget;
        for (int attempt = 1; attempt <= MaxBuildAttempts; attempt++)
        {
            BuildDungeon();
            if (hasBossRoom && MissingRooms() == 0)
                break;
            Debug.LogWarning(hasBossRoom
                ? $"Dungeon layout {attempt} has {RoomMix()}, short of the targets; building another"
                : $"Dungeon layout {attempt} had no room for the boss room (the exit); building another");
            if (attempt < MaxBuildAttempts)
                ClearDungeon();
        }
        if (!hasBossRoom)
            Debug.LogError("No dungeon layout had room for the boss room, so this dungeon has no exit");
        SpawnPlayer();
    }

    void BuildDungeon()
    {
        occupiedTiles = new List<Tuple<int, int>>();
        roomCounts.Clear();
        // Spawn the first tile at (0,0) and register its exits
        Room firstTile = Instantiate(startTilePrefab, Vector2.zero, Quaternion.identity);
        spawnedRooms.Add(firstTile.gameObject);
        Tuple<int, int> firstTileLocation = new Tuple<int, int>(0, 0);
        occupiedTiles.Add(firstTileLocation);
        foreach (Node node in firstTile.GetComponentsInChildren<Node>())
        {
            if (node.isExit)
            {
                node.shouldGoTo = NodeShouldGoTo.Right;
                firstTile.location = firstTileLocation;
                activeNodes.Add(node);
            }
        }
        SuperMap map = firstTile.GetComponentInParent<SuperMap>();
        Room secondTile = Instantiate(emptyTilePrefab, new Vector3(-map.m_Width, 0) + map.transform.position, Quaternion.identity);
        spawnedRooms.Add(secondTile.gameObject);
        Tuple<int, int> secondTileLocation = new Tuple<int, int>(-1, 0);
        secondTile.location = secondTileLocation;
        occupiedTiles.Add(secondTileLocation);
        ExpandToMaxDungeon();
    }

    // Removes everything BuildDungeon placed. Immediate, because the next layout is built in the same frame
    // and must not collide with the old rooms. Their Start (enemy and loot spawning) never ran
    void ClearDungeon()
    {
        foreach (GameObject room in spawnedRooms)
        {
            if (room != null)
                DestroyImmediate(room);
        }
        spawnedRooms.Clear();
        activeNodes.Clear();
        hasBossRoom = false;
        lowestPoint = new Vector2(-11, -11);
        highestPoint = new Vector2(12, 12);
    }

    void CheckForNewCameraBounds(Vector3 tilePosition)
    {
        lowestPoint.x   =   tilePosition.x < lowestPoint.x     ? tilePosition.x -2 : lowestPoint.x;
        lowestPoint.y   =   tilePosition.y < lowestPoint.y     ? tilePosition.y -12 : lowestPoint.y;
        highestPoint.x  =   tilePosition.x > highestPoint.x    ? tilePosition.x +12 : highestPoint.x;
        highestPoint.y  =   tilePosition.y > highestPoint.y    ? tilePosition.y +2 : highestPoint.y;
        camera.minBounds = new Vector2(lowestPoint.x, lowestPoint.y - 2);
        camera.maxBounds =  new Vector2(highestPoint.x + 14, highestPoint.y);
    }

    Vector3 GetOffsetValue(Room tile, Node exitNode)
    {
        SuperMap map = tile.GetComponentInParent<SuperMap>();
        Vector3 parentPosition = map.transform.position;
        int width = 12;
        int height = 12;
        if (map != null)
        {
            width = map.m_Width;
            height = map.m_Height;
        }
        switch (exitNode.shouldGoTo)
        {
            case NodeShouldGoTo.Left:
                return new Vector3(-width, 0) + parentPosition;
            case NodeShouldGoTo.Right:
                return new Vector3(width, 0) + parentPosition;
            case NodeShouldGoTo.Top:
                return new Vector3(0, height) + parentPosition;
            case NodeShouldGoTo.Bottom:
                return new Vector3(0, -height) + parentPosition;
            default:
                return Vector3.zero;
        }
    }

    Tuple<int, int> GetNextLocation(Room room, Node exitNode)
    {
        var tileLoc = room.location;
        switch (exitNode.shouldGoTo)
        {
            case NodeShouldGoTo.Left:
                return new Tuple<int, int>(room.location.Item1 - 1, room.location.Item2);
            case NodeShouldGoTo.Right:
                return new Tuple<int, int>(room.location.Item1 + 1, room.location.Item2);
            case NodeShouldGoTo.Top:
                return new Tuple<int, int>(room.location.Item1, room.location.Item2 + 1);
            case NodeShouldGoTo.Bottom:
                return new Tuple<int, int>(room.location.Item1, room.location.Item2 - 1);
            default:
                return tileLoc;
        }
    }

    Vector3 GetTileCenter(Room tile)
    {
        Bounds bounds = new Bounds(tile.transform.position, Vector3.zero);

        foreach (Renderer r in tile.GetComponentsInChildren<Renderer>())
        {
            bounds.Encapsulate(r.bounds);
        }

        return bounds.center;
    }
    void AssignNodeDirections(Room tile)
    {
        Node[] nodes = tile.GetComponentsInChildren<Node>();
        Vector3 center = GetTileCenter(tile);

        foreach (Node node in nodes)
        {
            Vector3 localPos = node.transform.position - center;

            if (Mathf.Abs(localPos.x) > Mathf.Abs(localPos.y))
            {
                node.shouldGoTo = localPos.x > 0 ? NodeShouldGoTo.Right : NodeShouldGoTo.Left;
            }
            else
            {
                node.shouldGoTo = localPos.y > 0 ? NodeShouldGoTo.Top : NodeShouldGoTo.Bottom;
            }
        }
    }
    Room ChooseRandomDungeonTile(Room room, Node node)
    {
        float totalChance = 0f;

        foreach (DungeonRoomType roomTile in roomGeneration.rules[room.Type][node.shouldGoTo])
        {
            totalChance += RoomWeight(roomTile);
        }
        float randomValue = UnityEngine.Random.Range(0f, totalChance);
        float currentChance = 0f;
        foreach (DungeonRoomType roomTile in roomGeneration.rules[room.Type][node.shouldGoTo])
        {
            currentChance += RoomWeight(roomTile);
            if (randomValue <= currentChance)
            {
                return roomTile.tilePrefab;
            }
        }
        return null;
    }

    // The rule's weight, adjusted toward DungeonManager's enemy and loot room targets: rarer once a type has
    // enough rooms, much likelier while it's short and the rooms left to place barely cover what's missing
    float RoomWeight(DungeonRoomType option)
    {
        RoomType type = option.tilePrefab.Type;
        if (!roomTargets.TryGetValue(type, out int target))
            return option.spawnChance;
        int placed = roomCounts.TryGetValue(type, out int count) ? count : 0;
        if (placed >= target)
            return option.spawnChance * overTargetWeight;
        return MissingRooms() >= expansionStepsLeft ? option.spawnChance * catchUpWeight : option.spawnChance;
    }

    int MissingRooms()
    {
        int missing = 0;
        foreach (KeyValuePair<RoomType, int> target in roomTargets)
            missing += Mathf.Max(0, target.Value - (roomCounts.TryGetValue(target.Key, out int count) ? count : 0));
        return missing;
    }

    string RoomMix() =>
        string.Join(", ", roomTargets.Select(t => $"{(roomCounts.TryGetValue(t.Key, out int n) ? n : 0)}/{t.Value} {t.Key} rooms"));

    void SpawnTile(Room lastRoom, Node exitNode, bool fillEmpty = false)
    {
        Room instatiateObject = null;
        Room newTile = null;
        Node[] nodes = null;

        // Find the best entrance node (the one closest to exitNode)
        Node bestEntrance = null;
        int checkTime = 0;
        while (bestEntrance == null && checkTime < 10)
        {
            checkTime++;
            Tuple<int, int> newTileLocation = GetNextLocation(lastRoom, exitNode);
            Vector3 offsetPosition = GetOffsetValue(lastRoom, exitNode);
            if (offsetPosition == Vector3.zero || occupiedTiles.Contains(newTileLocation))
            {
                continue;
            }
            if (fillEmpty)
                if (!hasBossRoom && exitNode.shouldGoTo == NodeShouldGoTo.Right)
                {
                    hasBossRoom = true;
                    instatiateObject = bossTilePrefab;
                }
                else
                    instatiateObject = emptyTilePrefab;
            else
                    instatiateObject = ChooseRandomDungeonTile(lastRoom, exitNode);
            newTile = Instantiate(instatiateObject, Vector3.zero, Quaternion.identity);
            spawnedRooms.Add(newTile.gameObject);

            AssignNodeDirections(newTile);
            nodes = newTile.GetComponentsInChildren<Node>();
            newTile.location = newTileLocation;
            foreach (Node node in nodes)
            {
                if (node.isEntrance && node.pairedNode != null)
                {
                    // Temporarily move the tile to test alignment
                    newTile.transform.position = offsetPosition;
                    float epsilon = 0.01f;
                    Vector2 testOffsetMain = exitNode.transform.position - node.transform.position;
                    Vector2 testOffsetPaired = exitNode.pairedNode.transform.position - node.pairedNode.transform.position;
                    if ((testOffsetMain).sqrMagnitude < epsilon * epsilon && (testOffsetPaired).sqrMagnitude < epsilon * epsilon)
                    {
                        newTile.transform.position = Vector3.zero;
                        bestEntrance = node;
                        bestEntrance.isExit = false;
                        bestEntrance.pairedNode.isExit = false;
                        newTile.transform.position = offsetPosition;
                        break;
                    }
                    // Reset position before trying next entrance

                    newTile.transform.position = Vector3.zero;
                }
            }
            if (bestEntrance != null)
                break; // An entrance lines up; the tile is already moved into place

            if (fillEmpty && newTileLocation != new Tuple<int, int>(0, 0))
            {
                // Fillers (empty caps, boss room) are placed without a matching entrance
                newTile.transform.position = offsetPosition;
                occupiedTiles.Add(newTileLocation); // So no second filler lands on the same cell
                CheckForNewCameraBounds(newTile.transform.position);
                // Remove used exit from active list
                activeNodes.Remove(exitNode);
                activeNodes.Remove(exitNode.pairedNode);
                break;
            }

            // No entrance lines up: discard the whole tile (destroying only the Room component left
            // the tilemap behind at the origin) and roll again
            Debug.Log("Failed to create");
            spawnedRooms.Remove(newTile.gameObject);
            DestroyImmediate(newTile.gameObject);
            newTile = null;
            nodes = null;

            if (checkTime == 10 && !fillEmpty)
                return; // Leave the exit open; a later pass or the fill phase deals with it
        }
        if (bestEntrance != null && bestEntrance.pairedNode != null)
        {
            // Connect the nodes
            exitNode.connectedNodes.Add(bestEntrance);
            bestEntrance.connectedNodes.Add(exitNode);

            exitNode.pairedNode.connectedNodes.Add(bestEntrance.pairedNode);
            bestEntrance.pairedNode.connectedNodes.Add(exitNode.pairedNode);

            // Add newTileLocation to occupiedTiles
            occupiedTiles.Add(newTile.location);
            roomCounts[newTile.Type] = (roomCounts.TryGetValue(newTile.Type, out int placed) ? placed : 0) + 1;

            CheckForNewCameraBounds(newTile.transform.position);
        }

        // Remove used exit from active list
        activeNodes.Remove(exitNode);
        activeNodes.Remove(exitNode.pairedNode);

        // Add remaining exit nodes to active list
        if (nodes != null && nodes.Length > 0) 
        {
            nodes = nodes.OrderBy(n => (int)n.shouldGoTo).ToArray();
            foreach (Node node in nodes)
            {
                bool isUsedEntrance = bestEntrance != null && (node == bestEntrance || node == bestEntrance.pairedNode);
                if (node.isExit && !isUsedEntrance)
                {
                    node.isExit = true;
                    node.isEntrance = false;
                    activeNodes.Add(node);
                }
            }
        }
    }

    public void ExpandToMaxDungeon()
    {
        Room lastRoom = null;
        for (int i = 0; i <= numberOfTiles; i++)
        {
            expansionStepsLeft = numberOfTiles - i + 1;

            if (activeNodes.Count > 0)
            {
                Node nodeToExpand = activeNodes[0]; // Use the first available exit node
                lastRoom = nodeToExpand.GetComponentInParent<Room>();
                SpawnTile(lastRoom, nodeToExpand);
            }
        }
        while (activeNodes.Count > 0)
        {
            Node nodeToExpand = activeNodes[0];
            lastRoom = nodeToExpand.GetComponentInParent<Room>();
            SpawnTile(lastRoom, nodeToExpand, fillEmpty: true);
        }
    }

    public void ExpandDungeon()
    {
        Room lastRoom = null;
        if (activeNodes.Count > 0)
        {
            Node nodeToExpand = activeNodes[0]; // Use the first available exit node
            lastRoom = nodeToExpand.GetComponentInParent<Room>();
            SpawnTile(lastRoom, nodeToExpand);
        }
    }

    private void SpawnPlayer()
    {
        if (PersistentPlayerHealth.Instance == null)
        {
            Debug.LogError("Player not found in the scene! Enter Play mode from Level0.");
            return;
        }
        if (!GameRespawn.Instance.MoveToEntryPoint())
        {
            Debug.LogError("EntryPoint not found in the scene!");
            return;
        }
        SaveSystem.Instance.Save();
    }
}
