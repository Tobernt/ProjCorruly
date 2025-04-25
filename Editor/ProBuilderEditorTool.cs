using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Collections.Generic;
using System.Linq;

public class ProBuilderHeightPainter : EditorWindow
{
    [System.Serializable]
    public class InstancedMeshVariant
    {
        public Mesh mesh;
        public Material material;
        public float weight = 1f;
    }

    [System.Serializable]
    public class WeightedTreePrefab
    {
        public GameObject prefab;
        public float weight = 1f;
    }
    private List<WeightedTreePrefab> treePrefabs = new List<WeightedTreePrefab>();
    private float grassSpacing = 1f;
    private float grassDensity = 5f;

    private ProBuilderMesh pbMesh;
    private float brushSize = 1f;
    private float brushStrength = 0.1f;
    private bool isPainting = false;
    private GameObject selectedPrefab;
    private float minPrefabSpacing = 1.0f;
    private float minPrefabScale = 0.8f;
    private float maxPrefabScale = 1.2f;
    private int maxPrefabsPerBrush = 5;
    private float minGrassScale = 0.8f;
    private float maxGrassScale = 1.2f;
    private bool alignToSurface = true;
    private Vector3 lastHitPoint;
    private bool validHit = false;
    private MeshCollider meshCollider;

    private List<InstancedMeshVariant> instancedGrassVariants = new List<InstancedMeshVariant>();
    private Dictionary<(UnityEngine.Mesh, UnityEngine.Material), List<Matrix4x4>> instanceBatches = new();

    private Transform prefabParent;
    private Transform treeParent;

    private enum PaintMode { Raise, Lower, SetHeight, SmoothToLevel, PaintPrefabs, PaintTrees, ErasePrefabs, PaintGrass}
    private PaintMode selectedMode = PaintMode.Raise;
    private float targetHeight = 0f;

    [MenuItem("Tools/ProBuilder Height Painter")]
    public static void ShowWindow()
    {
        GetWindow<ProBuilderHeightPainter>("Height Painter");
    }

    void OnGUI()
    {
        GUILayout.Label("ProBuilder Height Painter", EditorStyles.boldLabel);
        pbMesh = (ProBuilderMesh)EditorGUILayout.ObjectField("ProBuilder Mesh", pbMesh, typeof(ProBuilderMesh), true);

        if (pbMesh == null)
        {
            EditorGUILayout.HelpBox("Select a ProBuilder mesh to modify.", MessageType.Warning);
            return;
        }

        if (!pbMesh.GetComponent<MeshCollider>())
        {
            if (GUILayout.Button("Add Mesh Collider"))
            {
                meshCollider = pbMesh.gameObject.AddComponent<MeshCollider>();
                Debug.Log("✅ MeshCollider added to ProBuilder mesh.");
            }
        }

        brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 0.1f, 50f);
        brushStrength = EditorGUILayout.Slider("Brush Strength", brushStrength, 0.01f, 1f);
        selectedMode = (PaintMode)EditorGUILayout.EnumPopup("Paint Mode", selectedMode);

        if (selectedMode == PaintMode.PaintGrass)
        {
            if (GUILayout.Button("Bake Grass to GameObjects"))
            {
                BakeGrassInstances();
            }

            GUILayout.Label("Instanced Grass Meshes", EditorStyles.boldLabel);

            grassSpacing = EditorGUILayout.FloatField("Min Spacing", grassSpacing);
            grassDensity = EditorGUILayout.Slider("Density", grassDensity, 1f, 20f); // you can tweak this range
            minGrassScale = EditorGUILayout.FloatField("Min Scale", minGrassScale);
            maxGrassScale = EditorGUILayout.FloatField("Max Scale", maxGrassScale);


            for (int i = 0; i < instancedGrassVariants.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                instancedGrassVariants[i].mesh = (Mesh)EditorGUILayout.ObjectField(instancedGrassVariants[i].mesh, typeof(Mesh), false);
                instancedGrassVariants[i].material = (Material)EditorGUILayout.ObjectField(instancedGrassVariants[i].material, typeof(Material), false);
                instancedGrassVariants[i].weight = EditorGUILayout.FloatField(instancedGrassVariants[i].weight);
                if (GUILayout.Button("X", GUILayout.Width(20))) instancedGrassVariants.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Grass Variant"))
                instancedGrassVariants.Add(new InstancedMeshVariant());
        }


        if (selectedMode == PaintMode.SetHeight || selectedMode == PaintMode.SmoothToLevel)
            targetHeight = EditorGUILayout.FloatField("Target Height", targetHeight);
        else if (selectedMode == PaintMode.PaintPrefabs || selectedMode == PaintMode.PaintTrees || selectedMode == PaintMode.ErasePrefabs)
        {
            minPrefabSpacing = EditorGUILayout.FloatField("Min Prefab Spacing", minPrefabSpacing);
            minPrefabScale = EditorGUILayout.FloatField("Min Prefab Scale", minPrefabScale);
            maxPrefabScale = EditorGUILayout.FloatField("Max Prefab Scale", maxPrefabScale);
            alignToSurface = EditorGUILayout.Toggle("Align to Surface", alignToSurface);
            if (selectedMode == PaintMode.PaintTrees)
            {
                GUILayout.Label("Tree Prefabs (Weighted)", EditorStyles.boldLabel);
                for (int i = 0; i < treePrefabs.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    treePrefabs[i].prefab = (GameObject)EditorGUILayout.ObjectField(treePrefabs[i].prefab, typeof(GameObject), false);
                    treePrefabs[i].weight = EditorGUILayout.FloatField(treePrefabs[i].weight);
                    if (GUILayout.Button("X", GUILayout.Width(20))) treePrefabs.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Add Tree Prefab"))
                    treePrefabs.Add(new WeightedTreePrefab());
            }
            else
            {
                selectedPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", selectedPrefab, typeof(GameObject), false);
            }

        }

        isPainting = GUILayout.Toggle(isPainting, "Enable Painting");
    }
    void BakeGrassInstances()
    {
        GameObject root = new GameObject("BakedGrass");

        foreach (var kvp in instanceBatches)
        {
            var (mesh, material) = kvp.Key;
            var matrices = kvp.Value;

            for (int i = 0; i < matrices.Count; i++)
            {
                Matrix4x4 matrix = matrices[i];

                Vector3 position = matrix.GetColumn(3);
                Vector3 forward = matrix.GetColumn(2).normalized;
                Vector3 up = matrix.GetColumn(1).normalized;
                Quaternion rotation = Quaternion.LookRotation(forward, up);
                Vector3 scale = new Vector3(
                    matrix.GetColumn(0).magnitude,
                    matrix.GetColumn(1).magnitude,
                    matrix.GetColumn(2).magnitude
                );

                // 🔧 Auto-create GameObject with mesh & material
                GameObject grassGO = new GameObject($"Grass_{i}");
                grassGO.transform.SetParent(root.transform);
                grassGO.transform.SetPositionAndRotation(position, rotation);
                grassGO.transform.localScale = scale;

                MeshFilter mf = grassGO.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;

                MeshRenderer mr = grassGO.AddComponent<MeshRenderer>();
                mr.sharedMaterial = material;
            }
        }

        Debug.Log("✅ Baked all grass into scene as runtime-generated GameObjects.");
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (!isPainting || pbMesh == null) return;

        Event e = Event.current;
        Ray ray = UnityEditor.HandleUtility.GUIPointToWorldRay(e.mousePosition);

        foreach (var kvp in instanceBatches)
        {
            var (mesh, mat) = kvp.Key;
            List<Matrix4x4> matrices = kvp.Value;

            for (int i = 0; i < matrices.Count; i += 1023)
            {
                int batchCount = Mathf.Min(1023, matrices.Count - i);
                Graphics.DrawMeshInstanced(mesh, 0, mat, matrices.GetRange(i, batchCount));
            }
        }

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            lastHitPoint = hit.point;
            validHit = true;

            Handles.color = new Color(1, 0, 0, 0.5f);
            Handles.DrawSolidDisc(hit.point, Vector3.up, brushSize);

            if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag && e.button == 0)
            {
                ModifyMesh(hit);
                e.Use();
            }
        }
        else
        {
            validHit = false;
        }

        sceneView.Repaint();
    }
    void AddInstancedGrass(Vector3 center, Vector3 normal)
    {
        var variant = GetRandomGrassVariant();
        if (variant == null || variant.mesh == null || variant.material == null) return;

        var key = (variant.mesh, variant.material);
        if (!instanceBatches.ContainsKey(key))
            instanceBatches[key] = new List<Matrix4x4>();
        Debug.Log($"✔ Painted grass instance using: {variant.mesh.name}, {variant.material.name}");

        // 🔁 Try multiple random points in the brush per call (can tweak this number)
        int instancesToTry = Mathf.RoundToInt(grassDensity);
        for (int i = 0; i < instancesToTry; i++)
        {
            // Random offset within brush radius
            Vector2 offset = Random.insideUnitCircle * brushSize;
            Vector3 offsetPos = center + new Vector3(offset.x, 0, offset.y);

            // Raycast down to hit ground
            if (!Physics.Raycast(offsetPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit))
                continue;

            Vector3 finalPos = hit.point;

            // Rotation & scale
            Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            Quaternion randomY = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            Quaternion finalRotation = surfaceRotation * randomY;

            float scale = Random.Range(minGrassScale, maxGrassScale);

            Matrix4x4 matrix = Matrix4x4.TRS(finalPos, finalRotation, Vector3.one * scale);

            instanceBatches[key].Add(matrix);
        }
    }

    private InstancedMeshVariant GetRandomGrassVariant()
    {
        if (instancedGrassVariants == null || instancedGrassVariants.Count == 0)
            return null;

        float totalWeight = instancedGrassVariants.Sum(v => v.weight);
        float roll = Random.Range(0f, totalWeight);
        foreach (var variant in instancedGrassVariants)
        {
            if (roll < variant.weight)
                return variant;
            roll -= variant.weight;
        }
        return instancedGrassVariants.FirstOrDefault();
    }

    void ModifyMesh(RaycastHit hit)
    {
        Vector3[] vertices = pbMesh.positions.ToArray();
        bool modified = false;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPos = pbMesh.transform.TransformPoint(vertices[i]);
            float distance = Vector3.Distance(worldPos, hit.point);

            if (distance < brushSize)
            {
                float strength = Mathf.Lerp(brushStrength, 0, distance / brushSize);
                switch (selectedMode)
                {
                    case PaintMode.Raise:
                        vertices[i].y += strength;
                        modified = true;
                        break;
                    case PaintMode.Lower:
                        vertices[i].y -= strength;
                        modified = true;
                        break;
                    case PaintMode.SetHeight:
                        vertices[i].y = targetHeight;
                        modified = true;
                        break;
                    case PaintMode.SmoothToLevel:
                        vertices[i].y = Mathf.Lerp(vertices[i].y, targetHeight, 0.2f);
                        modified = true;
                        break;
                    case PaintMode.PaintPrefabs:
                        PlacePrefab(hit, false);
                        break;
                    case PaintMode.PaintGrass:
                        AddInstancedGrass(hit.point, hit.normal);
                        break;
                    case PaintMode.PaintTrees:
                        PlacePrefab(hit, true);
                        break;
                    case PaintMode.ErasePrefabs:
                        RemovePrefab(hit.point);
                        break;
                }
            }
        }

        if (modified)
        {
            pbMesh.positions = vertices;
            pbMesh.ToMesh();
            pbMesh.Refresh(RefreshMask.Normals);
            pbMesh.Refresh(RefreshMask.UV);
            pbMesh.Refresh(RefreshMask.Tangents);
            pbMesh.Refresh(RefreshMask.Collisions);
            UpdateMeshCollider();
        }
    }
    void AddGrassInstance(Vector3 position, Vector3 normal)
    {
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
        float scale = Random.Range(0.8f, 1.2f);
        Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, Vector3.one * scale);
    }
    void UpdateMeshCollider()
    {
        if (meshCollider == null) meshCollider = pbMesh.GetComponent<MeshCollider>();
        if (meshCollider != null) meshCollider.sharedMesh = pbMesh.gameObject.GetComponent<MeshFilter>().sharedMesh;
    }


    void PlacePrefab(RaycastHit hit, bool isTree)
    {
        if (isTree)
        {
            if (treePrefabs == null || treePrefabs.Count == 0) return;
        }
        else
        {
            if (selectedPrefab == null) return;
        }


        // **Create parent containers if they don't exist**
        if (isTree)
        {
            if (treeParent == null) treeParent = new GameObject("PaintedTrees").transform;
        }
        else
        {
            if (prefabParent == null) prefabParent = new GameObject("PaintedPrefabs").transform;
        }

        Transform parent = isTree ? treeParent : prefabParent;
        int prefabsPlaced = 0;

        // **Try to place multiple prefabs within the brush area**
        for (int i = 0; i < maxPrefabsPerBrush; i++)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-brushSize, brushSize),
                0,
                Random.Range(-brushSize, brushSize)
            );

            Vector3 spawnPosition = hit.point + randomOffset;

            // **Raycast downward to find correct ground position**
            if (Physics.Raycast(spawnPosition + Vector3.up * 10f, Vector3.down, out RaycastHit spawnHit))
            {

                if (Vector3.Distance(spawnHit.point, hit.point) > brushSize) continue; // Stay inside the brush area

                // **Check for spacing with correct prefab type**
                bool tooClose = false;
                foreach (Transform child in parent)
                {
                    if (Vector3.Distance(child.position, spawnHit.point) < minPrefabSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose) continue;

                // **Instantiate Prefab**
                GameObject prefabToUse = isTree ? GetRandomTreePrefab() : selectedPrefab;
                if (prefabToUse == null) continue;

                GameObject instance = Instantiate(prefabToUse, spawnHit.point, Quaternion.identity, parent);


                // **Random Scale**
                float randomScale = Random.Range(minPrefabScale, maxPrefabScale);
                instance.transform.localScale *= randomScale;

                // **Random Rotation**
                float randomRotationY = Random.Range(0f, 360f);
                Quaternion randomRotation = Quaternion.Euler(0, randomRotationY, 0);

                if (alignToSurface)
                {
                    Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, spawnHit.normal);

                    if (isTree)
                    {
                        // Trees stay more upright
                        instance.transform.rotation = Quaternion.Lerp(Quaternion.identity, surfaceRotation, 0.3f) * randomRotation;
                    }
                    else
                    {
                        // Prefabs fully align to the surface
                        instance.transform.rotation = surfaceRotation * randomRotation;
                    }
                }
                else
                {
                    // No alignment, just random Y rotation
                    instance.transform.rotation = randomRotation;
                }

                prefabsPlaced++;
                if (prefabsPlaced >= maxPrefabsPerBrush) break; // Stop placing if we reached max
            }
        }
    }
    private GameObject GetRandomTreePrefab()
    {
        float totalWeight = treePrefabs.Sum(p => p.weight);
        float roll = Random.Range(0f, totalWeight);
        foreach (var entry in treePrefabs)
        {
            if (roll < entry.weight)
                return entry.prefab;
            roll -= entry.weight;
        }
        return treePrefabs.FirstOrDefault()?.prefab;
    }

    void RemovePrefab(Vector3 position)
    {
        List<Transform> toRemove = new List<Transform>();

        // **Check both trees and prefabs separately**
        if (treeParent != null)
        {
            foreach (Transform child in treeParent)
            {
                if (Vector3.Distance(child.position, position) < brushSize * 0.5f)
                {
                    toRemove.Add(child);
                }
            }
        }

        if (prefabParent != null)
        {
            foreach (Transform child in prefabParent)
            {
                if (Vector3.Distance(child.position, position) < brushSize * 0.5f)
                {
                    toRemove.Add(child);
                }
            }
        }

        foreach (Transform child in toRemove)
        {
            DestroyImmediate(child.gameObject);
        }
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
}
