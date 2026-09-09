using System.Collections.Generic;
using UnityEngine;

public class GraphDebugger
{
    private NodeGraph _graph;

    public GraphDebugger(NodeGraph graph)
    {
        _graph = graph;
    }

    public void PrintExecutionOrder()
    {
        List<GraphNode> executionOrder = new List<GraphNode>();
        foreach (var node in _graph.nodes)
        {
            executionOrder.Add(node);
        }

        Debug.Log("🛠️ Execution Order:");
        foreach (var node in executionOrder)
        {
            Debug.Log($"➡️ {node.GetType().Name}");
        }
    }

    public void HighlightInvalidConnections()
    {
        foreach (var connection in _graph.connections) // Now this works!
        {
            if (connection.fromNode == connection.toNode)
            {
                Debug.LogError($"❌ Invalid Connection: {connection.fromNode} loops to itself!");
            }
        }
    }
}
