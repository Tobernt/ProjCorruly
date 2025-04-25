
using UnityEngine;

[CreateAssetMenu(menuName = "ProjectileEffects/Wall On Hit")]
public class WallOnHitEffect : ProjectileEffect
{
    public GameObject wallPrefab;

    public override void ApplyEffect(ProjectileContext context)
    {
        var waller = context.projectileGO.AddComponent<WallSpawnerOnHit>();
        waller.wallPrefab = wallPrefab;
    }
}
