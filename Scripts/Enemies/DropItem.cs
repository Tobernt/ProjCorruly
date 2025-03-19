using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class EnemyDropTable : NetworkBehaviour
{
    [System.Serializable]
    public class DropEntry
    {
        public GameObject itemPrefab;
        [Range(0f, 100f)]
        public float dropChance = 10f;
    }

    [Header("Loot Table")]
    public DropEntry[] possibleDrops;

    [Header("Drop Settings")]
    public Transform dropOrigin;             // Optional: specify a custom drop point
    public float scatterRadius = 0.5f;       // Radius to randomize XZ drop position
    public float dropVerticalOffset = 0.5f;  // Y offset to prevent overlapping with ground

    private PhysicsScene physicsScene;

    private void Awake()
    {
        physicsScene = gameObject.scene.GetPhysicsScene(); // Get the physics scene
    }

    [Server]
    public void DropLoot()
    {
        if (possibleDrops == null || possibleDrops.Length == 0) return;

        Vector3 center = GetCenterPosition();

        foreach (var drop in possibleDrops)
        {
            float roll = Random.Range(0f, 100f);
            if (roll <= drop.dropChance)
            {
                Vector2 randomXZ = Random.insideUnitCircle * scatterRadius;
                Vector3 spawnPos = new Vector3(center.x + randomXZ.x, center.y + dropVerticalOffset, center.z + randomXZ.y);

                GameObject spawned = Instantiate(drop.itemPrefab, spawnPos, Quaternion.identity);
                SceneManager.MoveGameObjectToScene(spawned, gameObject.scene);
                NetworkServer.Spawn(spawned);

                Debug.Log($"💰 Dropped: {drop.itemPrefab.name} (rolled {roll:F2} ≤ {drop.dropChance})");
            }
        }
    }

    /// <summary>
    /// Gets the center of the collider bounds, or transform.position if no collider exists.
    /// </summary>
    private Vector3 GetCenterPosition()
    {
        if (dropOrigin != null)
            return dropOrigin.position;

        Collider col = GetComponent<Collider>();
        if (col != null)
            return col.bounds.center;

        Debug.LogWarning("[EnemyDropTable] No collider or dropOrigin found! Using transform.position.");
        return transform.position;
    }
}
