using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

public class GraphProcessor
{
    public NodeGraph Graph { get; private set; }

    [SerializeField] private int tileCountX = 2;
    [SerializeField] private int tileCountY = 2;
    [SerializeField] private float tileSize = 10f;

    public int TileCountX => tileCountX;
    public int TileCountY => tileCountY;
    public float TileSize => tileSize;

    public GraphProcessor(NodeGraph graph)
    {
        Graph = graph ?? ScriptableObject.CreateInstance<NodeGraph>();
    }

    public void SetTileCounts(int x, int y)
    {
        tileCountX = x;
        tileCountY = y;
    }

    public void SetTileSize(float size)
    {
        tileSize = size;
    }

    public void SetTileCountX(int count)
    {
        tileCountX = Mathf.Max(1, count);
        Debug.Log($"GraphProcessor: Set Tile Count X to {tileCountX}");
    }

    public void SetTileCountY(int count)
    {
        tileCountY = Mathf.Max(1, count);
        Debug.Log($"GraphProcessor: Set Tile Count Y to {tileCountY}");
    }
    private void ClearPreviousMeshes()
    {
        Debug.Log("🗑 Clearing previously generated meshes...");

        // Destroy all previous generated meshes to avoid duplicates
        foreach (GraphNode node in Graph.nodes)
        {
            if (node is MeshNode meshNode && meshNode.GeneratedMesh != null)
            {
                GameObject.DestroyImmediate(meshNode.GeneratedMesh.gameObject);
                meshNode.GeneratedMesh = null;
            }
        }
    }

    public List<ProBuilderMesh> ExecuteGraph()
    {
        if (Graph == null)
        {
            Debug.LogError("GraphProcessor: Graph is NULL!");
            return null;
        }

        Debug.Log("🔄 Manually Executing Graph");

        Dictionary<GraphNode, ProBuilderMesh> nodeMeshMap = new Dictionary<GraphNode, ProBuilderMesh>();
        ClearPreviousMeshes(); // Ensures old meshes are removed

        // Refresh graph dynamically from NodeGraphView
        Graph.nodes = Graph.nodes.Where(node => node != null).ToList();
        Debug.Log($"🔄 Graph refreshed. Active nodes: {Graph.nodes.Count}");

        foreach (GraphNode node in Graph.nodes)
        {
            if (node == null) continue;

            Debug.Log($"🛠 Processing Node: {node.name}");

            if (node is MeshNode meshNode)
            {
                ProBuilderMesh inputMesh = null;
                if (meshNode.inputConnections.Count > 0)
                {
                    GraphNode parentNode = meshNode.inputConnections[0];
                    if (nodeMeshMap.ContainsKey(parentNode))
                    {
                        inputMesh = nodeMeshMap[parentNode];
                    }
                    else
                    {
                        Debug.LogError($"❌ {node.name} couldn't retrieve a valid mesh from {parentNode.name}");
                        continue;
                    }
                }

                ProBuilderMesh generatedMesh = meshNode.GenerateMesh(inputMesh);
                if (generatedMesh != null)
                {
                    nodeMeshMap[node] = generatedMesh;
                    Debug.Log($"✅ {node.name} successfully processed mesh.");
                }
                else
                {
                    Debug.LogError($"❌ {node.name} failed to modify mesh!");
                }
            }
        }

        return nodeMeshMap.Values.ToList();
    }
}
