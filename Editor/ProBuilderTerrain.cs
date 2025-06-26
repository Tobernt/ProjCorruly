using System.Linq;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine;

public class ProBuilderTerrain : EditorWindow
{
    private float heightMultiplier = 10f;
    private float noiseScale = 0.05f;
    private float edgeHeight = 0f;
    private int terrainResolution = 40;
    private float centerInfluence = 1.0f;
    private int edgeTransitionFaces = 3;
    public ProBuilderMesh pbMesh;
    private int seed = 1000;

    private Material baseMaterial;

    private NoiseType selectedNoise = NoiseType.Perlin;

    private enum NoiseType
    {
        Perlin,
        Ridged,
        Billow,
        Simplex,
        Plateau,
        MultiFractal,
        Wavy,
        Step
    }

    [MenuItem("Tools/ProBuilder Terrain Generator")]
    public static void ShowWindow()
    {
        GetWindow<ProBuilderTerrain>("ProBuilder Terrain");
    }

    void OnGUI()
    {
        GUILayout.Label("ProBuilder Terrain Generator", EditorStyles.boldLabel);

        pbMesh = Selection.activeGameObject?.GetComponent<ProBuilderMesh>();

        if (pbMesh == null)
        {
            EditorGUILayout.HelpBox("Select a ProBuilder object to modify.", MessageType.Warning);
            return;
        }

        heightMultiplier = EditorGUILayout.Slider("Height Multiplier", heightMultiplier, 1f, 50f);
        noiseScale = EditorGUILayout.Slider("Noise Scale", noiseScale, 0.01f, 0.5f);
        edgeHeight = EditorGUILayout.Slider("Edge Height", edgeHeight, -10f, 50f);
        terrainResolution = EditorGUILayout.IntSlider("Terrain Resolution", terrainResolution, 10, 100);
        centerInfluence = EditorGUILayout.Slider("Center Influence", centerInfluence, 0.1f, 5f);
        seed = EditorGUILayout.IntField("Noise Seed", seed);
        edgeTransitionFaces = EditorGUILayout.IntSlider("Edge Transition Faces", edgeTransitionFaces, 1, 20); // New slider

        selectedNoise = (NoiseType)EditorGUILayout.EnumPopup("Noise Type", selectedNoise);

        baseMaterial = (Material)EditorGUILayout.ObjectField("Base Material", baseMaterial, typeof(Material), false);

        if (GUILayout.Button("Generate Terrain"))
        {
            GenerateTerrain();
        }
    }

    void GenerateTerrain()
    {
        if (pbMesh == null) return;

        Vector3[] vertices = pbMesh.positions.ToArray();
        Vector3 scale = pbMesh.transform.localScale;

        // Determine fixed face count independent of resolution
        int fixedFaceCount = 100; // Set a constant number of faces across terrain
        int gridSize = fixedFaceCount + 1; // Keeps vertex grid consistent
        Vector3[,] vertexGrid = new Vector3[gridSize, gridSize];

        float minX = vertices.Min(v => v.x);
        float maxX = vertices.Max(v => v.x);
        float minZ = vertices.Min(v => v.z);
        float maxZ = vertices.Max(v => v.z);
        float centerX = (minX + maxX) * 0.5f;
        float centerZ = (minZ + maxZ) * 0.5f;

        // Ensure face width is based on fixed face count
        float faceWidth = (maxX - minX) / (float)fixedFaceCount;
        float edgeSmoothingRange = faceWidth * edgeTransitionFaces * 10f;
        float centerSmoothingRange = faceWidth * centerInfluence * 8f;

        // Store vertices in a 2D array
        for (int i = 0; i < vertices.Length; i++)
        {
            int xIndex = Mathf.RoundToInt((vertices[i].x - minX) / faceWidth);
            int zIndex = Mathf.RoundToInt((vertices[i].z - minZ) / faceWidth);

            // Prevent index out of range
            xIndex = Mathf.Clamp(xIndex, 0, gridSize - 1);
            zIndex = Mathf.Clamp(zIndex, 0, gridSize - 1);

            vertexGrid[xIndex, zIndex] = vertices[i];
        }

        // Apply smoothing based on fixed face count
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                Vector3 worldPos = pbMesh.transform.TransformPoint(vertexGrid[x, z]);

                // Determine edge distance
                int edgeDistance = Mathf.Min(x, z, gridSize - 1 - x, gridSize - 1 - z);
                float edgeFactor = Mathf.Clamp01((float)edgeDistance / (edgeTransitionFaces * 2.0f));

                // Calculate noise-based height
                float rawNoise = GetNoise(worldPos.x * noiseScale, worldPos.z * noiseScale);
                float height = rawNoise * heightMultiplier;

                // Smooth transition from edge height to terrain height
                float smoothedHeight = Mathf.Lerp(edgeHeight, height, Mathf.Pow(edgeFactor, 2.0f));

                // Compute center influence for natural blending
                float distToCenter = Vector2.Distance(new Vector2(worldPos.x, worldPos.z), new Vector2(centerX, centerZ));
                float centerBlendFactor = Mathf.SmoothStep(0.3f, 1f, distToCenter / centerSmoothingRange);

                // Apply final height adjustment
                vertexGrid[x, z].y = Mathf.Lerp(edgeHeight, smoothedHeight, centerBlendFactor);
            }
        }

        // Apply modified heights back to the vertex array
        for (int i = 0; i < vertices.Length; i++)
        {
            int xIndex = Mathf.RoundToInt((vertices[i].x - minX) / faceWidth);
            int zIndex = Mathf.RoundToInt((vertices[i].z - minZ) / faceWidth);

            // Prevent index out of range
            xIndex = Mathf.Clamp(xIndex, 0, gridSize - 1);
            zIndex = Mathf.Clamp(zIndex, 0, gridSize - 1);

            vertices[i] = vertexGrid[xIndex, zIndex];
        }

        pbMesh.positions = vertices;
        pbMesh.ToMesh();
        pbMesh.Refresh(RefreshMask.Normals);
        pbMesh.Refresh(RefreshMask.UV);
        pbMesh.Refresh(RefreshMask.Tangents);
        pbMesh.Refresh(RefreshMask.Collisions);
        ApplyMaterials();
    }

    float GetNoise(float x, float z)
    {
        float scaledSeed = seed * 0.01f; // Scale seed to avoid overly large offsets
        float scale = 3.0f; // Adjust scale for better coverage

        switch (selectedNoise)
        {
            case NoiseType.Perlin:
                return Mathf.PerlinNoise((x + scaledSeed) * noiseScale * scale, (z + scaledSeed) * noiseScale * scale);

            case NoiseType.Ridged:
                return 1f - Mathf.Abs(Mathf.PerlinNoise((x + scaledSeed) * noiseScale * scale, (z + scaledSeed) * noiseScale * scale) * 2f - 1f);

            case NoiseType.Billow:
                return Mathf.Abs(Mathf.PerlinNoise((x + scaledSeed) * noiseScale * scale, (z + scaledSeed) * noiseScale * scale) * 2f - 1f);

            case NoiseType.Simplex:
                return Mathf.PerlinNoise((x + scaledSeed) * noiseScale * scale * 1.2f, (z + scaledSeed) * noiseScale * scale * 1.2f) * 0.8f +
                       Mathf.PerlinNoise((x + scaledSeed) * noiseScale * scale * 0.8f, (z + scaledSeed) * noiseScale * scale * 0.8f) * 0.2f;

            case NoiseType.Plateau:
                float perlin = Mathf.PerlinNoise((x + scaledSeed) * noiseScale * scale, (z + scaledSeed) * noiseScale * scale);
                return Mathf.Clamp01(perlin * perlin * perlin * 4f);

            case NoiseType.MultiFractal:
                return Mathf.PerlinNoise((x + scaledSeed) * noiseScale * scale * 0.5f, (z + scaledSeed) * noiseScale * scale * 0.5f) * 0.5f +
                       Mathf.PerlinNoise((x + scaledSeed) * noiseScale * scale * 1.0f, (z + scaledSeed) * noiseScale * scale * 1.0f) * 0.3f +
                       Mathf.PerlinNoise((x + scaledSeed) * noiseScale * scale * 2.0f, (z + scaledSeed) * noiseScale * scale * 2.0f) * 0.2f;

            case NoiseType.Wavy:
                return (Mathf.Sin((x + scaledSeed) * 3f) + Mathf.Sin((z + scaledSeed) * 3f)) * 0.5f + 0.5f;

            case NoiseType.Step:
                return Mathf.Round(Mathf.PerlinNoise((x + scaledSeed) * noiseScale * scale, (z + scaledSeed) * noiseScale * scale) * 3f) / 3f;

            default:
                return Mathf.PerlinNoise((x + scaledSeed) * noiseScale * scale, (z + scaledSeed) * noiseScale * scale);
        }
    }

    float ComputeHeight(float x, float z)
    {
        float baseHeight = Mathf.PerlinNoise(x * noiseScale + seed, z * noiseScale + seed) * heightMultiplier;
        float ridgeNoise = 1f - Mathf.Abs(Mathf.PerlinNoise(x * 0.02f, z * 0.02f) * 2f - 1f);
        float valleyNoise = Mathf.PerlinNoise(x * 0.015f, z * 0.015f) * 0.5f;

        float blendedHeight = baseHeight * 0.6f + ridgeNoise * 8f - valleyNoise * 5f;
        return Mathf.Clamp(blendedHeight, edgeHeight, heightMultiplier);
    }

    void ApplyMaterials()
    {
        if (pbMesh == null) return;

        MeshRenderer meshRenderer = pbMesh.gameObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = pbMesh.gameObject.AddComponent<MeshRenderer>();

        if (meshRenderer.sharedMaterials.Length < 1)
        {
            Material[] newMaterials = new Material[1];
            newMaterials[0] = baseMaterial;
            meshRenderer.sharedMaterials = newMaterials;
        }
        else
        {
            Material[] materials = meshRenderer.sharedMaterials;
            materials[0] = baseMaterial;
            meshRenderer.sharedMaterials = materials;
        }
    }
}