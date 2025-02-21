using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Linq;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Graph/Nodes/MeshExporterNode")]
public class MeshExporterNode : MeshNode
{
    public ComputeShader normalComputeShader; // ✅ Compute Shader for GPU-based normal calculation

    public override ProBuilderMesh GenerateMesh(ProBuilderMesh inputMesh)
    {
        if (inputMesh == null)
        {
            Debug.LogError($"❌ MeshExporterNode {name}: Received a NULL mesh!");
            return null;
        }

        Debug.Log($"🛠 MeshExporterNode {name}: Exporting mesh with {inputMesh.vertexCount} vertices.");
        return inputMesh;
    }

    public void ExportMesh(ProBuilderMesh mesh)
    {
        if (mesh == null)
        {
            Debug.LogError("❌ MeshExporterNode tried to export a NULL mesh!");
            return;
        }

        GameObject exportedObject = new GameObject("Final Exported Mesh");
        MeshFilter meshFilter = exportedObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = exportedObject.AddComponent<MeshRenderer>();

        Mesh finalMesh = new Mesh();
        mesh.ToMesh();  // ✅ Convert ProBuilder mesh to Unity mesh
        mesh.Refresh(); // ✅ Ensure it's updated

        finalMesh.vertices = mesh.positions.ToArray();
        finalMesh.triangles = mesh.faces.SelectMany(f => f.indexes).ToArray();

        // ✅ **Use GPU for normal recalculation if supported**
        if (SystemInfo.supportsComputeShaders && normalComputeShader != null)
        {
            Debug.Log("⚡ Using GPU Compute Shader for normal recalculation...");
            finalMesh.normals = RecalculateNormalsGPU(finalMesh);
        }
        else
        {
            Debug.LogWarning("⚠️ Compute Shaders not supported, falling back to CPU normals.");
            finalMesh.RecalculateNormals(); // ✅ Fallback to CPU calculation
        }

        meshFilter.mesh = finalMesh;
        meshRenderer.material = new Material(Shader.Find("Standard"));

        Debug.Log("✅ Final Mesh Exported to Scene!");
    }

    // 🔥 **GPU-based normal recalculation using Compute Shader**
    private Vector3[] RecalculateNormalsGPU(Mesh mesh)
    {
        int vertexCount = mesh.vertexCount;
        Vector3[] normals = new Vector3[vertexCount];

        // ✅ Set up Compute Buffers
        ComputeBuffer vertexBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3);
        ComputeBuffer normalBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3);

        vertexBuffer.SetData(mesh.vertices);
        normalComputeShader.SetBuffer(0, "vertices", vertexBuffer);
        normalComputeShader.SetBuffer(0, "normals", normalBuffer);
        normalComputeShader.SetInt("vertexCount", vertexCount);

        // ✅ Dispatch Compute Shader
        int threadGroups = Mathf.CeilToInt(vertexCount / 64f);
        normalComputeShader.Dispatch(0, threadGroups, 1, 1);

        // ✅ Get results back from GPU
        normalBuffer.GetData(normals);

        // ✅ Cleanup
        vertexBuffer.Release();
        normalBuffer.Release();

        return normals;
    }
}
