using Mirror;
using UnityEngine;
using System.Collections;

public class PlayerHealth : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int currentHealth;

    public int maxHealth = 100;
    public bool canBeDamagedByPlayers = true;
    public bool friendlyFireEnabled = false;

    private HealthBarUI healthBar;
    private PlayerRespawnHandler respawnHandler;

    private void Awake()
    {
        healthBar = GetComponentInChildren<HealthBarUI>();
        respawnHandler = GetComponent<PlayerRespawnHandler>();

        if (healthBar == null)
            Debug.LogWarning("[PlayerHealth] No HealthBarUI assigned or found in children!");
    }

    public override void OnStartServer()
    {
        currentHealth = maxHealth;
    }

    public override void OnStartClient()
    {
        if (!isLocalPlayer) return;

        if (healthBar != null && healthBar.healthText != null)
        {
            healthBar.healthText.gameObject.SetActive(true); // 👈 Enable UI for local player only
            healthBar.SetHealth(currentHealth, maxHealth);
        }
    }



    [Server]
    public void TakeDamage(int amount, GameObject attacker = null)
    {
        if (currentHealth <= 0) return;

        if (attacker != null && attacker.CompareTag("Player"))
        {
            if (!canBeDamagedByPlayers) return;
            if (!friendlyFireEnabled && ArePlayersInSameParty(attacker)) return;
        }

        int oldHealth = currentHealth;
        currentHealth -= amount;

        // ✅ Manually call hook on local player if they own this object
        if (connectionToClient != null)
            TargetUpdateHealth(connectionToClient, currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            //RpcTriggerDeathEffect();
            respawnHandler?.HandleDeathWithDeathEffect();
        }
    }

    private bool ArePlayersInSameParty(GameObject otherPlayer)
    {
        // Placeholder for party system
        return false;
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        if (!isLocalPlayer) return; // 👈 Only update UI locally

        if (healthBar != null)
            healthBar.SetHealth(newHealth, maxHealth);
    }

    [ClientRpc]
    public void RpcTriggerDeathEffect()
    {
        Debug.Log("[PlayerHealth] Death visual triggered.");
        // Hook in visual effects here if needed
    }

    [Server]
    public void ResetHealth()
    {
        currentHealth = maxHealth;

        // ✅ Tell the owning client to update its UI manually
        if (connectionToClient != null)
            TargetUpdateHealth(connectionToClient, currentHealth, maxHealth);
    }
    [Server]
    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

        // ✅ Ensure UI is updated on the client
        if (connectionToClient != null)
            TargetUpdateHealth(connectionToClient, currentHealth, maxHealth);
    }


    [TargetRpc]
    private void TargetUpdateHealth(NetworkConnection target, int newHealth, int maxHealth)
    {
        if (healthBar != null)
            healthBar.SetHealth(newHealth, maxHealth);
    }
}
