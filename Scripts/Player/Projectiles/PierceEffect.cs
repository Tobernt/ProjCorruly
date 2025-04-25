using UnityEngine;

[CreateAssetMenu(menuName = "ProjectileEffects/Pierce")]
public class PierceEffect : ProjectileEffect
{
    public int pierceCount = 1;

    public override void ApplyEffect(ProjectileContext context)
    {
        var pierceComp = context.projectileGO.AddComponent<PierceHandler>();
        pierceComp.remainingPierces = pierceCount;
    }
}
