using UnityEngine;
using Mirror;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Spawner Settings")]
    public GameObject enemyPrefab;
    public int maxCount = 5;
    public float spawnRadius = 10f;
    public LayerMask groundLayer;

    [Header("Height Offset Settings")]
    public float minYOffset = 0.5f; // Minimum Y offset from ground
    public float maxYOffset = 3f; // Maximum Y offset from ground

    [Header("Spawn Settings")]
    public bool spawnIndividually = false; // Toggle for single vs batch respawn
    public float spawnInterval = 5f; // Adjustable time between spawns

    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private PhysicsScene physicsScene;
    private bool hasPlayers = false;

    public override void OnStartServer()
    {
        physicsScene = gameObject.scene.GetPhysicsScene();
        Debug.Log($"[Spawner] Physics Scene Assigned: {physicsScene.IsValid()}");

        StartCoroutine(CheckForPlayers());
    }

    private IEnumerator CheckForPlayers()
    {
        while (!PlayersExist())
        {
            Debug.Log("[Spawner] No players found. Waiting...");
            yield return new WaitForSeconds(1f);
        }

        hasPlayers = true;
        Debug.Log("[Spawner] Players found! Spawning enemies...");

        for (int i = 0; i < maxCount; i++)
        {
            SpawnEnemy();
        }
    }

    private bool PlayersExist()
    {
        return GameObject.FindGameObjectWithTag("Player") != null;
    }

    [Server]
    void SpawnEnemy()
    {
        if (!hasPlayers) return;

        Vector3 spawnPoint = GetValidSpawnPoint();
        if (spawnPoint == Vector3.zero)
        {
            Debug.LogWarning("[Spawner] No valid spawn point found!");
            return;
        }

        Debug.Log($"[Spawner] Spawning enemy at {spawnPoint}");

        Quaternion randomYRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint, randomYRotation);
        SceneManager.MoveGameObjectToScene(enemy, gameObject.scene);
        NetworkServer.Spawn(enemy);

        Health health = enemy.GetComponent<Health>();
        if (health != null)
        {
            health.OnDeath += () => OnEnemyDeath(enemy);
        }

        spawnedEnemies.Add(enemy);
    }

    [Server]
    void OnEnemyDeath(GameObject enemy)
    {
        spawnedEnemies.Remove(enemy);
        if (enemy != null)
            NetworkServer.Destroy(enemy);

        if (spawnIndividually)
        {
            StartCoroutine(RespawnWithDelay()); // One-by-one respawn
        }
        else
        {
            StartCoroutine(RespawnAllMissing()); // Batch respawn with intervals
        }
    }

    [Server]
    IEnumerator RespawnWithDelay()
    {
        yield return new WaitForSeconds(spawnInterval);
        if (spawnedEnemies.Count < maxCount && hasPlayers)
        {
            SpawnEnemy();
        }
    }

    [Server]
    IEnumerator RespawnAllMissing()
    {
        int missingEnemies = maxCount - spawnedEnemies.Count;
        for (int i = 0; i < missingEnemies; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnInterval); // Delay between batch spawns
        }
    }

    private Vector3 GetValidSpawnPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * spawnRadius;
            randomPoint.y = transform.position.y + 10f;

            RaycastHit hit = new RaycastHit(); // Initialize hit to avoid compiler error
            bool hitSomething = physicsScene.IsValid() && physicsScene.Raycast(randomPoint, Vector3.down, out hit, 20f, groundLayer);

            if (!hitSomething)
            {
                Debug.Log($"[Spawner] physicsScene.Raycast failed at {randomPoint}. Trying global Physics.Raycast...");
                hitSomething = Physics.Raycast(randomPoint, Vector3.down, out hit, 20f, groundLayer);
            }

            if (hitSomething)
            {
                float heightOffset = Random.Range(minYOffset, maxYOffset);
                Vector3 spawnPos = hit.point + Vector3.up * heightOffset;

                Debug.Log($"[Spawner] Found valid ground at {spawnPos}");
                return spawnPos;
            }
        }

        Debug.LogWarning("[Spawner] No valid ground found after 10 attempts!");
        return Vector3.zero;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
