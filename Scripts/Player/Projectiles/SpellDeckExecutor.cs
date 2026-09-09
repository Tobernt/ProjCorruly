using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpellDeckExecutor
{
    private List<ProjectileEffect> spellDeck;
    private bool shuffle;
    private int nextIndex = 0;

    public List<ProjectileEffect> CurrentDeck => spellDeck;
    public int CurrentIndex => nextIndex;

    public SpellDeckExecutor(List<ProjectileEffect> deck, bool shuffle)
    {
        spellDeck = new List<ProjectileEffect>(deck);
        this.shuffle = shuffle;

        if (shuffle && spellDeck.Count > 0)
            spellDeck.Shuffle();
    }

    public void CastNext(Transform firePoint, Vector3 direction, NetworkIdentity owner)
    {
        if (spellDeck == null || spellDeck.Count == 0)
        {
            Debug.LogWarning("📭 Spell deck is empty.");
            return;
        }

        List<ProjectileEffect> modifiers = new();

        while (nextIndex < spellDeck.Count)
        {
            var effect = spellDeck[nextIndex++];
            if (effect == null) continue;

            if (effect.spellType == ProjectileEffect.SpellType.Modifier)
            {
                if (effect.affectsNext)
                    modifiers.Add(effect);
                else
                {
                    var tempContext = new ProjectileContext();
                    tempContext.InitializeContext(null, direction, owner, 1f);
                    effect.ApplyEffect(tempContext);
                }
                continue;
            }

            if (effect.spellType == ProjectileEffect.SpellType.Projectile)
            {
                GameObject prefab = TryGetPrefabFrom(effect);
                if (prefab == null)
                {
                    Debug.LogWarning($"❌ Cannot cast projectile: prefab missing for {effect.name}");
                    return;
                }

                GameObject projectile = Object.Instantiate(prefab, firePoint.position, Quaternion.LookRotation(direction));
                SceneManager.MoveGameObjectToScene(projectile, firePoint.gameObject.scene);
                NetworkServer.Spawn(projectile);

                var context = new ProjectileContext();
                context.InitializeContext(projectile, direction, owner, 1f);
                context.chainedEffects = modifiers;

                foreach (var mod in modifiers)
                    mod?.ApplyEffect(context);

                effect.ApplyEffect(context);

                if (projectile.TryGetComponent<IProjectile>(out var projScript))
                    projScript.Initialize(direction, 1f, context);

                break;
            }
        }

        if (nextIndex >= spellDeck.Count)
        {
            nextIndex = 0;
            if (shuffle)
                spellDeck.Shuffle();
        }
    }
    public void CastNextFromServer(Vector3 position, Vector3 direction, NetworkIdentity owner, Scene targetScene)
    {
        if (spellDeck == null || spellDeck.Count == 0) return;

        List<ProjectileEffect> modifiers = new();
        while (nextIndex < spellDeck.Count)
        {
            var effect = spellDeck[nextIndex];
            nextIndex++;
            if (effect == null) continue;

            if (effect.spellType == ProjectileEffect.SpellType.Modifier && effect.affectsNext)
            {
                modifiers.Add(effect);
                continue;
            }

            if (effect.spellType == ProjectileEffect.SpellType.Projectile)
            {
                GameObject prefab = TryGetPrefabFrom(effect);
                if (prefab == null)
                {
                    Debug.LogWarning($"❌ SpellDeck: Prefab missing for effect {effect.name}");
                    return;
                }

                GameObject proj = Object.Instantiate(prefab, position, Quaternion.LookRotation(direction));
                SceneManager.MoveGameObjectToScene(proj, targetScene);
                NetworkServer.Spawn(proj);

                var ctx = new ProjectileContext();
                ctx.InitializeContext(proj, direction, owner, 1f);
                ctx.chainedEffects = new List<ProjectileEffect>(modifiers);

                effect.ApplyEffect(ctx);
                foreach (var mod in modifiers)
                    mod.ApplyEffect(ctx);

                if (proj.TryGetComponent<IProjectile>(out var projScript))
                    projScript.Initialize(direction, 1f, ctx);

                break;
            }
        }

        if (nextIndex >= spellDeck.Count)
        {
            nextIndex = 0;
            if (shuffle) spellDeck.Shuffle();
        }
    }

    private GameObject TryGetPrefabFrom(ProjectileEffect effect)
    {
        switch (effect)
        {
            case NormalProjectileEffect normal:
                return normal.projectilePrefab;
            case SplitEffect split:
                return split.projectilePrefab;
            // These are modifiers, not projectile effects
            default:

                return null;
        }
    }
}
