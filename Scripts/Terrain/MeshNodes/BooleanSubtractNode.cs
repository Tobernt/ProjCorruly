using UnityEngine;
using UnityEngine.ProBuilder;
using Parabox.CSG;
using System.Collections.Generic;
using UnityEngine.ProBuilder.MeshOperations;

public class BooleanSubtractNode : MeshNode
{
    [SerializeField] public GameObject additionalModelPrefab; // Optional prefab as second input

    public override ProBuilderMesh GenerateMesh(ProBuilderMesh inputMesh)
    {
        if (inputConnections.Count < 1 && additionalModelPrefab == null)
        {
            Debug.LogError("❌ BooleanSubtractNode requires at least one input mesh or an assigned prefab!");
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
            Debug.LogError("❌ BooleanSubtractNode: One or both meshes are null!");
            return null;
        }

        Debug.Log($"🛠 Performing Boolean Subtraction on {meshA.vertexCount} and {meshB.vertexCount} vertices.");

        // Convert ProBuilder Meshes to GameObjects for CSG
        GameObject objA = meshA.gameObject;
        GameObject objB = meshB.gameObject;

        if (objA == null || objB == null)
        {
            Debug.LogError("❌ BooleanSubtractNode: Failed to get GameObject references for CSG!");
            return null;
        }

        Model resultModel = CSG.Subtract(objA, objB);

        if (resultModel == null)
        {
            Debug.LogError("❌ Boolean Subtraction failed!");
            return null;
        }

        // Convert CSG result back into a ProBuilderMesh
        ProBuilderMesh resultMesh = ConvertCSGResultToProBuilder(resultModel);

        // Cleanup generated GameObjects to avoid memory leaks
        GameObject.Destroy(objA);
        GameObject.Destroy(objB);

        return resultMesh;
    }

    // Convert prefab into a ProBuilderMesh correctly
    private ProBuilderMesh ConvertPrefabToProBuilder(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("❌ ConvertPrefabToProBuilder: Given prefab is NULL!");
            return null;
        }

        GameObject instance = GameObject.Instantiate(prefab);
        instance.name = "ConvertedPrefab";

        ProBuilderMesh pbMesh = instance.GetComponent<ProBuilderMesh>();
        if (pbMesh == null)
        {
            pbMesh = instance.AddComponent<ProBuilderMesh>();
        }

        pbMesh.ToMesh();
        pbMesh.Refresh();

        Debug.Log($"✅ Prefab {prefab.name} converted to ProBuilderMesh with {pbMesh.vertexCount} vertices.");
        return pbMesh;
    }

    // Convert CSG result back into a ProBuilderMesh
    private ProBuilderMesh ConvertCSGResultToProBuilder(Model csgResult)
    {
        if (csgResult == null || csgResult.mesh == null)
        {
            Debug.LogError("❌ ConvertCSGResultToProBuilder: Received NULL CSG mesh!");
            return null;
        }

        GameObject resultObj = new GameObject("Boolean Subtract Result");
        ProBuilderMesh resultMesh = resultObj.AddComponent<ProBuilderMesh>();

        Mesh mesh = csgResult.mesh;

        // Convert Parabox.CSG.Vertex to UnityEngine.ProBuilder.Vertex
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

        Debug.Log($"✅ Boolean Subtraction completed successfully! Result has {resultMesh.vertexCount} vertices.");
        return resultMesh;
    }
}
