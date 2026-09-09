using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public class NodeGraphView : GraphView
{
    public event Action<GraphNode> OnNodeSelected;
    private NodeGraph _graph;

    public NodeGraphView(NodeGraph graph)
    {
        _graph = graph;

        style.flexGrow = 1;
        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        GridBackground grid = new GridBackground();
        Insert(0, grid);

        graphViewChanged += OnGraphViewChanged;
        this.RegisterCallback<GeometryChangedEvent>(evt => CleanupDeletedNodes()); // Listen for changes

    }
    private void CleanupDeletedNodes()
    {
        if (_graph == null) return;

        // Remove nodes that no longer exist in the UI
        List<GraphNode> toRemove = _graph.nodes
            .Where(node => this.Q<NodeView>(node.name) == null) // Corrected null check
            .ToList();

        foreach (GraphNode node in toRemove)
        {
            _graph.RemoveNode(node);
            Debug.Log($"🗑 Cleaned up missing node: {node.name}");
        }
    }

    public void DeleteNode(NodeView nodeView)
    {
        if (_graph == null || nodeView == null) return;

        GraphNode node = nodeView.graphNode;

        if (node != null)
        {
            // Remove all connections
            foreach (var inputNode in node.inputConnections.ToList())
            {
                inputNode.RemoveOutputConnection(node);
                node.RemoveInputConnection(inputNode);
            }
            foreach (var outputNode in node.outputConnections.ToList())
            {
                outputNode.RemoveInputConnection(node);
                node.RemoveOutputConnection(outputNode);
            }

            // Remove node from graph
            _graph.RemoveNode(node);
            Debug.Log($"🗑 Deleted node: {node.name}");

            // Destroy the generated mesh if applicable
            if (node is PlaneNode planeNode && planeNode.GeneratedMesh != null)
            {
                GameObject.DestroyImmediate(planeNode.GeneratedMesh.gameObject);
                planeNode.GeneratedMesh = null;
                Debug.Log($"🗑 Destroyed generated mesh for {node.name}");
            }

            // Remove from UI
            RemoveElement(nodeView);
        }
    }

    public void RegisterNode(NodeView nodeView)
    {
        nodeView.NodeSelected += (selectedNode) => OnNodeSelected?.Invoke(selectedNode.graphNode);
        {
            Debug.Log($"Node Selected: {nodeView.title}");
            OnNodeSelected?.Invoke(nodeView.graphNode);
        };

        AddElement(nodeView);
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        // Primitive Nodes
        evt.menu.AppendAction("Primitive Nodes/Plane", action => CreateNode(typeof(PlaneNode)));
        evt.menu.AppendAction("Primitive Nodes/Sphere", action => CreateNode(typeof(SphereNode)));
        evt.menu.AppendAction("Primitive Nodes/Cylinder", action => CreateNode(typeof(CylinderNode)));
        evt.menu.AppendAction("Primitive Nodes/Primitive (Cube, Torus, Cone)", action => CreateNode(typeof(PrimitiveNode)));

        // Boolean Operations
        evt.menu.AppendAction("Boolean Nodes/Union", action => CreateNode(typeof(BooleanUnionNode)));
        evt.menu.AppendAction("Boolean Nodes/Subtract", action => CreateNode(typeof(BooleanSubtractNode)));
        evt.menu.AppendAction("Boolean Nodes/Intersect", action => CreateNode(typeof(BooleanIntersectNode)));
        evt.menu.AppendAction("Boolean Nodes/Generic Boolean", action => CreateNode(typeof(BooleanNode)));

        // Modification Nodes
        evt.menu.AppendAction("Modification Nodes/Noise Deformer", action => CreateNode(typeof(NoiseDeformerNode)));
        evt.menu.AppendAction("Modification Nodes/Extrude", action => CreateNode(typeof(ExtrudeNode)));
        evt.menu.AppendAction("Modification Nodes/Smooth", action => CreateNode(typeof(SmoothNode)));

        // Material Nodes
        evt.menu.AppendAction("Material Nodes/Material", action => CreateNode(typeof(MaterialNode)));
        evt.menu.AppendAction("Material Nodes/Blend Shader", action => CreateNode(typeof(BlendShaderNode)));
        evt.menu.AppendAction("Material Nodes/Mask Input", action => CreateNode(typeof(MaskNode)));

        // Special Nodes
        evt.menu.AppendAction("Special Nodes/Spline", action => CreateNode(typeof(SplineNode)));
        evt.menu.AppendAction("Special Nodes/UV Mapping", action => CreateNode(typeof(UVMappingNode)));
        evt.menu.AppendAction("Special Nodes/Erosion", action => CreateNode(typeof(ErosionNode)));

        //Prefabs
        evt.menu.AppendAction("Prefab Nodes/Prefab Scatter", action => CreateNode(typeof(PrefabScatterNode)));

        // Export Nodes
        evt.menu.AppendAction("Export Nodes/Mesh Exporter", action => CreateNode(typeof(MeshExporterNode)));
    }


    private void CreateNode(Type nodeType)
    {
        if (_graph == null)
        {
            Debug.LogError("NodeGraph is NULL! Reinitializing...");
            _graph = ScriptableObject.CreateInstance<NodeGraph>();
        }

        GraphNode newNode = ScriptableObject.CreateInstance(nodeType) as GraphNode;
        if (newNode == null)
        {
            Debug.LogError($"Failed to create node of type {nodeType.Name}. Ensure it inherits from GraphNode.");
            return;
        }

        _graph.AddNode(newNode);
        NodeView nodeView = new NodeView(newNode);
        RegisterNode(nodeView);

        Vector2 mousePosition = Event.current != null
            ? this.LocalToWorld(this.contentViewContainer.WorldToLocal(Event.current.mousePosition))
            : Vector2.zero;

        nodeView.SetPosition(new Rect(mousePosition, new Vector2(150, 100)));
        Debug.Log($"Created Node: {newNode.GetType().Name} at {mousePosition}");
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter adapter)
    {
        return ports.ToList().Where(port =>
            port.direction != startPort.direction &&
            port.portType == startPort.portType &&
            port.node != startPort.node
        ).ToList();
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        if (change.elementsToRemove != null)
        {
            foreach (GraphElement element in change.elementsToRemove)
            {
                if (element is NodeView nodeView)
                {
                    GraphNode node = nodeView.graphNode;
                    if (_graph.nodes.Contains(node))
                    {
                        _graph.RemoveNode(node);
                        Debug.Log($"🗑 Removed node {node.name} from graph.");
                    }
                }
            }
        }

        if (change.edgesToCreate != null)
        {
            foreach (Edge edge in change.edgesToCreate)
            {
                if (edge.input.node is NodeView inputNodeView && edge.output.node is NodeView outputNodeView)
                {
                    GraphNode inputNode = inputNodeView.graphNode;
                    GraphNode outputNode = outputNodeView.graphNode;

                    // Ensure connections are tracked in correct ports
                    inputNode.AddInputConnection(outputNode);
                    outputNode.AddOutputConnection(inputNode);

                    Debug.Log($"🔗 Created connection: {outputNode.name} → {inputNode.name}");

                    RefreshGraphEdges();
                }
            }
        }

        return change;
    }

    // Ensure edges stay visually synced when moving nodes
    private void RefreshGraphEdges()
    {
        foreach (Edge edge in edges.ToList())
        {
            RemoveElement(edge);
        }

        foreach (var nodeView in nodes.OfType<NodeView>())
        {
            for (int i = 0; i < nodeView.graphNode.outputConnections.Count; i++)
            {
                GraphNode outputNode = nodeView.graphNode.outputConnections[i];
                NodeView outputNodeView = nodes.OfType<NodeView>().FirstOrDefault(n => n.graphNode == outputNode);
                if (outputNodeView != null)
                {
                    Port outputPort = nodeView.outputPorts[i];
                    Port inputPort = outputNodeView.inputPorts.FirstOrDefault(); // Get first available input

                    if (outputPort != null && inputPort != null)
                    {
                        Edge newEdge = outputPort.ConnectTo(inputPort);
                        AddElement(newEdge);
                    }
                }
            }
        }

        Debug.Log("♻️ Refreshed all edges visually.");
    }
}
