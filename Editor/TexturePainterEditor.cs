using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TexturePainterEditor : EditorWindow
{
    private MeshFilter targetMeshFilter;
    private Material targetMaterial;

    private Texture2D texture1, texture2, texture3, texture4;
    private int selectedTextureChannel = 0;
    private float brushSize = 0.5f;
    private float brushStrength = 1f;
    private float innerRadius = 0.2f;
    private float outerRingSize = 0.3f;
    private bool paintingEnabled = false;
    private float perlinScale = 5f; // Scale for Perlin noise
    private enum BrushType { Circle, PerlinNoise }
    private BrushType selectedBrush = BrushType.Circle;
    private static readonly string[] TextureProperties = { "_FirstTex", "_SecondTex", "_ThirdTex", "_FourthTex" };

    [MenuItem("Tools/Texture Painter")]
    public static void ShowWindow()
    {
        GetWindow<TexturePainterEditor>("Texture Painter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Texture Painter", EditorStyles.boldLabel);
        targetMeshFilter = EditorGUILayout.ObjectField("Target Mesh", targetMeshFilter, typeof(MeshFilter), true) as MeshFilter;

        if (targetMeshFilter)
        {
            targetMaterial = targetMeshFilter.GetComponent<Renderer>().sharedMaterial;
        }

        GUILayout.Label("Texture Selection (Auto-Assigned to Shader)");

        texture1 = EditorGUILayout.ObjectField("Texture 1 (Red)", texture1, typeof(Texture2D), false) as Texture2D;
        texture2 = EditorGUILayout.ObjectField("Texture 2 (Green)", texture2, typeof(Texture2D), false) as Texture2D;
        texture3 = EditorGUILayout.ObjectField("Texture 3 (Blue)", texture3, typeof(Texture2D), false) as Texture2D;
        texture4 = EditorGUILayout.ObjectField("Texture 4 (Alpha)", texture4, typeof(Texture2D), false) as Texture2D;

        selectedTextureChannel = EditorGUILayout.IntPopup("Brush Texture", selectedTextureChannel, new string[] { "Texture 1", "Texture 2", "Texture 3", "Texture 4" }, new int[] { 0, 1, 2, 3 });

        selectedBrush = (BrushType)EditorGUILayout.EnumPopup("Brush Type", selectedBrush);

        brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 0.01f, 100f);
        brushStrength = EditorGUILayout.Slider("Brush Strength", brushStrength, 0.01f, 1f);
        innerRadius = EditorGUILayout.Slider("Inner Radius", innerRadius, 0f, brushSize);
        outerRingSize = EditorGUILayout.Slider("Outer Ring Size", outerRingSize, 0f, brushSize);

        if (selectedBrush == BrushType.PerlinNoise)
        {
            perlinScale = EditorGUILayout.Slider("Perlin Noise Scale", perlinScale, 1f, 20f);
        }

        paintingEnabled = EditorGUILayout.Toggle("Enable Painting", paintingEnabled);

        if (GUILayout.Button("Apply Textures to Material"))
        {
            ApplyTexturesToMaterial();
        }

        if (GUILayout.Button("Fill Entire Mesh"))
        {
            FillTexture();
        }
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (targetMeshFilter == null || !paintingEnabled) return;

        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject == targetMeshFilter.gameObject)
            {
                Handles.color = Color.yellow;
                Handles.DrawWireDisc(hit.point, hit.normal, brushSize);

                if (selectedBrush == BrushType.Circle)
                {
                    // **Draw only for Circle brush**
                    Handles.color = Color.green;
                    Handles.DrawWireDisc(hit.point, hit.normal, innerRadius);
                    Handles.color = Color.red;
                    Handles.DrawWireDisc(hit.point, hit.normal, innerRadius + outerRingSize);
                }
                else if (selectedBrush == BrushType.PerlinNoise)
                {
                    // **Visualize Perlin Brush instead of circles**
                    DrawPerlinBrush(hit.point, hit.normal);
                }

                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
                {
                    PaintMesh(hit.point);
                    e.Use();
                }
            }
        }

        if (e.type == EventType.Repaint)
        {
            sceneView.Repaint();
        }
    }

    private void DrawPerlinBrush(Vector3 center, Vector3 normal)
    {
        int samplePoints = 16; // Number of points to visualize
        float angleStep = 360f / samplePoints;

        for (int i = 0; i < samplePoints; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float xOffset = Mathf.Cos(angle) * brushSize;
            float zOffset = Mathf.Sin(angle) * brushSize;
            Vector3 samplePoint = center + new Vector3(xOffset, 0, zOffset);

            float noiseValue = Mathf.PerlinNoise(
            (samplePoint.x + center.x) * perlinScale * 0.1f,
            (samplePoint.z + center.z) * perlinScale * 0.1f
        );

            Handles.color = new Color(1, 1, 1, noiseValue);
            Handles.DrawSolidDisc(samplePoint, normal, brushSize * 0.05f);
        }
    }

    private void PaintMesh(Vector3 hitPoint)
    {
        if (targetMeshFilter == null) return;

        Mesh mesh = targetMeshFilter.sharedMesh;
        if (mesh == null) return;

        Vector3[] vertices = mesh.vertices;
        Color[] colors = mesh.colors.Length == vertices.Length ? mesh.colors : new Color[vertices.Length];

        bool modified = false;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPos = targetMeshFilter.transform.TransformPoint(vertices[i]);
            float distanceSqr = (worldPos - hitPoint).sqrMagnitude;
            float finalStrength = 0f;

            if (selectedBrush == BrushType.PerlinNoise)
            {
                float distance = Vector3.Distance(worldPos, hitPoint);

                if (distance < brushSize) // **Only apply within brush area**
                {
                    float noise = Mathf.PerlinNoise(
                        (worldPos.x + hitPoint.x) * perlinScale * 0.1f,
                        (worldPos.z + hitPoint.z) * perlinScale * 0.1f
                    );

                    float falloff = Mathf.Clamp01(1 - (distance / brushSize)); // **Fade near edges**
                    finalStrength = noise * brushStrength * falloff;
                    modified = true;
                }
            }

            else if (distanceSqr < (innerRadius + outerRingSize) * (innerRadius + outerRingSize))
            {
                modified = true;
                float distance = Mathf.Sqrt(distanceSqr);

                // **Standard Brush Blending**
                float blendFactor = (distance < innerRadius) ? 1f : Mathf.Clamp01(1 - ((distance - innerRadius) / outerRingSize));
                finalStrength = blendFactor * brushStrength;
            }

            if (finalStrength > 0f)
            {
                float remainingStrength = 1f - finalStrength;
                colors[i] = new Color(
                    colors[i].r * remainingStrength,
                    colors[i].g * remainingStrength,
                    colors[i].b * remainingStrength,
                    colors[i].a * remainingStrength
                );

                // **Apply selected texture channel**
                switch (selectedTextureChannel)
                {
                    case 0: colors[i].r = Mathf.Clamp01(colors[i].r + finalStrength); break;
                    case 1: colors[i].g = Mathf.Clamp01(colors[i].g + finalStrength); break;
                    case 2: colors[i].b = Mathf.Clamp01(colors[i].b + finalStrength); break;
                    case 3: colors[i].a = Mathf.Clamp01(colors[i].a + finalStrength); break;
                }
            }
        }

        if (modified)
        {
            mesh.colors = colors;
            targetMeshFilter.sharedMesh = mesh;
            EditorUtility.SetDirty(mesh);
        }
    }

    private void FillTexture()
    {
        if (targetMeshFilter == null) return;

        Mesh mesh = targetMeshFilter.sharedMesh;
        if (mesh == null) return;

        Color[] colors = new Color[mesh.vertexCount];

        for (int i = 0; i < colors.Length; i++)
        {
            switch (selectedTextureChannel)
            {
                case 0: colors[i] = new Color(1, 0, 0, 0); break;
                case 1: colors[i] = new Color(0, 1, 0, 0); break;
                case 2: colors[i] = new Color(0, 0, 1, 0); break;
                case 3: colors[i] = new Color(0, 0, 0, 1); break;
            }
        }

        mesh.colors = colors;
        targetMeshFilter.sharedMesh = mesh;
        EditorUtility.SetDirty(mesh);
    }

    private void ApplyTexturesToMaterial()
    {
        if (targetMaterial == null)
        {
            Debug.LogWarning("No material found on selected mesh.");
            return;
        }


        targetMaterial.SetTexture(TextureProperties[0], texture1);
        targetMaterial.SetTexture(TextureProperties[1], texture2);
        targetMaterial.SetTexture(TextureProperties[2], texture3);
        targetMaterial.SetTexture(TextureProperties[3], texture4);

        EditorUtility.SetDirty(targetMaterial);
    }
}
