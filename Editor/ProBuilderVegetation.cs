using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using System.Collections.Generic;
using System.Linq;

public class ProBuilderVegetation : EditorWindow
{
    private ProBuilderMesh pbMesh;

    // Vegetation settings
    private int grassAmount = 50;
    private Vector2 grassScaleRange = new Vector2(0.8f, 1.2f);
    private Vector2 grassYScaleRange = new Vector2(0.5f, 1.5f); // Added Y-scale range for grass
    private List<GameObject> grassPrefabs = new List<GameObject>();

    private int treeAmount = 10;
    private Vector2 treeScaleRange = new Vector2(1.0f, 3.0f);
    private List<GameObject> treePrefabs = new List<GameObject>();

    private int foliageAmount = 20;
    private Vector2 foliageScaleRange = new Vector2(0.8f, 2.0f);
    private List<GameObject> foliagePrefabs = new List<GameObject>();

    // Heightmap settings
    private Texture2D heightmap;
    private float heightMultiplier = 10f;

    // Altitude settings
    private Vector2 altitudeRange = new Vector2(0f, 100f); // Added altitude filtering

    // UI Scrolling
    private Vector2 scrollPosition;

    [MenuItem("Tools/ProBuilder Vegetation")]
    public static void ShowWindow()
    {
        GetWindow<ProBuilderVegetation>("Vegetation");
    }
    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Width(position.width), GUILayout.Height(position.height));

        GUILayout.Label("Vegetation Generator", EditorStyles.boldLabel);
        pbMesh = Selection.activeGameObject?.GetComponent<ProBuilderMesh>();

        if (pbMesh == null)
        {
            EditorGUILayout.HelpBox("Select a ProBuilder object to modify.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        GUILayout.Space(10);
        DrawGrassSettings(); // Separate method for grass (since it has an additional Y-scale range)
        DrawVegetationSettings("Trees", ref treeAmount, ref treeScaleRange, ref treePrefabs);
        DrawVegetationSettings("Foliage", ref foliageAmount, ref foliageScaleRange, ref foliagePrefabs);

        GUILayout.Space(10);
        GUILayout.Label("Altitude Range", EditorStyles.boldLabel);
        altitudeRange = EditorGUILayout.Vector2Field("Min / Max Altitude", altitudeRange);

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Vegetation"))
        {
            GenerateVegetation();
        }

        GUILayout.Space(20);
        GUILayout.Label("Heightmap Settings", EditorStyles.boldLabel);
        heightmap = (Texture2D)EditorGUILayout.ObjectField("Heightmap", heightmap, typeof(Texture2D), false);
        heightMultiplier = EditorGUILayout.Slider("Height Multiplier", heightMultiplier, 1f, 50f);

        if (GUILayout.Button("Apply Heightmap"))
        {
            ApplyHeightMap(heightmap);
        }

        EditorGUILayout.EndScrollView();
    }
    void DrawGrassSettings()
    {
        GUILayout.Label("Grass Settings", EditorStyles.boldLabel);
        grassAmount = EditorGUILayout.IntField("Grass Amount", grassAmount);
        grassScaleRange = EditorGUILayout.Vector2Field("Grass Scale Range", grassScaleRange);
        grassYScaleRange = EditorGUILayout.Vector2Field("Grass Y Scale Range", grassYScaleRange); // Added Y-scale input

        EditorGUILayout.LabelField("Grass Prefabs:");
        if (GUILayout.Button("Add Grass Prefab Slot"))
        {
            grassPrefabs.Add(null);
        }

        for (int i = 0; i < grassPrefabs.Count; i++)
        {
            grassPrefabs[i] = (GameObject)EditorGUILayout.ObjectField($"Grass Prefab {i + 1}", grassPrefabs[i], typeof(GameObject), false);
            if (GUILayout.Button($"Remove Grass Prefab {i + 1}"))
            {
                grassPrefabs.RemoveAt(i);
                break;
            }
        }
        GUILayout.Space(10);
    }
    void DrawVegetationSettings(string label, ref int amount, ref Vector2 scaleRange, ref List<GameObject> prefabs)
    {
        GUILayout.Label($"{label} Settings", EditorStyles.boldLabel);
        amount = EditorGUILayout.IntField($"{label} Amount", amount);
        scaleRange = EditorGUILayout.Vector2Field($"{label} Scale Range", scaleRange);

        EditorGUILayout.LabelField($"{label} Prefabs:");
        if (GUILayout.Button($"Add {label} Prefab Slot"))
        {
            prefabs.Add(null);
        }

        for (int i = 0; i < prefabs.Count; i++)
        {
            prefabs[i] = (GameObject)EditorGUILayout.ObjectField($"{label} Prefab {i + 1}", prefabs[i], typeof(GameObject), false);
            if (GUILayout.Button($"Remove {label} Prefab {i + 1}"))
            {
                prefabs.RemoveAt(i);
                break;
            }
        }
        GUILayout.Space(10);
    }

    void GenerateVegetation()
    {
        if (pbMesh == null) return;

        Transform parent = GameObject.Find("VegetationParent")?.transform;
        if (parent == null)
        {
            parent = new GameObject("VegetationParent").transform;
            parent.position = Vector3.zero;
        }

        foreach (Transform child in parent) DestroyImmediate(child.gameObject);

        Vector3[] vertices = pbMesh.positions.ToArray();
        Vector3 scale = pbMesh.transform.localScale;

        float minX = vertices.Min(v => v.x) * scale.x + pbMesh.transform.position.x;
        float maxX = vertices.Max(v => v.x) * scale.x + pbMesh.transform.position.x;
        float minZ = vertices.Min(v => v.z) * scale.z + pbMesh.transform.position.z;
        float maxZ = vertices.Max(v => v.z) * scale.z + pbMesh.transform.position.z;

        SpawnVegetation(grassAmount, grassScaleRange, grassYScaleRange, grassPrefabs, parent, minX, maxX, minZ, maxZ, true);
        SpawnVegetation(treeAmount, treeScaleRange, null, treePrefabs, parent, minX, maxX, minZ, maxZ, true);
        SpawnVegetation(foliageAmount, foliageScaleRange, null, foliagePrefabs, parent, minX, maxX, minZ, maxZ, true);
    }

    void SpawnVegetation(int amount, Vector2 scaleRange, Vector2? yScaleRange, List<GameObject> prefabs, Transform parent, float minX, float maxX, float minZ, float maxZ, bool isGrass)
    {
        if (prefabs.Count == 0) return;

        for (int i = 0; i < amount; i++)
        {
            float x = Random.Range(minX, maxX);
            float z = Random.Range(minZ, maxZ);
            Vector3 spawnPos = new Vector3(x, 100, z);
            Vector3 adjustedPos = GetTerrainPosition(spawnPos);

            if (adjustedPos.y < altitudeRange.x || adjustedPos.y > altitudeRange.y)
                continue; // Skip positions outside the altitude range

            Quaternion alignedRotation = GetTerrainNormalRotation(adjustedPos, isGrass);

            float randomScale = Random.Range(scaleRange.x, scaleRange.y);

            float randomScaleX, randomScaleY, randomScaleZ;

            if (isGrass && yScaleRange.HasValue)
            {
                // Grass gets separate Y scaling
                randomScaleX = Random.Range(scaleRange.x, scaleRange.y);
                randomScaleZ = Random.Range(scaleRange.x, scaleRange.y);
                randomScaleY = Random.Range(yScaleRange.Value.x, yScaleRange.Value.y);
            }
            else
            {
                // Trees and bushes get uniform scaling
                randomScaleX = randomScaleY = randomScaleZ = randomScale;
            }

            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            GameObject instance = Instantiate(prefab, adjustedPos, alignedRotation);
            instance.transform.localScale = new Vector3(randomScaleX, randomScaleY, randomScaleZ);
            instance.transform.SetParent(parent, true);
        }
    }

    void ApplyHeightMap(Texture2D heightmap)
    {
        if (pbMesh == null || heightmap == null) return;

        Vector3[] vertices = pbMesh.positions.ToArray();
        Vector3 scale = pbMesh.transform.localScale;

        float minX = vertices.Min(v => v.x) * scale.x;
        float maxX = vertices.Max(v => v.x) * scale.x;
        float minZ = vertices.Min(v => v.z) * scale.z;
        float maxZ = vertices.Max(v => v.z) * scale.z;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPos = pbMesh.transform.TransformPoint(vertices[i]);

            // Normalize UV coords for heightmap lookup
            float u = Mathf.InverseLerp(minX, maxX, worldPos.x);
            float v = Mathf.InverseLerp(minZ, maxZ, worldPos.z);

            Color pixel = heightmap.GetPixelBilinear(u, v);
            float heightOffset = (1f - pixel.r) * heightMultiplier;

            vertices[i].y -= heightOffset / scale.y; // Darkest parts push terrain down
        }

        pbMesh.positions = vertices;
        pbMesh.ToMesh();
        pbMesh.Refresh(RefreshMask.Normals);
        pbMesh.Refresh(RefreshMask.UV);
        pbMesh.Refresh(RefreshMask.Tangents);
        pbMesh.Refresh(RefreshMask.Collisions);
    }

    Vector3 GetTerrainPosition(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(position.x, 100, position.z), Vector3.down, out hit, Mathf.Infinity))
        {
            return hit.point;
        }
        return position;
    }

    Quaternion GetTerrainNormalRotation(Vector3 position, bool isGrass)
    {
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(position.x, 100, position.z), Vector3.down, out hit, Mathf.Infinity))
        {
            Vector3 normal = hit.normal;
            if (!isGrass)
            {
                normal = Vector3.Lerp(Vector3.up, normal, 0.3f);
            }
            return Quaternion.FromToRotation(Vector3.up, normal);
        }
        return Quaternion.identity;
    }
}
