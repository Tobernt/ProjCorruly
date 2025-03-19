using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class InspectorPanel : VisualElement
{
    private SerializedObject _serializedObject;
    private VisualElement _inspectorContainer;
    private GraphProcessor _graphProcessor;
    private VisualElement _resizeHandle;
    private float _panelWidth = 300f; // ✅ Default width (Resizable)

    public InspectorPanel(GraphProcessor graphProcessor)
    {
        _graphProcessor = graphProcessor;

        // ✅ Initial panel width & styling
        style.width = _panelWidth;
        style.minWidth = 200; // ✅ Prevents collapsing
        style.maxWidth = 600; // ✅ Prevents too wide
        style.flexShrink = 0;
        style.borderLeftWidth = 2; // ✅ Visual border for separation
        style.borderLeftColor = new Color(0.2f, 0.2f, 0.2f, 1); // ✅ Dark grey border
        style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1); // ✅ Dark UI panel color

        // ✅ Title
        Label title = new Label("Inspector Panel")
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = 14,
                paddingLeft = 10
            }
        };

        // ✅ Scrollable area for properties
        ScrollView scrollView = new ScrollView(ScrollViewMode.Vertical)
        {
            style =
            {
                flexGrow = 1,
                paddingLeft = 10,
                paddingRight = 10,
                maxHeight = new StyleLength(new Length(100, LengthUnit.Percent))
            }
        };

        _inspectorContainer = new VisualElement();
        scrollView.Add(_inspectorContainer);

        // ✅ Add UI Elements
        Add(title);
        Add(scrollView);

        // ✅ Create draggable resize handle
        _resizeHandle = new VisualElement()
        {
            style =
            {
                width = 5,
                height = new StyleLength(new Length(100, LengthUnit.Percent)),
                backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1),
                position = Position.Absolute,
                left = -5 // Ensures correct dragging placement
            }
        };

        _resizeHandle.RegisterCallback<MouseDownEvent>(OnMouseDown);
        _resizeHandle.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        _resizeHandle.RegisterCallback<MouseUpEvent>(OnMouseUp);

        Add(_resizeHandle);
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
            _panelWidth -= evt.mouseDelta.x;
            _panelWidth = Mathf.Clamp(_panelWidth, 200, 600); // ✅ Keep within min/max range
            style.width = _panelWidth;
        }
    }

    private void OnMouseUp(MouseUpEvent evt)
    {
        _isResizing = false;
    }

    public void UpdateInspector(GraphNode selectedNode)
    {
        _inspectorContainer.Clear();

        if (selectedNode == null)
        {
            _inspectorContainer.Add(new Label("No node selected"));
            return;
        }

        Debug.Log($"Updating Inspector for: {selectedNode.name}");

        _serializedObject = new SerializedObject(selectedNode);
        _serializedObject.Update();

        SerializedProperty iterator = _serializedObject.GetIterator();
        if (iterator.NextVisible(true))
        {
            do
            {
                PropertyField propertyField = new PropertyField(iterator);
                propertyField.Bind(_serializedObject);
                propertyField.RegisterValueChangeCallback(evt =>
                {
                    _serializedObject.ApplyModifiedProperties();
                });

                _inspectorContainer.Add(propertyField);
            }
            while (iterator.NextVisible(false));
        }
    }
}
