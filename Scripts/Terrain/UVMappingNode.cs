using UnityEngine;
using UnityEngine.ProBuilder;
using System.Collections.Generic;

public class UVMappingNode : MeshNode
{
    public enum UVProjectionType { WorldSpace, Planar, Triplanar, Cylindrical }
    public enum NoiseType { None, Perlin, Worley }

    [SerializeField] public UVProjectionType uvProjection = UVProjectionType.WorldSpace;
    [SerializeField] public NoiseType noiseType = NoiseType.None;
    [SerializeField] public Vector2 uvScale = new Vector2(1f, 1f);
    [SerializeField] public Vector2 uvOffset = Vector2.zero;
    [SerializeField] public Material blendMaterial;

    [SerializeField] public Texture2D sandTexture, grassTexture, rockTexture, snowTexture;
    [SerializeField] public float heightBlendFactor = 0.5f;
    [SerializeField] public float slopeBlendFactor = 0.5f;
    [SerializeField] public float noiseScale = 5f;
    [SerializeField] public float noiseStrength = 0.5f;
    [SerializeField] public float sandThreshold = 5f;
    [SerializeField] public float grassThreshold = 15f;
    [SerializeField] public float rockThreshold = 30f;
    [SerializeField] public float snowThreshold = 50f;

    public override ProBuilderMesh GenerateMesh(ProBuilderMesh pbMesh)
    {
        if (pbMesh == null) return null;

        List<Vector2> uvs2D = GenerateUVs(pbMesh.positions, uvProjection);
        List<Vector4> uvs4D = ConvertToVector4(uvs2D, pbMesh.positions);

        pbMesh.SetUVs(0, uvs4D); // Set UVs with biome weight in z component
        pbMesh.ToMesh();
        pbMesh.Refresh();

        AssignMaterial(pbMesh);

        Debug.Log($"✅ UVMappingNode: Applied {uvProjection} UV projection with {noiseType} biome blending.");
        return pbMesh;
    }

    private List<Vector2> GenerateUVs(IList<Vector3> vertices, UVProjectionType projection)
    {
        List<Vector2> uvs = new List<Vector2>();
        foreach (var v in vertices)
        {
            switch (projection)
            {
                case UVProjectionType.WorldSpace:
                    uvs.Add(new Vector2(v.x, v.z));
                    break;
                case UVProjectionType.Planar:
                    uvs.Add(new Vector2(v.x, v.y));
                    break;
                case UVProjectionType.Triplanar:
                    uvs.Add(new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y)));
                    break;
                case UVProjectionType.Cylindrical:
                    float angle = Mathf.Atan2(v.z, v.x) / (2 * Mathf.PI) + 0.5f;
                    uvs.Add(new Vector2(angle, v.y));
                    break;
            }
        }
        return uvs;
    }

    private List<Vector4> ConvertToVector4(List<Vector2> uvs2D, IList<Vector3> positions)
    {
        List<Vector4> uvs4D = new List<Vector4>();
        for (int i = 0; i < uvs2D.Count; i++)
        {
            Vector2 adjustedUV = (uvs2D[i] * uvScale) + uvOffset;
            float heightFactor = Mathf.Clamp01(positions[i].y * heightBlendFactor);
            float slopeFactor = Mathf.Clamp01(Vector3.Dot(Vector3.up, positions[i].normalized) * slopeBlendFactor);
            float blend = Mathf.Lerp(heightFactor, slopeFactor, 0.5f);

            if (noiseType == NoiseType.Perlin)
                blend *= Mathf.PerlinNoise(adjustedUV.x * noiseScale, adjustedUV.y * noiseScale) * noiseStrength;

            if (noiseType == NoiseType.Worley)
                blend *= WorleyNoise(adjustedUV.x, adjustedUV.y, noiseScale) * noiseStrength;

            uvs4D.Add(new Vector4(adjustedUV.x, adjustedUV.y, blend, DetermineBiomeWeight(positions[i].y)));
        }

        return uvs4D;
    }

    private float DetermineBiomeWeight(float height)
    {
        if (height < sandThreshold) return 0f;
        if (height < grassThreshold) return 0.33f;
        if (height < rockThreshold) return 0.66f;
        return 1f;
    }

    private void AssignMaterial(ProBuilderMesh pbMesh)
    {
        if (blendMaterial == null)
        {
            Debug.LogWarning("⚠️ No blend material assigned! Default material will be used.");
            return;
        }

        MeshRenderer renderer = pbMesh.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            renderer = pbMesh.gameObject.AddComponent<MeshRenderer>();
        }

        renderer.sharedMaterial = blendMaterial;

        if (blendMaterial.HasProperty("_Texture1") && sandTexture != null) blendMaterial.SetTexture("_Texture1", sandTexture);
        if (blendMaterial.HasProperty("_Texture2") && grassTexture != null) blendMaterial.SetTexture("_Texture2", grassTexture);
        if (blendMaterial.HasProperty("_Texture3") && rockTexture != null) blendMaterial.SetTexture("_Texture3", rockTexture);
        if (blendMaterial.HasProperty("_Texture4") && snowTexture != null) blendMaterial.SetTexture("_Texture4", snowTexture);

        if (blendMaterial.HasProperty("_HeightBlendFactor")) blendMaterial.SetFloat("_HeightBlendFactor", heightBlendFactor);
        if (blendMaterial.HasProperty("_SlopeBlendFactor")) blendMaterial.SetFloat("_SlopeBlendFactor", slopeBlendFactor);

        Debug.Log("✅ Biome Blend Material & Textures Assigned!");
    }

    private float WorleyNoise(float x, float y, float scale)
    {
        int xi = Mathf.FloorToInt(x * scale);
        int yi = Mathf.FloorToInt(y * scale);
        float minDist = 1.0f;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                Vector2 cell = new Vector2(xi + dx + Random.value, yi + dy + Random.value);
                float dist = Vector2.Distance(new Vector2(x * scale, y * scale), cell);
                minDist = Mathf.Min(minDist, dist);
            }
        }

        return minDist;
    }
}
