using Mirror;
using UnityEngine;
using System;
using System.Collections;

public class Health : NetworkBehaviour
{
    [SyncVar] public int currentHealth;
    public int maxHealth = 100;
    private Color[] originalColors;
    private bool isDead = false;

    public event Action OnDeath;

    private Renderer[] renderers;
    private Material[] materials;
    private Collider[] colliders;
    private int defaultLayer;
    private Coroutine wobbleRoutine;
    private Coroutine flashRoutine;
    private Vector3 originalScale;

    private void Start()
    {
        currentHealth = maxHealth;

        renderers = GetComponentsInChildren<Renderer>();
        materials = new Material[renderers.Length];
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            materials[i] = renderers[i].material;
            if (materials[i].HasProperty("_Color"))
                originalColors[i] = materials[i].color;
        }

        colliders = GetComponentsInChildren<Collider>();
        defaultLayer = gameObject.layer;

        originalScale = transform.localScale; // ✅ Store the actual scale
    }


    [Server]
    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0 || isDead) return;

        currentHealth -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. Remaining health: {currentHealth}");

        RpcPlayHitEffect(); // 👈 Still show hit flash & wobble

        if (currentHealth <= 0)
        {
            isDead = true;
            RpcPlayDeathEffect();
            StartCoroutine(DelayedDestroy());
        }
    }


    [ClientRpc]
    private void RpcPlayHitEffect()
    {
        if (isDead) return;

        if (wobbleRoutine != null)
        {
            StopCoroutine(wobbleRoutine);
            transform.localScale = originalScale; // ✅ Restore original scale, not Vector3.one
        }

        // ✅ Restart flash if it's already running
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            ResetColors();
        }

        flashRoutine = StartCoroutine(HitFlashEffect());
        wobbleRoutine = StartCoroutine(HitWobbleEffect());
    }
    private void ResetColors()
    {
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i].HasProperty("_Color"))
                materials[i].color = originalColors[i];
        }
    }


    private IEnumerator HitFlashEffect()
    {
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i].HasProperty("_Color"))
                materials[i].color = Color.red;
        }

        yield return new WaitForSeconds(0.1f);

        ResetColors();
        flashRoutine = null;
    }


    private IEnumerator HitWobbleEffect()
    {
        Vector3 targetScale = originalScale * 1.25f;
        float duration = 0.2f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
        wobbleRoutine = null;
    }

    [ClientRpc]
    void RpcPlayDeathEffect()
    {
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        gameObject.layer = LayerMask.NameToLayer("IgnoreHits");
        StartCoroutine(DeathEffect());
    }

    private IEnumerator DeathEffect()
    {
        float duration = 0.8f;
        float scaleAmount = 0.8f;
        Vector3 originalScale = transform.localScale;

        foreach (Material mat in materials)
        {
            if (mat.HasProperty("_Color"))
                mat.color = Color.red;
        }

        float time = 0;
        while (time < duration)
        {
            float t = time / duration;
            float scaleFactor = Mathf.Lerp(1f, scaleAmount, t);
            transform.localScale = originalScale * scaleFactor;

            foreach (Material mat in materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.color;
                    color.a = Mathf.Lerp(1, 0, t);
                    mat.color = color;
                }
            }

            time += Time.deltaTime;
            yield return null;
        }

        foreach (Material mat in materials)
        {
            if (mat.HasProperty("_Color"))
            {
                Color color = mat.color;
                color.a = 0;
                mat.color = color;
            }
        }
    }

    [Server]
    private IEnumerator DelayedDestroy()
    {
        EnemyDropTable dropTable = GetComponent<EnemyDropTable>();
        if (dropTable != null)
        {
            dropTable.DropLoot();
        }

        yield return new WaitForSeconds(0.4f);
        OnDeath?.Invoke();
        NetworkServer.Destroy(gameObject);
    }
}
