using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Collections.Generic;

public class PrefabScatterNode : MeshNode
{
    [SerializeField] public GameObject[] prefabs; // ✅ Prefabs to scatter
    [SerializeField] public int density = 10;
    [SerializeField] public float scatterRange = 5f;
    [SerializeField] public LayerMask validLayer;
    [SerializeField] public float maxSlope = 30f; // ✅ Prevents spawning on steep surfaces
    [SerializeField] public bool alignToNormal = true; // ✅ Option to align prefabs to terrain

    // ✅ Rotation range per axis
    [SerializeField] public Vector2 rotationXRange = new Vector2(0, 0);
    [SerializeField] public Vector2 rotationYRange = new Vector2(0, 360);
    [SerializeField] public Vector2 rotationZRange = new Vector2(0, 0);

    // ✅ Scale variation
    [SerializeField] public float minScale = 0.8f;
    [SerializeField] public float maxScale = 1.5f;

    // ✅ Altitude Restrictions
    [SerializeField] public float minAltitude = 0f;
    [SerializeField] public float maxAltitude = 100f;

    public override ProBuilderMesh GenerateMesh(ProBuilderMesh pbMesh)
    {
        if (pbMesh == null || prefabs == null || prefabs.Length == 0)
        {
            Debug.LogError("❌ PrefabScatterNode: No valid prefabs assigned or ProBuilder mesh is NULL!");
            return pbMesh;
        }

        Debug.Log($"🛠 PrefabScatterNode: Attempting to scatter {density} objects on {pbMesh.name}");

        List<Vector3> vertices = new List<Vector3>(pbMesh.positions);
        List<Vector3> normals = new List<Vector3>(pbMesh.GetNormals());

        if (vertices.Count == 0 || normals.Count == 0)
        {
            Debug.LogError("❌ PrefabScatterNode: Mesh has no vertices or normals!");
            return pbMesh;
        }

        int placedPrefabs = 0;

        for (int i = 0; i < density; i++)
        {
            int randomIndex = Random.Range(0, vertices.Count);
            Vector3 spawnPos = pbMesh.transform.TransformPoint(vertices[randomIndex]);
            Vector3 normal = normals[randomIndex];

            if (IsValidPlacement(spawnPos, normal))
            {
                GameObject selectedPrefab = prefabs[Random.Range(0, prefabs.Length)];

                if (selectedPrefab == null)
                {
                    Debug.LogError("❌ PrefabScatterNode: Encountered a NULL prefab!");
                    continue;
                }

                GameObject prefabInstance = GameObject.Instantiate(selectedPrefab, spawnPos, Quaternion.identity);

                // ✅ Apply Random Rotation
                float rotX = Random.Range(rotationXRange.x, rotationXRange.y);
                float rotY = Random.Range(rotationYRange.x, rotationYRange.y);
                float rotZ = Random.Range(rotationZRange.x, rotationZRange.y);
                Quaternion randomRotation = Quaternion.Euler(rotX, rotY, rotZ);

                if (alignToNormal)
                {
                    Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up, normal);
                    prefabInstance.transform.rotation = normalRotation * randomRotation;
                }
                else
                {
                    prefabInstance.transform.rotation = randomRotation;
                }

                // ✅ Apply Random Scale
                float randomScale = Random.Range(minScale, maxScale);
                prefabInstance.transform.localScale = Vector3.one * randomScale;

                // ✅ Attach to parent
                prefabInstance.transform.SetParent(pbMesh.transform);

                Debug.Log($"✅ Instantiated {prefabInstance.name} at {spawnPos} with scale {randomScale}");

                placedPrefabs++;
            }
        }

        if (placedPrefabs == 0)
        {
            Debug.LogWarning("⚠ PrefabScatterNode: No valid positions found for prefab scattering.");
        }

        return pbMesh;
    }

    private bool IsValidPlacement(Vector3 position, Vector3 normal)
    {
        // ✅ Check altitude range
        if (position.y < minAltitude || position.y > maxAltitude)
        {
            Debug.Log($"❌ Invalid Placement: {position} - Outside Altitude Range [{minAltitude}, {maxAltitude}]");
            return false;
        }

        // ✅ Check slope
        float slope = Vector3.Angle(normal, Vector3.up);
        if (slope > maxSlope)
        {
            Debug.Log($"❌ Invalid Placement: {position} - Too Steep ({slope}° > {maxSlope}°)");
            return false;
        }

        // ✅ Raycast downward to check if it's on valid terrain
        if (!Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f, validLayer))
        {
            Debug.Log($"❌ Invalid Placement: {position} - Raycast Did Not Hit Valid Layer");
            return false;
        }

        Debug.Log($"✅ Valid Placement at {position}");
        return true;
    }
}
