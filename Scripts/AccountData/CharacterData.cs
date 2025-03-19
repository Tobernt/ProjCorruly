using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class CharacterData
{
    public string Name;
    public int Level;
    public float Health;

    public List<InventorySlot> Inventory; // ✅ Now handles inventory slots properly
    public EquipmentSlot MainHand, Offhand;
    public EquipmentSlot[] Rings = new EquipmentSlot[5];
    public static CharacterData Current { get; set; } // ✅ Now settable from anywhere

    private static string savePath => Path.Combine(Application.persistentDataPath, "Characters");

    public CharacterData(string name, int level, float health)
    {
        this.Name = name;
        this.Level = level;
        this.Health = health;

        // ✅ Ensure Inventory is always initialized
        this.Inventory = new List<InventorySlot>();
        for (int i = 0; i < 40; i++)
            Inventory.Add(new InventorySlot());

        for (int i = 0; i < Rings.Length; i++)
            Rings[i] = new EquipmentSlot(ItemSO.ItemType.Ring);

        MainHand = new EquipmentSlot(ItemSO.ItemType.Weapon);
        Offhand = new EquipmentSlot(ItemSO.ItemType.Shield);
    }
    public static List<CharacterData> GetAllCharacters()
    {
        List<CharacterData> characters = new List<CharacterData>();
        if (Directory.Exists(savePath))
        {
            foreach (string file in Directory.GetFiles(savePath, "*.json"))
            {
                string json = File.ReadAllText(file);
                CharacterData character = JsonUtility.FromJson<CharacterData>(json);
                characters.Add(character);
            }
        }
        return characters;
    }
    public static void DeleteCharacter(string characterName)
    {
        string filePath = Path.Combine(savePath, $"{characterName}.json");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"🗑️ Character {characterName} deleted!");
        }
    }

    public void Save()
    {
        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);

        string json = JsonUtility.ToJson(this, true);
        File.WriteAllText(Path.Combine(savePath, $"{Name}.json"), json);

        Debug.Log($"💾 Character {Name} saved! Inventory Size: {Inventory.Count}, Equipment Saved.");
    }

    public static CharacterData Load(string characterName)
    {
        string filePath = Path.Combine(savePath, $"{characterName}.json");

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            Current = JsonUtility.FromJson<CharacterData>(json);

            Debug.Log($"✅ Character {characterName} loaded! Inventory Size: {Current.Inventory.Count}, Equipment Loaded.");

            // ✅ Apply all equipped ring effects
            Current.ApplyEquippedEffects();

            return Current;
        }

        Debug.LogError($"❌ Character file not found: {filePath}");
        return null;
    }
    public void ApplyEquippedEffects()
    {
        Debug.Log($"🔄 Applying equipped item effects...");

        // ✅ Apply effects for all rings
        foreach (var ringSlot in Rings)
        {
            if (ringSlot != null && ringSlot.IsOccupied)
            {
                ApplyItemEffect(ringSlot.ItemID);
            }
        }

        // ✅ Apply effects for weapon & shield
        if (MainHand.IsOccupied)
            ApplyItemEffect(MainHand.ItemID);

        if (Offhand.IsOccupied)
            ApplyItemEffect(Offhand.ItemID);
    }

    private void ApplyItemEffect(string itemId)
    {
        ItemSO item = ItemDatabaseSO.Instance.GetItemById(itemId);
        if (item == null)
        {
            Debug.LogError($"❌ Item with ID {itemId} not found in database.");
            return;
        }

        Debug.Log($"✨ Applying effect of {item.itemName}...");

        // ✅ Apply item-specific bonuses (example: speed, health, etc.)
        PlayerEquipmentTracker.Instance?.EquipItem(item.itemType.ToString(), itemId);
    }

    public EquipmentSlot GetEquipmentSlotByIndex(int index)
    {
        return index switch
        {
            0 => MainHand,
            1 => Offhand,
            _ => (index >= 2 && index < 6) ? Rings[index - 2] : null
        };
    }
}
