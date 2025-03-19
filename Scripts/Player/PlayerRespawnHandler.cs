using System.Collections;
using Mirror;
using UnityEngine;
using TMPro;

public class PlayerRespawnHandler : NetworkBehaviour
{
    private Vector3 deathPosition;
    private Renderer[] renderers;
    private Collider[] colliders;
    private Rigidbody rb;
    private PlayerHealth playerHealth;

    [Header("Respawn Settings")]
    public float respawnDelay = 5f;
    private Vector3 spawnPosition;
    private string originalTag;
    [Header("Respawn UI")]
    public TextMeshProUGUI respawnText;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();

        spawnPosition = transform.position;
        originalTag = gameObject.tag;
    }


    [Server]
    public void HandleDeathWithDeathEffect()
    {
        deathPosition = transform.position;
        gameObject.tag = "Dead"; // 🪦 Enemies can ignore this
        RpcPlayPlayerDeath();
        StartCoroutine(RespawnAfterDelay(respawnDelay + 0.8f));
    }


    [ClientRpc]
    private void RpcPlayPlayerDeath()
    {
        gameObject.tag = "Dead"; // 👈 Tag change must happen client-side
        StartCoroutine(DeathVisualSequence());
    }

    private IEnumerator DeathVisualSequence()
    {
        playerHealth?.RpcTriggerDeathEffect();

        yield return new WaitForSeconds(0.8f);

        DisablePlayer();
    }

    private void DisablePlayer()
    {
        foreach (var r in renderers)
            r.enabled = false;

        foreach (var c in colliders)
            c.enabled = false;
    }

    [Server]
    private IEnumerator RespawnAfterDelay(float totalDelay)
    {
        float countdown = totalDelay;
        while (countdown > 0f)
        {
            TargetUpdateRespawnCountdown(connectionToClient, Mathf.CeilToInt(countdown));
            yield return new WaitForSeconds(1f);
            countdown -= 1f;
        }

        TargetRespawnPlayer(connectionToClient, deathPosition);

        playerHealth.ResetHealth();

        gameObject.tag = "Player"; // ✅ Server-side tag fix

        RpcEnablePlayer();
    }


    [ClientRpc]
    private void RpcEnablePlayer()
    {
        foreach (var r in renderers)
            r.enabled = true;

        foreach (var c in colliders)
            c.enabled = true;
    }
    private IEnumerator HideRespawnTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (respawnText != null)
            respawnText.gameObject.SetActive(false);
    }

    [TargetRpc]
    private void TargetRespawnPlayer(NetworkConnection target, Vector3 positionFromServer)
    {
        transform.position = spawnPosition;
        gameObject.tag = "Player";

        if (respawnText != null)
        {
            respawnText.text = "Respawning...";
            StartCoroutine(HideRespawnTextAfterDelay(1f));
        }

        Debug.Log("✅ Player has respawned.");
    }

    [TargetRpc]
    private void TargetUpdateRespawnCountdown(NetworkConnection target, int secondsLeft)
    {
        if (respawnText != null)
        {
            respawnText.gameObject.SetActive(true);
            respawnText.text = $"Respawning in {secondsLeft}...";
        }
    }
}
