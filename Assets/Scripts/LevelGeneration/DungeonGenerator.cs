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
    public List<Tuple<int, int>> occupiedTiles;
    public int numberOfTiles = 10;
    private DungeonManager dungeonManager;
    private bool hasBossRoom;

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
        occupiedTiles = new List<Tuple<int, int>>();
        // Spawn the first tile at (0,0) and register its exits
        Room firstTile = Instantiate(startTilePrefab, Vector2.zero, Quaternion.identity);
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
        Tuple<int, int> secondTileLocation = new Tuple<int, int>(-1, 0);
        secondTile.location = secondTileLocation;
        occupiedTiles.Add(secondTileLocation);
        numberOfTiles = dungeonManager.DungeonSize;
        ExpandToMaxDungeon();
        SpawnPlayer();
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
            totalChance += roomTile.spawnChance;
        }
        float randomValue = UnityEngine.Random.Range(0f, totalChance);
        float currentChance = 0f;
        foreach (DungeonRoomType roomTile in roomGeneration.rules[room.Type][node.shouldGoTo])
        {
            currentChance += roomTile.spawnChance;
            if (randomValue <= currentChance)
            {
                return roomTile.tilePrefab;
            }
        }
        return null;
    }

    void SpawnTile(Room lastRoom, Node exitNode, bool isBossTile = false, bool fillEmpty = false)
    {
        Room instatiateObject = null;
        Room newTile = null;
        Node[] nodes = null;

        // Find the best entrance node (the one closest to exitNode)
        Node bestEntrance = null;
        int checkTime = 0;
        bool skipTile = false;
        while (bestEntrance == null && checkTime < 10) 
        {
            checkTime++;
            Tuple<int, int> newTileLocation = GetNextLocation(lastRoom, exitNode);
            Vector3 offsetPosition = GetOffsetValue(lastRoom, exitNode);
            if (offsetPosition == Vector3.zero || occupiedTiles.Contains(newTileLocation))
            {
                skipTile = true;
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

            if (occupiedTiles.Contains(newTileLocation))
            {
                activeNodes.Remove(exitNode);
                activeNodes.Remove(exitNode.pairedNode);
                skipTile = true;
                break;
            }
            AssignNodeDirections(newTile);
            nodes = newTile.GetComponentsInChildren<Node>();
            newTile.location = newTileLocation;
            skipTile = false;
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
            if (checkTime == 10)
            {
                newTile = null;
                DestroyImmediate(newTile);
                return;
            }
            else if (skipTile) 
            {
                newTile = null;
                DestroyImmediate(newTile);
                continue;
            }

            if (fillEmpty && !occupiedTiles.Contains(newTileLocation) && newTileLocation != new Tuple<int, int>(0, 0))
            {
                newTile.transform.position = offsetPosition;
                CheckForNewCameraBounds(newTile.transform.position);
                // Remove used exit from active list
                activeNodes.Remove(exitNode);
                activeNodes.Remove(exitNode.pairedNode);
                break;
            }
            if (bestEntrance == null)
            {
                Debug.Log("Failed to create");
                Destroy(newTile); // If no match found, discard the tile
            }
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

            CheckForNewCameraBounds(newTile.transform.position);
        }
        if (skipTile) 
        { 
            newTile = null;
            DestroyImmediate(newTile);
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
                if (node.isExit && node != bestEntrance && node != bestEntrance.pairedNode)
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

            if (activeNodes.Count > 0)
            {
                Node nodeToExpand = activeNodes[0]; // Use the first available exit node
                lastRoom = nodeToExpand.GetComponentInParent<Room>();
                SpawnTile(lastRoom, nodeToExpand, i == numberOfTiles);
            }
        }
        while (activeNodes.Count > 0)
        {
            Node nodeToExpand = activeNodes[0];
            lastRoom = nodeToExpand.GetComponentInParent<Room>();
            SpawnTile(lastRoom, nodeToExpand, false, true);
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
