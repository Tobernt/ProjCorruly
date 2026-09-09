using UnityEngine;
using System.Collections.Generic;

public class BiomeManager : MonoBehaviour
{
    public enum BiomeType { Forest, Desert, Mountain, Plains, Snow }

    [Header("Biome Settings")]
    public BiomeType currentBiome = BiomeType.Plains;

    private Dictionary<BiomeType, Color> biomeColors = new Dictionary<BiomeType, Color>
    {
        { BiomeType.Forest, new Color(0.1f, 0.5f, 0.1f) },   // Dark green
        { BiomeType.Desert, new Color(1.0f, 0.9f, 0.5f) },   // Sandy yellow
        { BiomeType.Mountain, new Color(0.5f, 0.5f, 0.5f) }, // Gray rocky
        { BiomeType.Plains, new Color(0.3f, 0.8f, 0.3f) },   // Bright green
        { BiomeType.Snow, Color.white }                      // Snowy white
    };

    [Header("Biome Textures")]
    public Texture2D forestTexture;
    public Texture2D desertTexture;
    public Texture2D mountainTexture;
    public Texture2D plainsTexture;
    public Texture2D snowTexture;

    private Terrain terrain;
    private TerrainData terrainData;
    private Material terrainMaterial;
    private Texture2D generatedSplatmap;

    private void Start()
    {
        terrain = FindObjectOfType<Terrain>();
        if (terrain != null)
        {
            terrainData = terrain.terrainData;
            terrainMaterial = terrain.materialTemplate;
            ApplyBiomeSettings();
        }
    }

    public void SetBiome(BiomeType biome)
    {
        currentBiome = biome;
        ApplyBiomeSettings();
    }

    private void ApplyBiomeSettings()
    {
        if (terrainMaterial == null)
        {
            Debug.LogWarning("⚠️ No terrain material found!");
            return;
        }

        Debug.Log($"🌍 Applying Biome: {currentBiome}");

        // Update terrain color
        terrainMaterial.color = biomeColors[currentBiome];

        // Assign biome-specific textures
        switch (currentBiome)
        {
            case BiomeType.Forest:
                terrainMaterial.SetTexture("_MainTex", forestTexture);
                break;
            case BiomeType.Desert:
                terrainMaterial.SetTexture("_MainTex", desertTexture);
                break;
            case BiomeType.Mountain:
                terrainMaterial.SetTexture("_MainTex", mountainTexture);
                break;
            case BiomeType.Plains:
                terrainMaterial.SetTexture("_MainTex", plainsTexture);
                break;
            case BiomeType.Snow:
                terrainMaterial.SetTexture("_MainTex", snowTexture);
                break;
        }

        GenerateBiomeSplatmap();
    }

    private void GenerateBiomeSplatmap()
    {
        Debug.Log("🖌 Generating Biome Splatmap...");

        int resolution = terrainData.alphamapResolution;
        float[,,] splatmap = new float[resolution, resolution, 5];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float height = terrainData.GetHeight(x, y) / terrainData.size.y;
                splatmap[x, y, 0] = Mathf.Clamp01(1f - Mathf.Abs(height - 0.2f)); // Sand
                splatmap[x, y, 1] = Mathf.Clamp01(1f - Mathf.Abs(height - 0.4f)); // Grass
                splatmap[x, y, 2] = Mathf.Clamp01(1f - Mathf.Abs(height - 0.6f)); // Rock
                splatmap[x, y, 3] = Mathf.Clamp01(1f - Mathf.Abs(height - 0.8f)); // Snow
            }
        }

        terrainData.SetAlphamaps(0, 0, splatmap);
        Debug.Log("✅ Biome Splatmap Applied!");
    }
}
