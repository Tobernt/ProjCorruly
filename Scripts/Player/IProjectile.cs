using UnityEngine;

public interface IProjectile
{
    void Initialize(Vector3 direction, float chargeMultiplier = 1f, ProjectileContext context = null);
}
