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

    [Header("HUD Prefab")]
    public GameObject respawnUIPrefabRoot; // Assign PlayerHUD instance to this

    [Header("Respawn Settings")]
    public float respawnDelay = 5f;
    private Vector3 spawnPosition;
    private string originalTag;

    [Header("Respawn UI (Auto-Assigned)")]
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

    public override void OnStartLocalPlayer()
    {
        StartCoroutine(WaitForRespawnText());
    }

    private IEnumerator WaitForRespawnText()
    {
        while (respawnText == null)
        {
            if (respawnUIPrefabRoot != null && respawnUIPrefabRoot.TryGetComponent(out HUDInitializer hud))
            {
                respawnText = hud.respawnText;
                if (respawnText != null)
                {
                    respawnText.gameObject.SetActive(false);
                    Debug.Log("[RespawnHandler] ✅ Found respawnText via HUDInitializer.");
                    break;
                }
            }
            else if (respawnUIPrefabRoot != null)
            {
                // Fallback: find by name
                Transform found = respawnUIPrefabRoot.transform.Find("RespawnText");
                if (found && found.TryGetComponent(out TextMeshProUGUI fallbackText))
                {
                    respawnText = fallbackText;
                    respawnText.gameObject.SetActive(false);
                    Debug.Log("[RespawnHandler] ⚠️ Found respawnText by fallback lookup.");
                    break;
                }
            }

            yield return null;
        }

        if (respawnText == null)
            Debug.LogError("[RespawnHandler] ❌ Failed to find respawnText!");
    }

    [Server]
    public void HandleDeathWithDeathEffect()
    {
        deathPosition = transform.position;
        gameObject.tag = "Dead";
        RpcPlayPlayerDeath();
        StartCoroutine(RespawnAfterDelay(respawnDelay + 0.8f));
    }

    [ClientRpc]
    private void RpcPlayPlayerDeath()
    {
        gameObject.tag = "Dead";
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
        gameObject.tag = "Player";
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
