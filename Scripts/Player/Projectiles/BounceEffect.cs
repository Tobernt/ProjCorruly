using UnityEngine;

[CreateAssetMenu(fileName = "BounceEffect", menuName = "ProjectileEffects/Bounce")]
public class BounceEffect : ProjectileEffect
{
    public int extraBounces = 1;

    public override void ApplyEffect(ProjectileContext context)
    {
        if (context == null || context.projectileGO == null)
        {
            Debug.LogWarning("⚠️ BounceEffect: Missing context or projectileGO.");
            return;
        }
        if (context.projectileGO.TryGetComponent(out Projectile proj))
        {
            proj.maxBounces += extraBounces;
        }
        else
        {
            Debug.LogWarning("⚠️ BounceEffect: projectileGO has no Projectile component.");
        }
    }
}