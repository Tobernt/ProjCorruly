using UnityEngine;
using System;
using UnityEditor;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemSO : ScriptableObject
{
    public string itemId;
    public string itemName;
    public string itemDescription;
    public Sprite itemIcon;
    public ItemType itemType;
    public bool isStackable;
    public int maxStackSize;
    public GameObject itemPrefab;

    // ✅ Equipment Stats
    public int damage;
    public float attackSpeed;
    public int defense;
    public int reloadSpeed;
    public int magSize;
    public float healthBonus;
    public float speedBonus;
    public float jumpBonus;

    public RarityType rarity;

    // ✅ Store script as a **string** instead of Type or MonoScript
    public MonoScript itemScript;

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
