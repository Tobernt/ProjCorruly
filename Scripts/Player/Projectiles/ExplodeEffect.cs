using UnityEngine;

[CreateAssetMenu(menuName = "ProjectileEffects/Explode")]
public class ExplodeEffect : ProjectileEffect
{
    public float explosionRadius = 3f;
    public int explosionDamage = 40;

    public override void ApplyEffect(ProjectileContext context)
    {
        var explodeComp = context.projectileGO.AddComponent<ExplodeOnHit>();
        explodeComp.radius = explosionRadius;
        explodeComp.damage = explosionDamage;
    }
}
