using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Collections.Generic;

public class ErosionNode : MeshNode
{
    [SerializeField] public int erosionIterations = 5;
    [SerializeField] public float erosionStrength = 0.1f;
    [SerializeField] public float rainAmount = 0.02f;
    [SerializeField] public float evaporationRate = 0.05f;
    [SerializeField] public float sedimentCapacity = 0.3f;
    [SerializeField] public float thermalStrength = 0.1f;
    [SerializeField] public float maxDisplacement = 0.05f; // Limits vertex movement per step

    private ComputeShader erosionShader;
    private ComputeBuffer vertexBuffer;
    private int kernelHandle;

    struct VertexData
    {
        public Vector3 position;
        public float water;
        public float sediment;
    }

    public override ProBuilderMesh GenerateMesh(ProBuilderMesh inputMesh)
    {
        if (inputMesh == null)
        {
            Debug.LogError("❌ ErosionNode requires a valid input mesh!");
            return null;
        }

        Debug.Log($"🌊 ErosionNode: Applying {erosionIterations} erosion passes on GPU...");

        List<Vector3> positions = new List<Vector3>(inputMesh.positions);
        int vertexCount = positions.Count;

        // Load Compute Shader
        if (erosionShader == null)
            erosionShader = Resources.Load<ComputeShader>("ErosionComputeShader");

        if (erosionShader == null)
        {
            Debug.LogError("❌ Compute Shader not found! Ensure it's in Resources folder.");
            return null;
        }

        // Setup Compute Shader
        kernelHandle = erosionShader.FindKernel("CSMain");

        // Create Compute Buffer
        vertexBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 5);
        VertexData[] vertexData = new VertexData[vertexCount];

        for (int i = 0; i < vertexCount; i++)
        {
            vertexData[i].position = positions[i];
            vertexData[i].water = 0f;
            vertexData[i].sediment = 0f;
        }

        vertexBuffer.SetData(vertexData);
        erosionShader.SetBuffer(kernelHandle, "vertices", vertexBuffer);

        // Set Shader Parameters
        erosionShader.SetInt("vertexCount", vertexCount);
        erosionShader.SetFloat("rainAmount", rainAmount);
        erosionShader.SetFloat("erosionStrength", erosionStrength);
        erosionShader.SetFloat("evaporationRate", evaporationRate);
        erosionShader.SetFloat("sedimentCapacity", sedimentCapacity);
        erosionShader.SetFloat("thermalStrength", thermalStrength);
        erosionShader.SetFloat("maxDisplacement", maxDisplacement);

        // Run Compute Shader
        int threadGroups = Mathf.CeilToInt(vertexCount / 256.0f);
        for (int i = 0; i < erosionIterations; i++)
        {
            erosionShader.Dispatch(kernelHandle, threadGroups, 1, 1);
        }

        // Retrieve Data from GPU
        vertexBuffer.GetData(vertexData);

        for (int i = 0; i < vertexCount; i++)
        {
            positions[i] = vertexData[i].position;
        }

        // Cleanup
        vertexBuffer.Release();

        // Apply Updated Mesh Data
        inputMesh.positions = positions;
        inputMesh.ToMesh();
        inputMesh.Refresh();

        Debug.Log($"✅ ErosionNode: Completed {erosionIterations} GPU-accelerated passes.");
        return inputMesh;
    }
}
