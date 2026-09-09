using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class Projectile : NetworkBehaviour, IProjectile
{
    [Header("Stats")]
    public int damage = 20;
    public float speed = 10f;
    public float lifetime = 5f;
    public int maxBounces = 3;
    public float hitRadius = 0.2f;
    [HideInInspector] public NetworkIdentity shooter;

    [Header("Chaining")]
    public int chainCount = 0;
    public int maxChains = 0;
    private bool isChaining = false;
    private GameObject chainTarget;

    [Header("Runtime")]
    private PhysicsScene physicsScene;
    private Vector3 lastPosition;
    public int bounceCount = 0;
    private ProjectileContext context;

    public void Initialize(Vector3 direction, float chargeMultiplier = 1f, ProjectileContext ctx = null)
    {
        context = ctx;
        physicsScene = gameObject.scene.GetPhysicsScene();
        lastPosition = transform.position;

        if (ctx != null)
        {
            shooter = ctx.owner;

            if (chargeMultiplier > 1f)
            {
                float clamp = Mathf.Clamp(chargeMultiplier, 1f, 2f);
                speed *= Mathf.Lerp(1f, 2f, clamp - 1f);
                damage = Mathf.RoundToInt(damage * Mathf.Lerp(1f, 2.5f, clamp - 1f));
                float scale = Mathf.Lerp(1f, 1.8f, clamp - 1f);
                transform.localScale *= scale;
            }
        }

        transform.rotation = Quaternion.LookRotation(direction);
        Destroy(gameObject, lifetime);
    }


    void Update()
    {
        if (!isServer) return;

        Vector3 direction = isChaining && chainTarget != null
            ? (chainTarget.transform.position - transform.position).normalized
            : transform.forward;

        Vector3 nextPosition = transform.position + direction * speed * Time.deltaTime;
        PerformRaycast(lastPosition, nextPosition);
        transform.position = nextPosition;
        lastPosition = nextPosition;
    }

    public void StartChainToTarget(GameObject target)
    {
        chainTarget = target;
        isChaining = true;
    }

    private void PerformRaycast(Vector3 start, Vector3 end)
    {
        RaycastHit hit = new RaycastHit(); // Ensure it's assigned
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);

        bool hitSomething = false;

        if (physicsScene.IsValid())
        {
            hitSomething = physicsScene.SphereCast(start, hitRadius, direction, out hit, distance);
        }

        if (!hitSomething)
        {
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

        if (hitObject.CompareTag("Projectile") || hitObject.layer == LayerMask.NameToLayer("IgnoreHits"))
            return;

        if (TryGetComponent<ChainLightning>(out var chain))
        {
            if (chain.OnHit(hit)) return;
        }

        if (TryGetComponent<SplitOnHit>(out var split))
        {
            bounceCount++;
            split.OnHit(hit);
        }

        if (hitObject.CompareTag("Punchable") && hitObject.scene == gameObject.scene)
        {
            if (hitObject.TryGetComponent(out Health health))
            {
                Debug.Log($"🎯 Hit {hitObject.name}, dealing {damage} damage.");
                health.TakeDamage(damage);
            }
        }

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
            if (maxChains > 0 && chainCount < maxChains)
            {
                // Let ChainLightning manage life
            }
            else
            {
                NetworkServer.Destroy(gameObject);
            }
        }

        // Apply all chained effects again (stackable behaviors)
        if (context != null && context.chainedEffects != null)
        {
            foreach (var effect in context.chainedEffects)
            {
                if (effect != null)
                    effect.ApplyEffect(context);
            }
        }
    }

    [ClientRpc]
    private void RpcSyncBounce(Vector3 newPosition, Vector3 newDirection)
    {
        if (isServer) return;
        transform.position = newPosition;
        transform.rotation = Quaternion.LookRotation(newDirection);
    }
}
