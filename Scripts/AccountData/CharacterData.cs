using CustomNamespace;
using Mirror;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class CharacterData
{
    public string Name;
    public int Level;
    public float Health;
    public string LastSceneName = "MainScene";

    public List<InventorySlot> Inventory;
    public EquipmentSlot MainHand, Offhand;
    public EquipmentSlot[] Rings = new EquipmentSlot[5];
    public List<int> HotbarLayout = new List<int> { -1, -1, -1, -1, -1, -1, -1, -1, -1 };

    public static CharacterData Current { get; set; }

    private static string savePath => Path.Combine(Application.persistentDataPath, "Characters");

    public CharacterData(string name, int level, float health)
    {
        Name = name;
        Level = level;
        Health = health;

        Inventory = new List<InventorySlot>();
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

        var player = NetworkClient.connection?.identity?.GetComponent<CustomPlayerController>();
        if (player != null && player.hotbar != null)
            HotbarLayout = new List<int>(player.hotbar.hotbarIndices);

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

            Debug.Log($"✅ Character {characterName} loaded! Inventory Size: {Current.Inventory.Count}");

            Current.ApplyEquippedEffects();

            return Current;
        }

        Debug.LogError($"❌ Character file not found: {filePath}");
        return null;
    }

    public void ApplyEquippedEffects()
    {
        Debug.Log($"🔄 Applying equipped item effects...");

        foreach (var ringSlot in Rings)
        {
            if (ringSlot != null && ringSlot.IsOccupied)
                ApplyItemEffect(ringSlot.ItemID);
        }

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
