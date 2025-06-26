using UnityEngine;
using Mirror;
using CustomNamespace;

[CreateAssetMenu(menuName = "ProjectileEffects/RecoilKnockbackEffect")]
public class RecoilKnockbackEffect : ProjectileEffect
{
    [Tooltip("Force applied to the shooter in the opposite direction of the projectile.")]
    public float knockbackForce = 10f;

    public override void ApplyEffect(ProjectileContext context)
    {
        if (context == null || context.owner == null || context.direction == Vector3.zero)
        {
            Debug.LogWarning("❌ RecoilKnockbackEffect: Projectile context missing or invalid.");
            return;
        }

        if (context.recoilApplied)
        {
            Debug.Log("⏭ RecoilKnockbackEffect already applied.");
            return;
        }

        context.recoilApplied = true;

        if (context.owner.TryGetComponent(out CustomPlayerController controller))
        {
            Vector3 knockbackDir = -context.direction.normalized;
            controller.TargetApplyKnockback(controller.connectionToClient, knockbackDir, knockbackForce);
        }
        else
        {
            Debug.LogWarning("❌ RecoilKnockbackEffect: Owner has no CustomPlayerController.");
        }
    }
}
