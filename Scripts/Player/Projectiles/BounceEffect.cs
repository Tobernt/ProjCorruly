using UnityEngine;

[CreateAssetMenu(fileName = "BounceEffect", menuName = "ProjectileEffects/Bounce")]
public class BounceEffect : ProjectileEffect
{
    public int extraBounces = 1;

    public override void ApplyEffect(ProjectileContext context)
    {
        if (context.projectileGO.TryGetComponent(out Projectile proj))
        {
            proj.maxBounces += extraBounces;
            Debug.Log($"🌀 BounceEffect applied: +{extraBounces} bounces");
        }
    }
}
