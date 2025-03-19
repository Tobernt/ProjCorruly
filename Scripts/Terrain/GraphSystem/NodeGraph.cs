using System.Collections.Generic;
using UnityEngine;

public class NodeGraph : ScriptableObject
{
    public List<GraphNode> nodes = new List<GraphNode>();
    public List<NodeConnection> connections = new List<NodeConnection>();

    public void AddNode(GraphNode node)
    {
        if (!nodes.Contains(node))
        {
            nodes.Add(node);
        }
    }

    public void RemoveNode(GraphNode node)
    {
        if (nodes.Contains(node))
        {
            node.ClearAllConnections(); // ✅ Properly clear all input/output connections
            nodes.Remove(node);
            Debug.Log($"🗑 Node {node.name} fully removed from the graph.");
        }
    }


    public void AddConnection(GraphNode fromNode, GraphNode toNode)
    {
        if (!ConnectionExists(fromNode, toNode))
        {
            connections.Add(new NodeConnection(fromNode, toNode));
            fromNode.AddOutputConnection(toNode);
            toNode.AddInputConnection(fromNode);
        }
    }

    private bool ConnectionExists(GraphNode fromNode, GraphNode toNode)
    {
        return connections.Exists(c => c.fromNode == fromNode && c.toNode == toNode);
    }
}
