using UnityEngine;
using UnityEngine.ProBuilder;
using System.Collections.Generic;

public class SplatmapNode : MeshNode
{
    [SerializeField] public int splatResolution = 256;
    [SerializeField] public float noiseScale = 5f;
    [SerializeField] public float heightBlendFactor = 0.5f;
    [SerializeField] public float slopeBlendFactor = 0.5f;
    [SerializeField] public float sandThreshold = 5f;
    [SerializeField] public float grassThreshold = 15f;
    [SerializeField] public float rockThreshold = 30f;
    [SerializeField] public float snowThreshold = 50f;

    public Texture2D generatedSplatmap;

    public override ProBuilderMesh GenerateMesh(ProBuilderMesh pbMesh)
    {
        if (pbMesh == null) return null;

        Debug.Log($"🖌 Generating Splatmap at {splatResolution}x{splatResolution}");
        List<Vector3> vertices = new List<Vector3>(pbMesh.positions); // ✅ EXPLICIT CONVERSION

        generatedSplatmap = GenerateSplatmap(vertices);

        ApplySplatmap(pbMesh, generatedSplatmap);

        return pbMesh;
    }

    private Texture2D GenerateSplatmap(List<Vector3> vertices)
    {
        Texture2D splatmap = new Texture2D(splatResolution, splatResolution, TextureFormat.RGBA32, false);

        for (int y = 0; y < splatResolution; y++)
        {
            for (int x = 0; x < splatResolution; x++)
            {
                float worldX = Mathf.Lerp(-10, 10, x / (float)splatResolution);
                float worldY = Mathf.Lerp(-10, 10, y / (float)splatResolution);

                float height = SampleHeight(vertices, worldX, worldY);
                float slope = SampleSlope(vertices, worldX, worldY);

                Color splatColor = DetermineSplatColor(height, slope);
                splatmap.SetPixel(x, y, splatColor);
            }
        }

        splatmap.Apply();
        return splatmap;
    }

    private float SampleHeight(List<Vector3> vertices, float worldX, float worldY)
    {
        float minDist = float.MaxValue;
        float height = 0f;

        foreach (var vert in vertices)
        {
            float dist = Vector2.Distance(new Vector2(vert.x, vert.z), new Vector2(worldX, worldY));
            if (dist < minDist)
            {
                minDist = dist;
                height = vert.y;
            }
        }

        return height;
    }

    private float SampleSlope(List<Vector3> vertices, float worldX, float worldY)
    {
        float height = SampleHeight(vertices, worldX, worldY);
        float slope = Mathf.Abs(height - SampleHeight(vertices, worldX + 1, worldY));
        return slope;
    }

    private Color DetermineSplatColor(float height, float slope)
    {
        float sandWeight = Mathf.Clamp01(1f - Mathf.Abs(height - sandThreshold) * heightBlendFactor);
        float grassWeight = Mathf.Clamp01(1f - Mathf.Abs(height - grassThreshold) * heightBlendFactor);
        float rockWeight = Mathf.Clamp01(1f - Mathf.Abs(height - rockThreshold) * heightBlendFactor);
        float snowWeight = Mathf.Clamp01(1f - Mathf.Abs(height - snowThreshold) * heightBlendFactor);

        sandWeight *= Mathf.Clamp01(1f - slope * slopeBlendFactor);
        grassWeight *= Mathf.Clamp01(1f - Mathf.Abs(slope - 0.5f) * slopeBlendFactor);
        rockWeight *= Mathf.Clamp01(1f - Mathf.Abs(slope - 1.0f) * slopeBlendFactor);
        snowWeight *= Mathf.Clamp01(slope * slopeBlendFactor);

        return new Color(sandWeight, grassWeight, rockWeight, snowWeight);
    }

    private void ApplySplatmap(ProBuilderMesh pbMesh, Texture2D splatmap)
    {
        MeshRenderer renderer = pbMesh.GetComponent<MeshRenderer>();
        if (renderer == null)
            renderer = pbMesh.gameObject.AddComponent<MeshRenderer>();

        if (renderer.sharedMaterial == null)
            renderer.sharedMaterial = new Material(Shader.Find("Standard"));

        if (renderer.sharedMaterial.HasProperty("_Splatmap"))
        {
            renderer.sharedMaterial.SetTexture("_Splatmap", splatmap);
            Debug.Log("✅ Splatmap applied to mesh.");
        }
        else
        {
            Debug.LogWarning("⚠️ Shader does not support splatmaps.");
        }
    }
}
