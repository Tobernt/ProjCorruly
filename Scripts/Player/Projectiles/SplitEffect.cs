using UnityEngine;

public class SplitEffect : ProjectileEffect
{
    public GameObject projectilePrefab;
    public int splitCount = 3;
    public float spreadAngle = 30f;
    public int bonusBounces = 0;
    public bool useChaoticSpread = false; // ✅ Toggle in the editor

    public override void ApplyEffect(ProjectileContext context)
    {
        if (!context.projectileGO.TryGetComponent(out SplitOnHit splitComponent))
        {
            splitComponent = context.projectileGO.AddComponent<SplitOnHit>();
        }

        splitComponent.projectilePrefab = projectilePrefab;
        splitComponent.splitCount = splitCount;
        splitComponent.spreadAngle = spreadAngle;
        splitComponent.useChaoticSpread = useChaoticSpread;

        if (context.projectileGO.TryGetComponent(out Projectile p))
        {
            p.maxBounces += bonusBounces;
            splitComponent.maxSplitDepth = p.maxBounces;
        }
    }
}
