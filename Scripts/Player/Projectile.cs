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

    private PhysicsScene physicsScene;
    private Vector3 lastPosition;
    private int bounceCount = 0;

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
            Vector3 nextPosition = transform.position + transform.forward * speed * Time.deltaTime;
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

        // ✅ Ignore collisions with other projectiles
        if (hitObject.CompareTag("Projectile"))
        {
            return;
        }
        // ✅ Ignore collisions with the IgnoreHits layer
        if (hitObject.layer == LayerMask.NameToLayer("IgnoreHits"))
        {
            return;
        }

        // ✅ Apply damage if the object is Punchable
        if (hitObject.CompareTag("Punchable") && hitObject.scene == gameObject.scene)
        {
            Health health = hitObject.GetComponent<Health>();
            if (health != null)
            {
                Debug.Log($"🎯 Projectile hit {hitObject.name}, dealing {damage} damage.");
                health.TakeDamage(damage);
            }

            // ✅ Destroy if we hit a damageable object
            NetworkServer.Destroy(gameObject);
            return;
        }

        // ✅ If max bounces reached, destroy the projectile
        if (bounceCount >= maxBounces)
        {
            NetworkServer.Destroy(gameObject);
            return;
        }

        // ✅ Reflect the projectile off the surface
        Vector3 reflectDirection = Vector3.Reflect(transform.forward, hit.normal);
        transform.rotation = Quaternion.LookRotation(reflectDirection);

        // ✅ Sync bounce effect to all clients
        RpcSyncBounce(transform.position, reflectDirection);

        bounceCount++;
        Debug.Log($"🔄 Projectile bounced! Remaining bounces: {maxBounces - bounceCount}");
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
