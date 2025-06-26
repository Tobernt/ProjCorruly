using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileContext
{
    public GameObject projectileGO;
    public Vector3 direction;
    public NetworkIdentity owner;
    public float chargeMultiplier;
    public List<ProjectileEffect> chainedEffects;
    public bool recoilApplied = false;
    public void InitializeContext(GameObject projectile, Vector3 dir, NetworkIdentity shooter, float charge)
    {
        projectileGO = projectile;
        direction = dir;
        owner = shooter;
        chargeMultiplier = charge;
        recoilApplied = false;
        // Start with empty list of chained effects by default
        chainedEffects = new List<ProjectileEffect>();
    }
}
