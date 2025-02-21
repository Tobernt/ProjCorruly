using Mirror;
using UnityEngine;

public class Health : NetworkBehaviour
{
    [SyncVar] public int currentHealth;
    public int maxHealth = 100;

    void Start()
    {
        currentHealth = maxHealth;
    }

    [Server]
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. Remaining health: {currentHealth}");

        if (currentHealth <= 0)
        {
            RpcDie();
        }
    }

    [Server]
    void RpcDie()
    {
        Debug.Log($"{gameObject.name} has died!");

        // Handle death logic here (e.g., trigger ragdoll or destroy object)
        Rigidbody[] ragdollBodies = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in ragdollBodies)
        {
            rb.isKinematic = false; // Enable ragdoll
        }

        // Optionally disable main scripts or collider
        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null)
        {
            mainCollider.enabled = false;
        }

        Destroy(this); // Remove the health script
    }
}
