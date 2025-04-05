using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;

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

    bool CheckForShouldGoToNode(Vector2 exitNode, Vector2 entryNode, NodeShouldGoTo shouldGoTo)
    {
        switch (shouldGoTo) 
        {
            case NodeShouldGoTo.Left:
                if (exitNode.x > entryNode.x) return true;
                break;  
            case NodeShouldGoTo.Right:
                if (exitNode.x < entryNode.x) return true;
                break;
            case NodeShouldGoTo.Top:
                if (exitNode.y < entryNode.y) return true;
                break;
            case NodeShouldGoTo.Bottom:
                if (exitNode.y > entryNode.y) return true;
                break;  
            default:
                return false;
        }
        return false;
    }

    void SetNodeShouldGoTo(Node exitNode, Node entryNode)
    {
        Vector2 desiredExitPos = (exitNode.transform.position + exitNode.pairedNode.transform.position) / 2f;
        Vector2 desiredEntryPos = (entryNode.transform.position + entryNode.pairedNode.transform.position) / 2f;

        if (desiredEntryPos.y == desiredExitPos.y)
        {
            if (desiredEntryPos.x < desiredExitPos.x) 
            {
                exitNode.shouldGoTo = NodeShouldGoTo.Left;
            }
            else
            {
                exitNode.shouldGoTo = NodeShouldGoTo.Right;
            }
        }
        else
        {
            if (desiredEntryPos.y < desiredExitPos.y)
            {
                exitNode.shouldGoTo = NodeShouldGoTo.Top;
            }
            else
            {
                exitNode.shouldGoTo = NodeShouldGoTo.Bottom;
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
            nodes = newTile.GetComponentsInChildren<Node>();
            foreach (Node node in nodes)
            {
                if (node.isEntrance && node.pairedNode != null)
                {
                    Vector2 offsetMain = exitNode.transform.position - node.transform.position;
                    Vector2 offsetPaired = exitNode.pairedNode.transform.position - node.pairedNode.transform.position;
                    Vector2 desiredOffset = (offsetMain + offsetPaired) / 2f;
                    // Temporarily move the tile to test alignment
                    newTile.transform.position = desiredOffset;
                    float epsilon = 0.01f;
                    Vector2 testOffsetMain = exitNode.transform.position - node.transform.position;
                    Vector2 testOffsetPaired = exitNode.pairedNode.transform.position - node.pairedNode.transform.position;

                    //if (!CheckForShouldGoToNode(exitNode.transform.position, newTile.transform.position, exitNode.shouldGoTo)) 
                    //{
                    //    continue;
                    //}
                    if ((testOffsetMain - testOffsetPaired).sqrMagnitude < epsilon * epsilon)
                    {
                        bestEntrance = node;
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
                    SetNodeShouldGoTo(node, bestEntrance);
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
