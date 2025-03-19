using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Collections.Generic;
using System.Linq;

public class ProBuilderHeightPainter : EditorWindow
{
    private ProBuilderMesh pbMesh;
    private float brushSize = 1f;
    private float brushStrength = 0.1f;
    private bool isPainting = false;
    private GameObject selectedPrefab;
    private float minPrefabSpacing = 1.0f;
    private float minPrefabScale = 0.8f;
    private float maxPrefabScale = 1.2f;
    private int maxPrefabsPerBrush = 5;
    private bool alignToSurface = true;
    private Vector3 lastHitPoint;
    private bool validHit = false;
    private MeshCollider meshCollider;

    private Transform prefabParent;
    private Transform treeParent;

    private enum PaintMode { Raise, Lower, SetHeight, SmoothToLevel, PaintPrefabs, PaintTrees, ErasePrefabs }
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

        if (selectedMode == PaintMode.SetHeight || selectedMode == PaintMode.SmoothToLevel)
            targetHeight = EditorGUILayout.FloatField("Target Height", targetHeight);
        else if (selectedMode == PaintMode.PaintPrefabs || selectedMode == PaintMode.PaintTrees || selectedMode == PaintMode.ErasePrefabs)
        {
            selectedPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", selectedPrefab, typeof(GameObject), false);
            minPrefabSpacing = EditorGUILayout.FloatField("Min Prefab Spacing", minPrefabSpacing);
            minPrefabScale = EditorGUILayout.FloatField("Min Prefab Scale", minPrefabScale);
            maxPrefabScale = EditorGUILayout.FloatField("Max Prefab Scale", maxPrefabScale);
            alignToSurface = EditorGUILayout.Toggle("Align to Surface", alignToSurface);
        }

        isPainting = GUILayout.Toggle(isPainting, "Enable Painting");
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (!isPainting || pbMesh == null) return;

        Event e = Event.current;
        Ray ray = UnityEditor.HandleUtility.GUIPointToWorldRay(e.mousePosition);

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

    void UpdateMeshCollider()
    {
        if (meshCollider == null) meshCollider = pbMesh.GetComponent<MeshCollider>();
        if (meshCollider != null) meshCollider.sharedMesh = pbMesh.gameObject.GetComponent<MeshFilter>().sharedMesh;
    }


    void PlacePrefab(RaycastHit hit, bool isTree)
    {
        if (selectedPrefab == null) return;

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
                GameObject instance = Instantiate(selectedPrefab, spawnHit.point, Quaternion.identity, parent);

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
