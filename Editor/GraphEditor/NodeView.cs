using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.ProBuilder;
using UnityEngine.UIElements;
using UnityEngine;
using System;

public class NodeView : Node
{
    public GraphNode graphNode { get; private set; }
    public List<Port> inputPorts = new List<Port>();  // 🔥 Change from private → public
    public List<Port> outputPorts = new List<Port>(); // 🔥 Change from private → public


    public event Action<NodeView> NodeSelected;

    public NodeView(GraphNode node)
    {
        if (node == null)
        {
            Debug.LogError("NodeView received a NULL node!");
            return;
        }

        this.graphNode = node;
        this.title = node.GetType().Name;
        style.width = 150;

        capabilities |= Capabilities.Movable | Capabilities.Selectable;

        // Allow multiple input ports
        RefreshInputPorts();
        RefreshOutputPorts();

        RefreshExpandedState();
        RefreshPorts();

        RegisterCallback<MouseDownEvent>(evt =>
        {
            evt.StopPropagation();
            NodeSelected?.Invoke(this);
        });

        this.AddManipulator(new Dragger());
    }

    public void RefreshInputPorts()
    {
        inputContainer.Clear();
        inputPorts.Clear();

        for (int i = 0; i < graphNode.inputConnections.Count; i++) // Sync with existing connections
        {
            Port inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(ProBuilderMesh));
            inputPort.portName = $"Input {i + 1}";
            inputPorts.Add(inputPort);
            inputContainer.Add(inputPort);
        }

        // 🔥 Always have an extra empty port for new connections
        Port extraInputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(ProBuilderMesh));
        extraInputPort.portName = $"Input {inputPorts.Count + 1}";
        inputPorts.Add(extraInputPort);
        inputContainer.Add(extraInputPort);
    }

    public void RefreshOutputPorts()
    {
        outputContainer.Clear();
        outputPorts.Clear();

        for (int i = 0; i < graphNode.outputConnections.Count; i++) // Sync with existing connections
        {
            Port outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(ProBuilderMesh));
            outputPort.portName = $"Output {i + 1}";
            outputPorts.Add(outputPort);
            outputContainer.Add(outputPort);
        }

        // 🔥 Always have an extra empty port for new connections
        Port extraOutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(ProBuilderMesh));
        extraOutputPort.portName = $"Output {outputPorts.Count + 1}";
        outputPorts.Add(extraOutputPort);
        outputContainer.Add(extraOutputPort);
    }
}
