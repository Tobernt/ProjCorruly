using UnityEngine;

public class NodeConnection
{
    public GraphNode fromNode;
    public GraphNode toNode;

    public NodeConnection(GraphNode from, GraphNode to)
    {
        fromNode = from;
        toNode = to;
    }
}
