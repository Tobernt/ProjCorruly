using UnityEngine;

[CreateAssetMenu(menuName = "ProjectileEffects/Wall On Hit")]
public class WallOnHitEffect : ProjectileEffect
{
    public GameObject wallPrefab;

    public override void ApplyEffect(ProjectileContext context)
    {
        if (context == null || context.projectileGO == null)
        {
            Debug.LogWarning("⚠️ WallOnHitEffect: Missing context or projectileGO.");
            return;
        }
        var spawner = context.projectileGO.AddComponent<WallSpawnerOnHit>();
        spawner.wallPrefab = wallPrefab;
    }
}