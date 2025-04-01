using UnityEngine;
using System.Collections.Generic;

public class Node : MonoBehaviour
{
    public List<Node> connectedNodes = new List<Node>(); // Stores connected nodes
    public bool isEntrance = false; // Mark as entrance
    public bool isExit = false; // Mark as exit
    public Node pairedNode;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Node otherNode = other.GetComponent<Node>();
        if (otherNode != null && !connectedNodes.Contains(otherNode))
        {
            connectedNodes.Add(otherNode); // Connect to another node
        }
    }
}
