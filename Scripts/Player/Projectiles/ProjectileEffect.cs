using UnityEngine;

public abstract class ProjectileEffect : ScriptableObject
{
    public abstract void ApplyEffect(ProjectileContext context);
}
