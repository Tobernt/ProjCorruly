using UnityEngine;

[CreateAssetMenu(menuName = "ProjectileEffects/Pierce")]
public class PierceEffect : ProjectileEffect
{
    public int pierceCount = 1;

    public override void ApplyEffect(ProjectileContext context)
    {
        if (context == null || context.projectileGO == null)
        {
            Debug.LogWarning("⚠️ PierceEffect: Missing context or projectileGO.");
            return;
        }
        var pierceComp = context.projectileGO.AddComponent<PierceHandler>();
        pierceComp.remainingPierces = pierceCount;
    }
}