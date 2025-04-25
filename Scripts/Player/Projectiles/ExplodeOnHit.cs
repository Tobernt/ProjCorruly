using Mirror;
using UnityEngine;

public class ExplodeOnHit : NetworkBehaviour
{
    public float radius = 3f;
    public int damage = 40;

    private void OnDestroy()
    {
        if (!isServer) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Punchable"))
            {
                var health = hit.GetComponent<Health>();
                if (health != null)
                {
                    health.TakeDamage(damage);
                }
            }
        }
    }
}
