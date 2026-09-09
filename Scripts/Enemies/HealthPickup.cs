using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class HealthPickup : NetworkBehaviour
{
    [Header("Healing Settings")]
    public int healAmount = 25;

    [Header("Pickup FX (Optional, Must Have NetworkIdentity + NetworkedSelfDestruct)")]
    public GameObject pickupEffect;

    [Header("Bounce Settings")]
    public float bounceForce = 5f;
    public float randomTorque = 30f;

    private Rigidbody rb;
    private bool hasLanded = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.isKinematic = false;
    }

    private void Start()
    {
        if (isServer)
        {
            SceneManager.MoveGameObjectToScene(gameObject, gameObject.scene);
            rb.velocity = Vector3.up * bounceForce;
            rb.AddTorque(Random.insideUnitSphere * randomTorque);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isServer || hasLanded) return;

        if (collision.gameObject.CompareTag("Ground") || collision.contacts[0].normal.y > 0.5f)
        {
            hasLanded = true;
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null && health.currentHealth < health.maxHealth)
            {
                health.Heal(healAmount);
                SpawnPickupEffect(); // Local server-side spawn
                NetworkServer.Destroy(gameObject);
            }
        }
    }

    private void SpawnPickupEffect()
    {
        if (pickupEffect == null) return;

        GameObject effect = Instantiate(pickupEffect, transform.position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(effect, gameObject.scene);
        NetworkServer.Spawn(effect); // Sync across all clients

        // The prefab must auto-destroy itself using a script like NetworkedSelfDestruct
    }
}
