using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemSO : ScriptableObject
{
    public enum FireMode
    {
        SemiAuto,
        Burst,
        FullAuto,
        ChargedShot
    }

    [Header("Firing")]
    public FireMode fireMode = FireMode.SemiAuto;
    public string itemId;
    public string itemName;
    public string itemDescription;
    public Sprite itemIcon;
    public ItemType itemType;
    public bool isStackable;
    public int maxStackSize;
    public GameObject itemPrefab; // ✅ Used for weapons, shields, etc.
    public GameObject effectPrefab; // ✅ NEW: Prefab containing the effect script
    public List<ProjectileEffect> projectileEffects;
    [Header("Pickup")]
    public GameObject pickupPrefab;

    // ✅ Equipment Stats
    public int damage;
    public float attackSpeed;
    public int defense;
    public float reloadSpeed;
    public int magSize;
    public int projectileMultiplier;
    public float healthBonus;
    public float speedBonus;
    public float jumpBonus;

    public RarityType rarity;

    public enum ItemType
    {
        Consumable,
        Component,
        QuestItem,
        Misc,
        Weapon,
        Shield,
        Ring
    }

    public enum RarityType
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}
