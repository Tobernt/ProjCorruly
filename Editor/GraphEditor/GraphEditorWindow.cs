using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.UIElements;

public class GraphEditorWindow : EditorWindow
{
    private NodeGraphView _graphView;
    private InspectorPanel _inspectorPanel;
    private GraphProcessor _graphProcessor;
    private VisualElement _mainContainer;
    private VisualElement _graphContainer;
    private VisualElement _rightPanel;
    private VisualElement _resizeHandle;
    private float _inspectorWidth = 300f; // Default Width for Inspector

    [MenuItem("Window/Procedural Mesh Graph")]
    public static void OpenWindow()
    {
        var window = GetWindow<GraphEditorWindow>();
        window.titleContent = new GUIContent("Procedural Mesh Graph");
        window.Show();
    }

    private void CreateGUI()
    {
        if (_graphProcessor == null)
        {
            NodeGraph existingGraph = ScriptableObject.CreateInstance<NodeGraph>();
            _graphProcessor = new GraphProcessor(existingGraph);
        }

        rootVisualElement.Clear();

        CreateToolbar();
        CreateMainLayout();
    }

    private void CreateToolbar()
    {
        var toolbar = new Toolbar();

        var generateButton = new Button(() =>
        {
            if (_graphProcessor != null)
            {
                Debug.Log("🔄 Manually Executing Graph");
                _graphProcessor.ExecuteGraph();
            }
        })
        { text = "Generate" };

        toolbar.Add(generateButton);
        rootVisualElement.Add(toolbar);
    }

    private void CreateMainLayout()
    {
        _mainContainer = new VisualElement
        {
            style =
            {
                flexGrow = 1,
                flexDirection = FlexDirection.Row // Side-by-side layout
            }
        };

        _graphContainer = new VisualElement
        {
            style =
            {
                flexGrow = 1,
                backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1) // Dark Background for Graph
            }
        };

        _rightPanel = new VisualElement
        {
            style =
            {
                width = _inspectorWidth,
                minWidth = 200,
                maxWidth = 600,
                flexShrink = 0,
                backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1),
                borderLeftWidth = 2,
                borderLeftColor = new Color(0.2f, 0.2f, 0.2f, 1)
            }
        };

        // Resizable Handle
        _resizeHandle = new VisualElement()
        {
            style =
            {
                width = 5,
                height = new StyleLength(new Length(100, LengthUnit.Percent)),
                backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1),
                position = Position.Absolute,
                left = -5
            }
        };

        _resizeHandle.RegisterCallback<MouseDownEvent>(OnMouseDown);
        _resizeHandle.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        _resizeHandle.RegisterCallback<MouseUpEvent>(OnMouseUp);

        _rightPanel.Add(_resizeHandle);
        CreateGraphView();
        CreateInspectorPanel();

        _mainContainer.Add(_graphContainer);
        _mainContainer.Add(_rightPanel);
        rootVisualElement.Add(_mainContainer);
    }

    private bool _isResizing = false;

    private void OnMouseDown(MouseDownEvent evt)
    {
        _isResizing = true;
        evt.StopPropagation();
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        if (_isResizing)
        {
            _inspectorWidth -= evt.mouseDelta.x;
            _inspectorWidth = Mathf.Clamp(_inspectorWidth, 200, 600);
            _rightPanel.style.width = _inspectorWidth;
        }
    }

    private void OnMouseUp(MouseUpEvent evt)
    {
        _isResizing = false;
    }

    private void CreateGraphView()
    {
        if (_graphProcessor == null || _graphProcessor.Graph == null)
        {
            Debug.LogWarning("GraphProcessor or its Graph is NULL! Creating a new graph.");
            _graphProcessor = new GraphProcessor(ScriptableObject.CreateInstance<NodeGraph>());
        }

        _graphView = new NodeGraphView(_graphProcessor.Graph)
        {
            style = { flexGrow = 1 }
        };

        _graphView.OnNodeSelected += node => _inspectorPanel.UpdateInspector(node);

        _graphContainer.Add(_graphView);
    }

    private void CreateInspectorPanel()
    {
        _inspectorPanel = new InspectorPanel(_graphProcessor)
        {
            style =
            {
                flexGrow = 1,
                overflow = Overflow.Visible // Make it scrollable
            }
        };

        _rightPanel.Add(_inspectorPanel);
    }
}
