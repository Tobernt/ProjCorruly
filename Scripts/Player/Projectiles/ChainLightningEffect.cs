using UnityEngine;

[CreateAssetMenu(menuName = "ProjectileEffects/Chain Lightning")]
public class ChainLightningEffect : ProjectileEffect
{
    public int chainCount = 3;
    public float chainRadius = 6f;

    public override void ApplyEffect(ProjectileContext context)
    {
        if (context == null || context.projectileGO == null)
        {
            Debug.LogWarning("⚠️ ChainLightningEffect: Missing context or projectileGO.");
            return;
        }
        if (!context.projectileGO.TryGetComponent(out ChainLightning chain))
        {
            chain = context.projectileGO.AddComponent<ChainLightning>();
        }
        chain.maxChains = chainCount;
        chain.chainRadius = chainRadius;
        if (context.projectileGO.TryGetComponent(out Projectile p))
        {
            chain.damage = p.damage;   // Sync damage
            p.maxChains = chainCount;
            p.chainCount = 0;
        }
    }
}