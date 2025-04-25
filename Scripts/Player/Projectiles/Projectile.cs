using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class Projectile : NetworkBehaviour, IProjectile
{
    public int damage = 20;
    public float speed = 10f;
    public float lifetime = 5f;
    public int maxBounces = 3;
    public float hitRadius = 0.2f; // ✅ Hit radius for better accuracy
    private GameObject chainTarget;
    private bool isChaining = false;
    public int chainCount = 0;
    public int maxChains = 0; // Set this from ChainLightningEffect
    private PhysicsScene physicsScene;
    private Vector3 lastPosition;
    public int bounceCount = 0;

    public void Initialize(Vector3 direction, float chargeMultiplier = 0f)
    {
        physicsScene = gameObject.scene.GetPhysicsScene();
        lastPosition = transform.position;

        // ✅ Apply scaling only if it's actually a charged shot
        if (chargeMultiplier > 0f)
        {
            float clampedMultiplier = Mathf.Clamp(chargeMultiplier, 0.1f, 1f);

            speed *= Mathf.Lerp(1f, 2f, clampedMultiplier); // ⚡ Faster bullet
            damage = Mathf.RoundToInt(damage * Mathf.Lerp(1f, 2.5f, clampedMultiplier)); // 💥 More damage

            float scale = Mathf.Lerp(1f, 1.8f, clampedMultiplier); // 📏 Bigger bullet
            transform.localScale *= scale;
        }

        transform.rotation = Quaternion.LookRotation(direction);
        Destroy(gameObject, lifetime);
    }


    void Update()
    {
        if (isServer)
        {
            // ✅ Server handles physics & collisions
            Vector3 direction;
            if (isChaining && chainTarget != null)
            {
                direction = (chainTarget.transform.position - transform.position).normalized;
                transform.rotation = Quaternion.LookRotation(direction);
            }
            else
            {
                direction = transform.forward;
            }

            Vector3 nextPosition = transform.position + direction * speed * Time.deltaTime;

            PerformRaycast(lastPosition, nextPosition);
            transform.position = nextPosition;
            lastPosition = nextPosition;
        }

        if (isClient && !isServer)
        {
            // ✅ Clients predict movement instantly for smoother gameplay
            transform.position += transform.forward * speed * Time.deltaTime;
        }
    }

    public void StartChainToTarget(GameObject target)
    {
        chainTarget = target;
        isChaining = true;
    }

    private void PerformRaycast(Vector3 start, Vector3 end)
    {
        RaycastHit hit = new RaycastHit(); // ✅ Ensure hit is initialized

        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);

        // ✅ SphereCast ensures better hit detection
        bool hitSomething = physicsScene.IsValid() &&
                            physicsScene.SphereCast(start, hitRadius, direction, out hit, distance);

        if (!hitSomething)
        {
            // ✅ Fallback to global Physics if physicsScene fails
            hitSomething = Physics.SphereCast(start, hitRadius, direction, out hit, distance);
        }

        if (hitSomething)
        {
            HandleCollision(hit);
        }
    }

    private void HandleCollision(RaycastHit hit)
    {
        GameObject hitObject = hit.collider.gameObject;

        // ✅ Ignore projectiles or ignore-hit layer
        if (hitObject.CompareTag("Projectile") || hitObject.layer == LayerMask.NameToLayer("IgnoreHits"))
            return;

        // ✅ ChainLightning takes over if present
        if (TryGetComponent<ChainLightning>(out var chain))
        {
            if (chain.OnHit(hit))
            {
                return; // ✅ ChainLightning handled everything (damage + chaining)
            }
        }

        // ✅ Run Split if present
        if (TryGetComponent<SplitOnHit>(out var split))
        {
            bounceCount++;
            split.OnHit(hit);
        }

        // ✅ Regular damage (if not handled above)
        if (hitObject.CompareTag("Punchable") && hitObject.scene == gameObject.scene)
        {
            if (hitObject.TryGetComponent(out Health health))
            {
                Debug.Log($"🎯 Hit {hitObject.name}, dealing {damage} damage.");
                health.TakeDamage(damage);
            }
        }

        // ✅ Bounce if allowed
        if (bounceCount < maxBounces)
        {
            Vector3 reflectDirection = Vector3.Reflect(transform.forward, hit.normal);
            transform.rotation = Quaternion.LookRotation(reflectDirection);
            RpcSyncBounce(transform.position, reflectDirection);
            bounceCount++;
            Debug.Log($"🔄 Bounced. Remaining bounces: {maxBounces - bounceCount}");
        }
        else
        {
            if (maxChains > 0)
            {
                // If chain is active, only destroy when chain is exhausted
                if (chainCount >= maxChains)
                {
                    NetworkServer.Destroy(gameObject);
                }
                // Otherwise, let ChainLightning continue handling
            }
            else
            {
                // Not using chaining, destroy immediately
                NetworkServer.Destroy(gameObject);
            }
        }

    }

    /// ✅ **Tell clients to update projectile bounce**
    [ClientRpc]
    private void RpcSyncBounce(Vector3 newPosition, Vector3 newDirection)
    {
        if (isServer) return; // Server already updated itself

        transform.position = newPosition;
        transform.rotation = Quaternion.LookRotation(newDirection);
    }
}
