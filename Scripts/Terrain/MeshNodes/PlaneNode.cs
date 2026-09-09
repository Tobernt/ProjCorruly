using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class PlaneNode : MeshNode
{
    public int width = 10;
    public int height = 10;
    public int subdivisions = 5;
    public Material defaultMaterial;

    private int lastWidth = -1, lastHeight = -1, lastSubdivisions = -1;

    public override ProBuilderMesh GenerateMesh(ProBuilderMesh existingMesh)
    {
        int optimizedSubdivisions = Mathf.Clamp(subdivisions, 1, 200); // Prevent extreme lag

        if (GeneratedMesh == null || width != lastWidth || height != lastHeight || subdivisions != lastSubdivisions)
        {
            lastWidth = width;
            lastHeight = height;
            lastSubdivisions = subdivisions;

            if (GeneratedMesh != null) Destroy(GeneratedMesh.gameObject);

            // Generate Plane Mesh
            GeneratedMesh = ShapeGenerator.GeneratePlane(
                PivotLocation.Center,
                width,
                height,
                optimizedSubdivisions,
                optimizedSubdivisions,
                Axis.Up
            );

            ApplyMaterial();
            GeneratedMesh.ToMesh();
            GeneratedMesh.Refresh();

            // Ensure MeshCollider is Added and Updated
            AddOrUpdateMeshCollider();
        }

        return GeneratedMesh;
    }

    private void ApplyMaterial()
    {
        if (GeneratedMesh == null) return;

        Renderer renderer = GeneratedMesh.gameObject.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            renderer = GeneratedMesh.gameObject.AddComponent<MeshRenderer>();
        }

        if (defaultMaterial != null)
        {
            renderer.sharedMaterial = defaultMaterial;
        }
        else
        {
            Debug.LogWarning("⚠ No default material assigned. Using Unity’s default material.");
            renderer.sharedMaterial = new Material(Shader.Find("Standard"));
        }
    }

    private void AddOrUpdateMeshCollider()
    {
        if (GeneratedMesh == null) return;

        MeshFilter meshFilter = GeneratedMesh.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("❌ MeshFilter or SharedMesh is missing on the generated Plane!");
            return;
        }

        // Ensure MeshCollider exists
        MeshCollider collider = GeneratedMesh.GetComponent<MeshCollider>();
        if (collider == null)
        {
            collider = GeneratedMesh.gameObject.AddComponent<MeshCollider>();
            Debug.Log("✅ MeshCollider added to PlaneNode.");
        }

        // Assign ProBuilder mesh to the MeshCollider
        collider.sharedMesh = meshFilter.sharedMesh;
        collider.convex = false; // Ensure it's a proper terrain surface
        collider.enabled = true;
    }
}
