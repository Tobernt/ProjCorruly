using UnityEngine;

[CreateAssetMenu(menuName = "Graph/Nodes/MaskNode")]
public class MaskNode : GraphNode
{
    public enum NoiseType { Perlin, Simplex, Worley }

    [SerializeField] public Texture2D maskTexture; // ✅ Optional user texture
    [SerializeField] public float maskStrength = 1f;
    [SerializeField] public float maskScale = 10f;
    [SerializeField] public bool invertMask = false; // ✅ New toggle for effect inversion

    [SerializeField] public bool useRandomSeed = true;
    [SerializeField] public int seed = 42;
    [SerializeField] public int resolution = 512;
    [SerializeField] public NoiseType noiseType = NoiseType.Perlin;

    private Texture2D generatedMaskTexture;
    private bool settingsChanged = true;

    public void OnValidate()
    {
        settingsChanged = true;
    }

    public float GetMaskValue(float x, float z)
    {
        // ✅ Only regenerate procedural mask if **no manual texture is provided**
        if (settingsChanged && maskTexture == null)
        {
            Debug.Log($"🎭 MaskNode: Regenerating procedural mask with {noiseType} noise.");
            generatedMaskTexture = GenerateProceduralMask();
            settingsChanged = false;
        }

        Texture2D activeMask = maskTexture ?? generatedMaskTexture;
        float maskValue = SampleTexture(activeMask, x, z);

        return invertMask ? 1f - maskValue : maskValue; // ✅ Flip effect but NOT the image itself
    }

    private float SampleTexture(Texture2D texture, float x, float z)
    {
        float u = Mathf.InverseLerp(-maskScale / 2f, maskScale / 2f, x);
        float v = Mathf.InverseLerp(-maskScale / 2f, maskScale / 2f, z);

        int texX = Mathf.FloorToInt(u * (texture.width - 1));
        int texY = Mathf.FloorToInt(v * (texture.height - 1));

        texX = Mathf.Clamp(texX, 0, texture.width - 1);
        texY = Mathf.Clamp(texY, 0, texture.height - 1);

        Color pixel = texture.GetPixel(texX, texY);
        float maskValue = pixel.grayscale * maskStrength;

        return maskValue; // ✅ No inversion here, effect is handled outside
    }

    private Texture2D GenerateProceduralMask()
    {
        Texture2D mask = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
        if (useRandomSeed) seed = Random.Range(0, 99999);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float nx = (float)x / resolution;
                float ny = (float)y / resolution;
                float value = GenerateNoise(nx, ny);

                value = Mathf.Pow(value, 3f); // ✅ Increase contrast

                mask.SetPixel(x, y, new Color(value, value, value));
            }
        }

        mask.Apply();
        return mask;
    }

    private float GenerateNoise(float x, float y)
    {
        switch (noiseType)
        {
            case NoiseType.Perlin: return Mathf.PerlinNoise((x + seed) * 10f, (y + seed) * 10f);
            case NoiseType.Simplex: return GenerateSimplexNoise(x, y);
            case NoiseType.Worley: return GenerateWorleyNoise(x, y);
            default: return 1f;
        }
    }

    private float GenerateSimplexNoise(float x, float y)
    {
        int i0 = Mathf.FloorToInt(x);
        int i1 = i0 + 1;
        int j0 = Mathf.FloorToInt(y);
        int j1 = j0 + 1;

        float sx = x - i0;
        float sy = y - j0;

        float n0 = Mathf.PerlinNoise(i0, j0);
        float n1 = Mathf.PerlinNoise(i1, j0);
        float ix0 = Mathf.Lerp(n0, n1, sx);

        float n2 = Mathf.PerlinNoise(i0, j1);
        float n3 = Mathf.PerlinNoise(i1, j1);
        float ix1 = Mathf.Lerp(n2, n3, sx);

        return Mathf.Lerp(ix0, ix1, sy);
    }

    private float GenerateWorleyNoise(float x, float y)
    {
        int gridSize = 4;
        float minDist = float.MaxValue;

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                float featureX = Mathf.Floor(x * gridSize) + i + Random.Range(0f, 1f);
                float featureY = Mathf.Floor(y * gridSize) + j + Random.Range(0f, 1f);
                float dist = Vector2.Distance(new Vector2(x * gridSize, y * gridSize), new Vector2(featureX, featureY));
                minDist = Mathf.Min(minDist, dist);
            }
        }

        return minDist;
    }
}
