using Mirror;
using UnityEngine;

public class ProjectileContext
{
    public GameObject projectileGO;
    public Vector3 direction;
    public NetworkIdentity owner;
    public float chargeMultiplier;

    public void InitializeContext(GameObject projectile, Vector3 dir, NetworkIdentity shooter, float charge)
    {
        projectileGO = projectile;
        direction = dir;
        owner = shooter;
        chargeMultiplier = charge;
    }
}
