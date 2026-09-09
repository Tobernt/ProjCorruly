using UnityEngine;
using UnityEngine.ProBuilder;
using Parabox.CSG;
using System.Collections.Generic;
using UnityEngine.ProBuilder.MeshOperations;

public class BooleanNode : MeshNode
{
    public enum BooleanOperation { Union, Subtract, Intersect }
    public BooleanOperation operation = BooleanOperation.Union;

    [SerializeField] public GameObject additionalModelPrefab; // Optional prefab as a second input

    public override ProBuilderMesh GenerateMesh(ProBuilderMesh inputMesh)
    {
        if (inputConnections.Count < 1 && additionalModelPrefab == null)
        {
            Debug.LogError("BooleanNode requires at least one input mesh or an assigned prefab!");
            return null;
        }

        GraphNode parentA = inputConnections.Count > 0 ? inputConnections[0] : null;
        GraphNode parentB = inputConnections.Count > 1 ? inputConnections[1] : null;

        ProBuilderMesh meshA = parentA is MeshNode meshNodeA ? meshNodeA.GenerateMesh(null) : null;
        ProBuilderMesh meshB = null;

        // If no second node connection, use the assigned prefab
        if (parentB is MeshNode meshNodeB)
        {
            meshB = meshNodeB.GenerateMesh(null);
        }
        else if (additionalModelPrefab != null)
        {
            meshB = ConvertPrefabToProBuilder(additionalModelPrefab);
        }

        if (meshA == null || meshB == null)
        {
            Debug.LogError("❌ BooleanNode: One or both meshes are null!");
            return null;
        }

        Debug.Log($"🛠 Performing Boolean {operation} Operation...");

        // Convert ProBuilder Meshes to GameObjects for CSG
        GameObject objA = meshA.gameObject;
        GameObject objB = meshB.gameObject;

        // Apply the selected boolean operation
        Model csgResult = null;
        switch (operation)
        {
            case BooleanOperation.Union:
                csgResult = CSG.Union(objA, objB);
                break;
            case BooleanOperation.Subtract:
                csgResult = CSG.Subtract(objA, objB);
                break;
            case BooleanOperation.Intersect:
                csgResult = CSG.Intersect(objA, objB);
                break;
        }

        if (csgResult == null)
        {
            Debug.LogError("❌ Boolean operation failed!");
            return null;
        }

        // Convert the CSG result into a new ProBuilderMesh
        return ConvertCSGResultToProBuilder(csgResult);
    }

    // Convert prefab into a ProBuilderMesh
    private ProBuilderMesh ConvertPrefabToProBuilder(GameObject prefab)
    {
        GameObject instance = GameObject.Instantiate(prefab);
        ProBuilderMesh pbMesh = instance.AddComponent<ProBuilderMesh>();
        pbMesh.ToMesh();
        pbMesh.Refresh();
        return pbMesh;
    }

    // Convert CSG result back into a ProBuilderMesh
    private ProBuilderMesh ConvertCSGResultToProBuilder(Model csgResult)
    {
        GameObject resultObj = new GameObject("Boolean Result");
        ProBuilderMesh resultMesh = resultObj.AddComponent<ProBuilderMesh>();

        Mesh mesh = csgResult.mesh;

        List<UnityEngine.ProBuilder.Vertex> pbVertices = new List<UnityEngine.ProBuilder.Vertex>();
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            pbVertices.Add(new UnityEngine.ProBuilder.Vertex
            {
                position = mesh.vertices[i],
                normal = mesh.normals.Length > i ? mesh.normals[i] : Vector3.up,
                tangent = mesh.tangents.Length > i ? mesh.tangents[i] : new Vector4(1, 0, 0, -1),
                uv0 = mesh.uv.Length > i ? mesh.uv[i] : Vector2.zero
            });
        }

        resultMesh.SetVertices(pbVertices);
        resultMesh.faces = new List<Face>();
        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            resultMesh.faces.Add(new Face(new int[] { mesh.triangles[i], mesh.triangles[i + 1], mesh.triangles[i + 2] }));
        }

        resultMesh.ToMesh();
        resultMesh.Refresh();
        Debug.Log("✅ Boolean operation completed successfully!");
        return resultMesh;
    }
}
