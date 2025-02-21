using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

[CreateAssetMenu(menuName = "Graph/Nodes/NoiseDeformerNode")]
public class NoiseDeformerNode : MeshNode
{
    [SerializeField] public float strength = 5f;
    [SerializeField] public float frequency = 0.3f;
    [SerializeField] public float detailScale = 1f;
    [SerializeField] public int octaves = 4;
    [SerializeField] public float persistence = 0.5f;
    [SerializeField] public float lacunarity = 2.0f;
    [SerializeField] public bool domainWarping = true;
    [SerializeField] public bool normalizeHeight = false;

    [SerializeField] public float falloff = 100f; // ✅ NEW: Falloff 0-100%

    [SerializeField] public bool useRandomSeed = true;
    [SerializeField] public int seed = 42;

    private MaskNode detectedMaskInput = null;

    public override ProBuilderMesh GenerateMesh(ProBuilderMesh inputMesh)
    {
        if (inputMesh == null)
        {
            Debug.LogError($"❌ NoiseDeformerNode {name}: Received a NULL input mesh!");
            return null;
        }

        Debug.Log($"🛠 NoiseDeformerNode {name}: Applying deformation to {inputMesh.vertexCount} vertices.");

        detectedMaskInput = GetConnectedMaskNode();

        List<Vector3> positions = new List<Vector3>(inputMesh.positions);
        float maxHeight = float.MinValue;
        float minHeight = float.MaxValue;

        if (useRandomSeed)
        {
            seed = Random.Range(0, 99999);
        }

        Vector3 center = ComputeMeshCenter(positions); // ✅ Find the center for falloff calculation
        float maxDistance = ComputeMaxDistance(positions, center);

        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 original = positions[i];

            // ✅ Apply **Domain Warping**
            Vector3 warpedPos = original;
            if (domainWarping)
            {
                float warpX = Mathf.PerlinNoise(original.z * 0.1f + seed, original.y * 0.1f + seed) * 2f - 1f;
                float warpZ = Mathf.PerlinNoise(original.x * 0.1f + seed, original.y * 0.1f + seed) * 2f - 1f;
                warpedPos += new Vector3(warpX * 2f, 0, warpZ * 2f);
            }

            // ✅ **Multi-Octave Perlin Noise**
            float noiseValue = 0;
            float amplitude = 1f;
            float frequencyMultiplier = frequency;
            for (int o = 0; o < octaves; o++)
            {
                noiseValue += Mathf.PerlinNoise((warpedPos.x + seed) * frequencyMultiplier * detailScale,
                                                (warpedPos.z + seed) * frequencyMultiplier * detailScale) * amplitude;

                frequencyMultiplier *= lacunarity;
                amplitude *= persistence;
            }

            float heightOffset = (noiseValue - 0.5f) * 2f * strength;

            // ✅ **Apply Falloff (0-100%)**
            float distanceToCenter = Vector3.Distance(original, center);
            float falloffFactor = Mathf.Clamp01(1f - (distanceToCenter / maxDistance) * (falloff / 100f));
            heightOffset *= falloffFactor;

            // ✅ **Apply Mask if Connected**
            if (detectedMaskInput != null)
            {
                float maskValue = detectedMaskInput.GetMaskValue(original.x, original.z);
                heightOffset *= maskValue; // Modifies height based on the mask
            }

            positions[i] += Vector3.up * heightOffset;

            if (normalizeHeight)
            {
                maxHeight = Mathf.Max(maxHeight, positions[i].y);
                minHeight = Mathf.Min(minHeight, positions[i].y);
            }
        }

        if (normalizeHeight)
        {
            float heightRange = maxHeight - minHeight;
            for (int i = 0; i < positions.Count; i++)
            {
                positions[i] = new Vector3(positions[i].x, Mathf.Lerp(0, strength, (positions[i].y - minHeight) / heightRange), positions[i].z);
            }
            Debug.Log($"✅ NoiseDeformerNode {name}: Heights normalized for uniform scaling.");
        }

        // ✅ Apply final vertex positions
        inputMesh.positions = positions;
        inputMesh.ToMesh();
        inputMesh.Refresh();

        Debug.Log($"🎯 NoiseDeformerNode {name}: Advanced deformation applied successfully.");
        return inputMesh;
    }

    // ✅ **Automatically detects connected mask node**
    private MaskNode GetConnectedMaskNode()
    {
        foreach (var input in inputConnections)
        {
            if (input is MaskNode mask)
            {
                Debug.Log($"🎭 NoiseDeformerNode detected MaskNode {mask.name} as input.");
                return mask;
            }
        }
        Debug.Log($"🚨 NoiseDeformerNode found NO mask input. Using full deformation.");
        return null;
    }

    // ✅ **Compute the mesh center for falloff**
    private Vector3 ComputeMeshCenter(List<Vector3> positions)
    {
        Vector3 sum = Vector3.zero;
        foreach (Vector3 pos in positions)
        {
            sum += pos;
        }
        return sum / positions.Count;
    }

    // ✅ **Compute max distance from center to outer vertices**
    private float ComputeMaxDistance(List<Vector3> positions, Vector3 center)
    {
        float maxDist = 0f;
        foreach (Vector3 pos in positions)
        {
            maxDist = Mathf.Max(maxDist, Vector3.Distance(pos, center));
        }
        return maxDist;
    }
}
