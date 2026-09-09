using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

public class ChainLightning : NetworkBehaviour
{
    public int maxChains = 3;
    public float chainRadius = 6f;
    public int damage = 20;

    private HashSet<GameObject> alreadyHit = new HashSet<GameObject>();
    private Projectile projectile;
    private GameObject currentTarget;

    private void Awake()
    {
        projectile = GetComponent<Projectile>();
    }

    [Server]
    public bool OnHit(RaycastHit hit)
    {
        GameObject target = hit.collider.gameObject;

        if (!target.CompareTag("Punchable"))
            return false;

        if (!alreadyHit.Add(target)) return false; // Already hit this one

        currentTarget = target;

        // Sync damage from projectile (failsafe)
        if (damage <= 0)
            damage = projectile.damage;

        // Deal damage
        if (target.TryGetComponent(out Health health))
        {
            health.TakeDamage(damage);
        }

        // Update chain state
        projectile.chainCount++;

        if (projectile.chainCount >= projectile.maxChains)
        {
            NetworkServer.Destroy(gameObject);
            return true;
        }

        StartCoroutine(ChainToNextTarget());
        return true;
    }

    [Server]
    private IEnumerator ChainToNextTarget()
    {
        yield return new WaitForSeconds(0.05f); // short delay

        GameObject next = FindClosestValidTarget(currentTarget.transform.position);

        if (next == null)
        {
            NetworkServer.Destroy(gameObject);
            yield break;
        }

        projectile.StartChainToTarget(next);
        projectile.transform.rotation = Quaternion.LookRotation(
            (next.transform.position - projectile.transform.position).normalized
        );
    }

    private GameObject FindClosestValidTarget(Vector3 origin)
    {
        GameObject[] punchables = GameObject.FindGameObjectsWithTag("Punchable");
        GameObject closest = null;
        float closestDist = float.MaxValue;

        foreach (GameObject obj in punchables)
        {
            if (!obj || alreadyHit.Contains(obj)) continue;

            float dist = Vector3.Distance(origin, obj.transform.position);
            if (dist > chainRadius) continue;

            if (dist < closestDist)
            {
                closest = obj;
                closestDist = dist;
            }
        }

        return closest;
    }
}
