using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;
using SuperTiled2Unity;

public class DungeonGenerator : MonoBehaviour
{
    public GameObject startTilePrefab;
    public GameObject bossTilePrefab;
    public GameObject[] tilePrefabs; // Assign tile prefabs in Inspector
    public List<Node> activeNodes = new List<Node>(); // Open connection points

    void Start()
    {
        // Spawn the first tile at (0,0) and register its exits
        GameObject firstTile = Instantiate(startTilePrefab, Vector2.zero, Quaternion.identity);
        foreach (Node node in firstTile.GetComponentsInChildren<Node>())
        {
            if (node.isExit)
            {
                node.shouldGoTo = NodeShouldGoTo.Right;
                activeNodes.Add(node);
            }
        }
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

    void SpawnTile(Node exitNode, bool isBossTile = false)
    {
        GameObject instatiateObject = null;
        GameObject newTile = null;
        Node[] nodes = null;

        // Find the best entrance node (the one closest to exitNode)
        Node bestEntrance = null;
        while (bestEntrance == null) 
        {
            instatiateObject = isBossTile ? bossTilePrefab : tilePrefabs[Random.Range(0, tilePrefabs.Length)];
            newTile = Instantiate(instatiateObject, Vector3.zero, Quaternion.identity);
            AssignNodeDirections(newTile);
            nodes = newTile.GetComponentsInChildren<Node>();
            foreach (Node node in nodes)
            {
                if (node.isEntrance && node.pairedNode != null)
                {
                    // Temporarily move the tile to test alignment
                    newTile.transform.position = GetOffsetValue(exitNode);
                    float epsilon = 0.01f;
                    Vector2 testOffsetMain = exitNode.transform.position - node.transform.position;
                    Vector2 testOffsetPaired = exitNode.pairedNode.transform.position - node.pairedNode.transform.position;

                    //if (!CheckForShouldGoToNode(exitNode.transform.position, newTile.transform.position, exitNode.shouldGoTo)) 
                    //{
                    //    continue;
                    //}
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

            if (bestEntrance == null)
            {
                Destroy(newTile); // If no match found, discard the tile
            }
        }
        if (bestEntrance != null && bestEntrance.pairedNode != null)
        {
            // Calculate offset for both entrance and its paired node
            Vector2 offsetMain = exitNode.transform.position - bestEntrance.transform.position;
            Vector2 offsetPaired = exitNode.pairedNode.transform.position - bestEntrance.pairedNode.transform.position;

            newTile.transform.position += (Vector3)((offsetMain + offsetPaired) / 2f);

            // Connect the nodes
            exitNode.connectedNodes.Add(bestEntrance);
            bestEntrance.connectedNodes.Add(exitNode);

            exitNode.pairedNode.connectedNodes.Add(bestEntrance.pairedNode);
            bestEntrance.pairedNode.connectedNodes.Add(exitNode.pairedNode);

            // Remove used exit from active list
            activeNodes.Remove(exitNode);
            activeNodes.Remove(exitNode.pairedNode);
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
        for (int i = 0; i <= 10; i++)
        {

            if (activeNodes.Count > 0)
            {
                Node nodeToExpand = activeNodes[0]; // Use the first available exit node
                SpawnTile(nodeToExpand, i == 10);
            }
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
}
