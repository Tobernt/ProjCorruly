using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class MeshSmoothingNode : MeshNode
{
    [SerializeField] public int smoothingIterations = 3; // Allow setting iterations

    public override ProBuilderMesh GenerateMesh(ProBuilderMesh pbMesh)
    {
        if (pbMesh == null) return null;

        Debug.Log($"🛠 MeshSmoothingNode: Smoothing {pbMesh.vertexCount} vertices with {smoothingIterations} iterations.");


        List<Vector3> modifiablePositions = new List<Vector3>(pbMesh.positions);

        for (int it = 0; it < smoothingIterations; it++)
        {
            for (int i = 0; i < modifiablePositions.Count; i++)
            {
                Vector3 avg = Vector3.zero;
                int neighborCount = 0;

                foreach (var vertex in modifiablePositions)
                {
                    if (Vector3.Distance(modifiablePositions[i], vertex) < 5f) // Find close vertices
                    {
                        avg += vertex;
                        neighborCount++;
                    }
                }

                if (neighborCount > 0)
                {
                    modifiablePositions[i] = avg / neighborCount; // Smooth vertex position
                }
            }
        }

        pbMesh.positions = modifiablePositions; // Assign back writable list
        pbMesh.ToMesh();
        pbMesh.Refresh();

        Debug.Log($"✅ MeshSmoothingNode: Smoothing complete.");
        return pbMesh;
    }
}
