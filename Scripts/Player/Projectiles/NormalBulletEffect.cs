using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "NormalProjectile", menuName = "ProjectileEffects/NormalProjectile")]
public class NormalProjectileEffect : ProjectileEffect
{
    [Header("Prefab")]
    public GameObject projectilePrefab;

    [Header("Base Stats")]
    public int damage = 20;
    public float speed = 10f;
    public float lifetime = 5f;
    public int maxBounces = 0;

    public override void ApplyEffect(ProjectileContext context)
    {
        if (context == null || context.projectileGO != null)
        {
            Debug.LogWarning("❌ NormalProjectileEffect expects projectileGO to be null (it spawns it itself).");
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.LogWarning("❌ NormalProjectileEffect: projectilePrefab is missing.");
            return;
        }

        // Spawn and assign
        GameObject go = Instantiate(projectilePrefab, context.owner.transform.position + context.direction * 0.5f, Quaternion.LookRotation(context.direction));
        SceneManager.MoveGameObjectToScene(go, context.owner.gameObject.scene);
        NetworkServer.Spawn(go);

        context.projectileGO = go;

        // Apply runtime properties
        if (go.TryGetComponent(out Projectile proj))
        {
            proj.damage = damage;
            proj.speed = speed;
            proj.lifetime = lifetime;
            proj.maxBounces = maxBounces;
            proj.Initialize(context.direction, context.chargeMultiplier, context);
        }
        else
        {
            Debug.LogWarning("❌ NormalProjectileEffect: Spawned object is missing a Projectile component.");
        }
    }
}
