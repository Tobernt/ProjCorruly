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
        respawnHandler = GetComponent<PlayerRespawnHandler>();
    }

    public override void OnStartServer()
    {
        currentHealth = maxHealth;
    }

    public override void OnStartClient()
    {
        if (!isLocalPlayer) return;
        StartCoroutine(WaitForHUD());
    }

    private IEnumerator WaitForHUD()
    {
        // Wait until HUD is parented to the player
        while (healthBar == null)
        {
            healthBar = GetComponentInChildren<HealthBarUI>(true); // Include inactive
            if (healthBar != null)
            {
                Debug.Log("[PlayerHealth] ✅ Found HealthBarUI on player.");
                healthBar.healthText?.gameObject.SetActive(true);
                healthBar.SetHealth(currentHealth, maxHealth);
                break;
            }
            yield return null;
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

        currentHealth -= amount;

        if (connectionToClient != null)
            TargetUpdateHealth(connectionToClient, currentHealth, maxHealth);

        if (currentHealth <= 0)
            respawnHandler?.HandleDeathWithDeathEffect();
    }

    private bool ArePlayersInSameParty(GameObject otherPlayer) => false;

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        if (!isLocalPlayer) return;

        if (healthBar != null)
            healthBar.SetHealth(newHealth, maxHealth);
    }

    [ClientRpc]
    public void RpcTriggerDeathEffect()
    {
        Debug.Log("[PlayerHealth] Death visual triggered.");
    }

    [Server]
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        if (connectionToClient != null)
            TargetUpdateHealth(connectionToClient, currentHealth, maxHealth);
    }

    [Server]
    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
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
