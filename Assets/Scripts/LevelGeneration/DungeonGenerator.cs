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
                activeNodes.Add(node);
            }
        }
    }

    void SpawnTile(Node exitNode, bool isBossTile = false)
    {
        GameObject instatiateObject = isBossTile ? bossTilePrefab : tilePrefabs[Random.Range(0, tilePrefabs.Length)];
        GameObject newTile = Instantiate(instatiateObject, exitNode.transform.position, Quaternion.identity);
        Node[] nodes = newTile.GetComponentsInChildren<Node>();

        // Find the best entrance node (the one closest to exitNode)
        Node bestEntrance = null;
        float shortestDistance = Mathf.Infinity;
        foreach (Node node in nodes)
        {
            if (node.isEntrance && node.pairedNode != null)
            {
                float distance = Vector2.Distance(exitNode.transform.position, node.transform.position);
                float pairedDistance = Vector2.Distance(exitNode.pairedNode.transform.position, node.pairedNode.transform.position);

                // Ensure both distances are minimal and they match correctly
                if (distance + pairedDistance < shortestDistance)
                {
                    shortestDistance = distance + pairedDistance;
                    bestEntrance = node;
                }
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
        foreach (Node node in nodes)
        {
            if (node.isExit && node != bestEntrance && node != bestEntrance.pairedNode)
            {
                activeNodes.Add(node);
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
