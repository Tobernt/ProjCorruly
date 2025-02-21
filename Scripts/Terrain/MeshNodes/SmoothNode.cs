using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Collections.Generic;
using System.Linq;

public class SmoothNode : MeshNode
{
    [SerializeField] public int iterations = 3; // ✅ Exposed for tweaking
    [SerializeField] public float smoothingStrength = 0.5f; // ✅ Controls smoothing intensity

    public override ProBuilderMesh GenerateMesh(ProBuilderMesh pbMesh)
    {
        if (pbMesh == null)
        {
            Debug.LogError($"❌ SmoothNode {name}: Received a NULL input mesh!");
            return null;
        }

        // ✅ Get vertex positions
        List<Vector3> positions = new List<Vector3>(pbMesh.positions);

        // ✅ Convert sharedVertices to array explicitly
        SharedVertex[] sharedVertices = pbMesh.sharedVertices.ToArray();

        // ✅ Cache adjacency map (avoid repeated calculations)
        Dictionary<int, List<int>> adjacencyMap = BuildAdjacencyMap(pbMesh);

        for (int it = 0; it < iterations; it++)
        {
            Dictionary<int, Vector3> newPositions = new Dictionary<int, Vector3>();

            foreach (SharedVertex shared in sharedVertices)
            {
                Vector3 avg = Vector3.zero;
                int count = 0;

                foreach (int vIndex in shared)
                {
                    if (!adjacencyMap.ContainsKey(vIndex)) continue;

                    foreach (int neighbor in adjacencyMap[vIndex])
                    {
                        avg += positions[neighbor];
                        count++;
                    }
                }

                if (count > 0)
                {
                    avg /= count;
                    foreach (int vIndex in shared)
                    {
                        newPositions[vIndex] = Vector3.Lerp(positions[vIndex], avg, smoothingStrength);
                    }
                }
            }

            foreach (var kvp in newPositions)
            {
                positions[kvp.Key] = kvp.Value;
            }
        }

        pbMesh.positions = positions;
        pbMesh.ToMesh();
        pbMesh.Refresh();

        Debug.Log($"✅ SmoothNode {name}: Applied {iterations} iterations of smoothing.");
        return pbMesh;
    }

    // ✅ Builds adjacency map ONCE to speed up neighbor lookup
    private Dictionary<int, List<int>> BuildAdjacencyMap(ProBuilderMesh mesh)
    {
        Dictionary<int, List<int>> adjacency = new Dictionary<int, List<int>>();

        foreach (Face face in mesh.faces)
        {
            int[] indexes = face.indexes.ToArray();

            for (int j = 0; j < indexes.Length; j++)
            {
                int currentIndex = indexes[j];

                if (!adjacency.ContainsKey(currentIndex))
                    adjacency[currentIndex] = new List<int>();

                for (int k = 0; k < indexes.Length; k++)
                {
                    if (k != j && !adjacency[currentIndex].Contains(indexes[k]))
                    {
                        adjacency[currentIndex].Add(indexes[k]);
                    }
                }
            }
        }

        return adjacency;
    }
}
