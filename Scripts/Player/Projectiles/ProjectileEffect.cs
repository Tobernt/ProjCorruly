using System.Collections.Generic;
using UnityEngine;

public abstract class ProjectileEffect : ScriptableObject
{
    [Header("Chained Effects (optional)")]
    public List<ProjectileEffect> nextEffects;
    public SpellType spellType = SpellType.Projectile;
    public bool affectsNext = true; // If true, modify spell(s) after it

    public abstract void ApplyEffect(ProjectileContext context);
    public enum SpellType
    {
        Modifier,
        Projectile
    }

    protected void TriggerNextEffects(ProjectileContext context)
    {
        if (nextEffects == null) return;

        foreach (var effect in nextEffects)
        {
            if (effect != null)
                effect.ApplyEffect(context);
        }
    }
}
