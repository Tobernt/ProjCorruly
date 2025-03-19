using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class GraphNode : ScriptableObject
{
    public List<GraphNode> inputConnections = new List<GraphNode>();
    public List<GraphNode> outputConnections = new List<GraphNode>();

    public Dictionary<int, GraphNode> inputSockets = new Dictionary<int, GraphNode>();
    public Dictionary<int, GraphNode> outputSockets = new Dictionary<int, GraphNode>();

    public void AddInputConnection(GraphNode inputNode)
    {
        if (!inputConnections.Contains(inputNode))
        {
            inputConnections.Add(inputNode);
            int portIndex = inputSockets.Count; // Assign new socket index
            inputSockets[portIndex] = inputNode;
        }
    }

    public void AddOutputConnection(GraphNode outputNode)
    {
        if (!outputConnections.Contains(outputNode))
        {
            outputConnections.Add(outputNode);
            int portIndex = outputSockets.Count; // Assign new socket index
            outputSockets[portIndex] = outputNode;
        }
    }

    public void RemoveInputConnection(GraphNode inputNode)
    {
        if (inputConnections.Contains(inputNode))
        {
            inputConnections.Remove(inputNode);
            int keyToRemove = -1;
            foreach (var entry in inputSockets)
            {
                if (entry.Value == inputNode)
                {
                    keyToRemove = entry.Key;
                    break;
                }
            }
            if (keyToRemove != -1)
                inputSockets.Remove(keyToRemove);
        }
    }

    public void RemoveOutputConnection(GraphNode outputNode)
    {
        if (outputConnections.Contains(outputNode))
        {
            outputConnections.Remove(outputNode);
            int keyToRemove = -1;
            foreach (var entry in outputSockets)
            {
                if (entry.Value == outputNode)
                {
                    keyToRemove = entry.Key;
                    break;
                }
            }
            if (keyToRemove != -1)
                outputSockets.Remove(keyToRemove);
        }
    }
    public void ClearAllConnections()
    {
        foreach (var inputNode in inputConnections.ToList()) // Avoid modifying while iterating
        {
            inputNode.RemoveOutputConnection(this);
        }
        inputConnections.Clear();

        foreach (var outputNode in outputConnections.ToList()) // Avoid modifying while iterating
        {
            outputNode.RemoveInputConnection(this);
        }
        outputConnections.Clear();

        inputSockets.Clear();
        outputSockets.Clear();

        Debug.Log($"🗑 Cleared all connections for {this.name}");
    }
}
