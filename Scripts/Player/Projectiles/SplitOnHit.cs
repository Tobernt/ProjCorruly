using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class SplitOnHit : NetworkBehaviour
{
    public GameObject projectilePrefab;
    public int splitCount = 3;
    public float spreadAngle = 30f;
    public int maxSplitDepth = 1;
    public bool useChaoticSpread = false; // ✅ New toggle

    [SyncVar] public int currentSplitDepth = 0;

    public void OnHit(RaycastHit hit)
    {
        if (!NetworkServer.active || projectilePrefab == null) return;
        if (currentSplitDepth >= maxSplitDepth) return;

        Vector3 reflectDirection = Vector3.Reflect(transform.forward, hit.normal);

        for (int i = 0; i < splitCount; i++)
        {
            Vector3 direction;

            if (useChaoticSpread)
            {
                // ✅ Chaotic full-3D scatter
                direction = Quaternion.Euler(
                    Random.Range(-spreadAngle, spreadAngle),
                    Random.Range(-spreadAngle, spreadAngle),
                    0f) * reflectDirection;
            }
            else
            {
                // ✅ Standard cone-based scatter
                float angle = ((float)i / (splitCount - 1) - 0.5f) * spreadAngle;
                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
                direction = rotation * reflectDirection;
            }

            GameObject split = Instantiate(projectilePrefab, transform.position, Quaternion.LookRotation(direction));
            SceneManager.MoveGameObjectToScene(split, gameObject.scene);
            NetworkServer.Spawn(split);

            if (split.TryGetComponent(out IProjectile p))
                p.Initialize(direction, 0f);

            if (split.TryGetComponent(out SplitOnHit splitComp))
            {
                splitComp.currentSplitDepth = currentSplitDepth + 1;
                splitComp.useChaoticSpread = useChaoticSpread; // pass on style
            }
        }
    }
}
