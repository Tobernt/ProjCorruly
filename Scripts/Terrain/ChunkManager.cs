using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    [Header("Chunk Settings")]
    public int chunkSize = 10;
    public int renderDistance = 3;
    public GameObject chunkPrefab;
    public Terrain terrain;

    private Dictionary<Vector2Int, GameObject> loadedChunks = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int lastPlayerChunk;

    void Update()
    {
        Vector2Int playerChunk = GetPlayerChunkPosition();

        if (playerChunk != lastPlayerChunk)
        {
            lastPlayerChunk = playerChunk;
            LoadChunksAround(playerChunk);
            UnloadDistantChunks(playerChunk);
        }
    }

    private Vector2Int GetPlayerChunkPosition()
    {
        Vector3 playerPos = transform.position;
        return new Vector2Int(
            Mathf.FloorToInt(playerPos.x / chunkSize),
            Mathf.FloorToInt(playerPos.z / chunkSize)
        );
    }

    private void LoadChunksAround(Vector2Int centerChunk)
    {
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int y = -renderDistance; y <= renderDistance; y++)
            {
                Vector2Int chunkCoord = centerChunk + new Vector2Int(x, y);
                if (!loadedChunks.ContainsKey(chunkCoord))
                {
                    LoadChunk(chunkCoord);
                }
            }
        }
    }

    private void LoadChunk(Vector2Int coord)
    {
        GameObject chunk = Instantiate(chunkPrefab, new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize), Quaternion.identity);
        chunk.name = $"Chunk_{coord.x}_{coord.y}";
        loadedChunks[coord] = chunk;

        AdjustChunkTerrain(chunk, coord);
    }

    private void AdjustChunkTerrain(GameObject chunk, Vector2Int coord)
    {
        if (terrain == null) return;

        TerrainData terrainData = terrain.terrainData;
        float terrainHeight = terrainData.GetHeight(coord.x * chunkSize, coord.y * chunkSize);

        chunk.transform.position = new Vector3(coord.x * chunkSize, terrainHeight, coord.y * chunkSize);
    }

    private void UnloadDistantChunks(Vector2Int playerChunk)
    {
        List<Vector2Int> chunksToUnload = new List<Vector2Int>();

        foreach (var chunk in loadedChunks.Keys)
        {
            if (Vector2Int.Distance(chunk, playerChunk) > renderDistance)
            {
                chunksToUnload.Add(chunk);
            }
        }

        foreach (var chunk in chunksToUnload)
        {
            Destroy(loadedChunks[chunk]);
            loadedChunks.Remove(chunk);
        }
    }
}
