using UnityEngine;
using System.Collections.Generic;
using SuperTiled2Unity;
using System;
public class DungeonGenerator : MonoBehaviour
{
    [System.Serializable]
    public struct DungeonRoomType
    {
        public GameObject tilePrefab;
        public float spawnChance; // Probability %
        //could extend with aditional logic
    }
    public GameObject startTilePrefab;
    public GameObject bossTilePrefab;
    public GameObject emptyTilePrefab;
    public DungeonRoomType[] roomTilePrefabs;
    public List<Node> activeNodes = new List<Node>(); // Open connection points
    public List<Tuple<int, int>> occupiedTiles;
    public int numberOfTiles = 10;
    private DungeonManager dungeonManager;
    private bool hasBossRoom;

    [SerializeField]
    private CameraFollow camera;
    private Vector2 lowestPoint = new Vector2(-11, -11);
    private Vector2 highestPoint = new Vector2(12, 12);

    void Start()
    {
        hasBossRoom = false;
        dungeonManager = DungeonManager.Instance;
        dungeonManager.RegenerateDungeon();
        occupiedTiles = new List<Tuple<int, int>>();
        // Spawn the first tile at (0,0) and register its exits
        GameObject firstTile = Instantiate(startTilePrefab, Vector2.zero, Quaternion.identity);
        Tuple<int, int> firstTileLocation = new Tuple<int, int>(0, 0);
        occupiedTiles.Add(firstTileLocation);
        foreach (Node node in firstTile.GetComponentsInChildren<Node>())
        {
            if (node.isExit)
            {
                node.shouldGoTo = NodeShouldGoTo.Right;
                node.tileLocation = firstTileLocation;
                activeNodes.Add(node);
            }
        }
        SuperMap map = firstTile.GetComponentInParent<SuperMap>();
        GameObject secondTile = Instantiate(emptyTilePrefab, new Vector3(-map.m_Width, 0) + map.transform.position, Quaternion.identity);
        Tuple<int, int> secondTileLocation = new Tuple<int, int>(-1, 0);
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

    Vector3 GetOffsetValue(Node exitNode)
    {
        SuperMap map = exitNode.GetComponentInParent<SuperMap>();
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
        }
        return Vector3.zero;
    }

    Tuple<int, int> GetNextLocation(Node exitNode)
    {
        Debug.Log(exitNode.tileLocation.Item1);
        Debug.Log(exitNode.tileLocation.Item2);
        Debug.Log(exitNode.shouldGoTo);
        switch (exitNode.shouldGoTo)
        {
            case NodeShouldGoTo.Left:
                return new Tuple<int, int>(exitNode.tileLocation.Item1 - 1, exitNode.tileLocation.Item2);
            case NodeShouldGoTo.Right:
                return new Tuple<int, int>(exitNode.tileLocation.Item1 + 1, exitNode.tileLocation.Item2);
            case NodeShouldGoTo.Top:
                return new Tuple<int, int>(exitNode.tileLocation.Item1, exitNode.tileLocation.Item2 + 1);
            case NodeShouldGoTo.Bottom:
                return new Tuple<int, int>(exitNode.tileLocation.Item1, exitNode.tileLocation.Item2 - 1);
        }
        return new Tuple<int, int>(0, 0);
    }

    Vector3 GetTileCenter(GameObject tile)
    {
        Bounds bounds = new Bounds(tile.transform.position, Vector3.zero);

        foreach (Renderer r in tile.GetComponentsInChildren<Renderer>())
        {
            bounds.Encapsulate(r.bounds);
        }

        return bounds.center;
    }
    void AssignNodeDirections(GameObject tile)
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
    GameObject ChooseRandomDungeonTile()
    {
        float totalChance = 0f;

        foreach (var roomTile in roomTilePrefabs)
        {
            totalChance += roomTile.spawnChance;
        }
        float randomValue = UnityEngine.Random.Range(0f, totalChance);
        float currentChance = 0f;
        foreach (var roomTile in roomTilePrefabs)
        {
            currentChance += roomTile.spawnChance;
            if (randomValue <= currentChance)
            {
                if (roomTile.tilePrefab != null)
                {
                    return roomTile.tilePrefab;
                }
                return null;
            }
        }
        return null;
    }

    void SpawnTile(Node exitNode, bool isBossTile = false, bool fillEmpty = false)
    {
        GameObject instatiateObject = null;
        GameObject newTile = null;
        Node[] nodes = null;

        // Find the best entrance node (the one closest to exitNode)
        Node bestEntrance = null;
        int checkTime = 0;
        while (bestEntrance == null && checkTime < 10) 
        {
            checkTime++;
            if (fillEmpty)
                if (!hasBossRoom && exitNode.shouldGoTo == NodeShouldGoTo.Right)
                {
                    hasBossRoom = true;
                    instatiateObject = bossTilePrefab;
                }
                else
                    instatiateObject = emptyTilePrefab;
            else
                    instatiateObject = ChooseRandomDungeonTile();
            newTile = Instantiate(instatiateObject, Vector3.zero, Quaternion.identity);
            Tuple<int, int> newTileLocation = GetNextLocation(exitNode);

            Debug.Log(newTileLocation.Item1);
            Debug.Log(newTileLocation.Item2);
            if (occupiedTiles.Contains(newTileLocation))
            {
                activeNodes.Remove(exitNode);
                activeNodes.Remove(exitNode.pairedNode);
                break;
            }
            AssignNodeDirections(newTile);
            nodes = newTile.GetComponentsInChildren<Node>();
            foreach (Node node in nodes)
            {
                node.tileLocation = newTileLocation;
            }
            foreach (Node node in nodes)
            {
                if (node.isEntrance && node.pairedNode != null)
                {
                    // Temporarily move the tile to test alignment
                    newTile.transform.position = GetOffsetValue(exitNode);
                    float epsilon = 0.01f;
                    Vector2 testOffsetMain = exitNode.transform.position - node.transform.position;
                    Vector2 testOffsetPaired = exitNode.pairedNode.transform.position - node.pairedNode.transform.position;
                    if ((testOffsetMain).sqrMagnitude < epsilon * epsilon && (testOffsetPaired).sqrMagnitude < epsilon * epsilon)
                    {
                        bestEntrance = node;
                        bestEntrance.isExit = false;
                        bestEntrance.pairedNode.isExit = false;
                        newTile.transform.position = GetOffsetValue(exitNode);
                        break;
                    }
                    // Reset position before trying next entrance

                    newTile.transform.position = Vector3.zero;
                }
            }
            if (fillEmpty && !occupiedTiles.Contains(newTileLocation) && newTileLocation != new Tuple<int, int>(0, 0))
            {
                newTile.transform.position = GetOffsetValue(exitNode);
                CheckForNewCameraBounds(newTile.transform.position);
                // Remove used exit from active list
                activeNodes.Remove(exitNode);
                activeNodes.Remove(exitNode.pairedNode);
                break;
            }
            if (bestEntrance == null)
            {
                Destroy(newTile); // If no match found, discard the tile
            }
            if (checkTime == 10)
            {
                return;
            }
        }
        if (newTile != null && newTile.transform.position == Vector3.zero) 
        {
            Destroy(newTile); // If tile should spawn on vector3.zero
            return;
        }
        if (bestEntrance != null && bestEntrance.pairedNode != null)
        {
            // Calculate offset for both entrance and its paired node
            Vector2 offsetMain = exitNode.transform.position - bestEntrance.transform.position;
            Vector2 offsetPaired = exitNode.pairedNode.transform.position - bestEntrance.pairedNode.transform.position;

            //newTile.transform.position += (Vector3)((offsetMain + offsetPaired) / 2f);

            // Connect the nodes
            exitNode.connectedNodes.Add(bestEntrance);
            bestEntrance.connectedNodes.Add(exitNode);

            exitNode.pairedNode.connectedNodes.Add(bestEntrance.pairedNode);
            bestEntrance.pairedNode.connectedNodes.Add(exitNode.pairedNode);

            // Remove used exit from active list
            activeNodes.Remove(exitNode);
            activeNodes.Remove(exitNode.pairedNode);

            // Add newTileLocation to occupiedTiles
            occupiedTiles.Add(bestEntrance.tileLocation);

            CheckForNewCameraBounds(newTile.transform.position);
        }
        // Add remaining exit nodes to active list
        if (nodes != null && nodes.Length > 0) 
        {
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
        for (int i = 0; i <= numberOfTiles; i++)
        {

            if (activeNodes.Count > 0)
            {
                Node nodeToExpand = activeNodes[0]; // Use the first available exit node
                SpawnTile(nodeToExpand, i == numberOfTiles);
            }
        }
        while (activeNodes.Count > 0)
        {
            Node nodeToExpand = activeNodes[0];
            SpawnTile(nodeToExpand, false, true);
        }
    }

    public void ExpandDungeon()
    {
        if (activeNodes.Count > 0)
        {
            Node nodeToExpand = activeNodes[0]; // Use the first available exit node
            SpawnTile(nodeToExpand);
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
