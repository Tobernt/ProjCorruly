using UnityEngine;

[CreateAssetMenu(fileName = "SplitEffect", menuName = "ProjectileEffects/Split")]
public class SplitEffect : ProjectileEffect
{
    public GameObject projectilePrefab;
    public int splitCount = 3;
    public float spreadAngle = 30f;
    public int bonusBounces = 0;
    public bool useChaoticSpread = false;

    public override void ApplyEffect(ProjectileContext context)
    {
        if (context == null || context.projectileGO == null)
        {
            Debug.LogWarning("⚠️ SplitEffect: Missing context or projectileGO.");
            return;
        }

        if (!context.projectileGO.TryGetComponent(out SplitOnHit splitComp))
        {
            splitComp = context.projectileGO.AddComponent<SplitOnHit>();
        }
        splitComp.projectilePrefab = projectilePrefab;
        splitComp.splitCount = splitCount;
        splitComp.spreadAngle = spreadAngle;
        splitComp.useChaoticSpread = useChaoticSpread;
        if (context.projectileGO.TryGetComponent(out Projectile p))
        {
            p.maxBounces += bonusBounces;
            splitComp.maxSplitDepth = p.maxBounces;
        }
    }
}