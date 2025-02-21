using System.Collections.Generic;
using UnityEngine;

public class CharacterLoader : MonoBehaviour
{
    private void Awake()
    {
        if (CharacterData.Current == null)
        {
            Debug.LogError("❌ No character data loaded in memory! Ensure a character is selected before entering the game.");
            return;
        }

        Debug.Log($"✅ Character {CharacterData.Current.Name} is now active in the scene.");

        // ✅ Ensure inventory is initialized
        if (CharacterData.Current.Inventory == null || CharacterData.Current.Inventory.Count == 0)
        {
            Debug.LogWarning("⚠ Inventory was null or empty. Initializing new inventory.");
            CharacterData.Current.Inventory = new List<InventorySlot>();
            for (int i = 0; i < 40; i++)
                CharacterData.Current.Inventory.Add(new InventorySlot());
        }

        // ✅ Ensure equipment slots are initialized
        InitializeEquipment();

        // ✅ Wait for UI components to exist before initializing
        Invoke(nameof(InitializeInventoryUI), 0.1f);
        Invoke(nameof(InitializeEquipmentUI), 0.2f); // Delayed slightly to avoid race conditions
    }

    private void InitializeInventoryUI()
    {
        InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.InitializeInventory(CharacterData.Current.Inventory.Count);
            inventoryUI.RefreshUI();
            Debug.Log("✅ Inventory UI successfully initialized.");
        }
        else
        {
            Debug.LogError("❌ Inventory UI not found in the scene.");
        }
    }

    private void InitializeEquipmentUI()
    {
        EquipmentUI equipmentUI = FindObjectOfType<EquipmentUI>();
        if (equipmentUI != null)
        {
            equipmentUI.RefreshUI(); // ✅ Refresh Equipment UI after game loads
            Debug.Log("✅ Equipment UI successfully initialized.");
        }
        else
        {
            Debug.LogError("❌ Equipment UI not found in the scene.");
        }
    }

    private void InitializeEquipment()
    {
        var character = CharacterData.Current;

        if (character.MainHand == null) character.MainHand = new EquipmentSlot(ItemSO.ItemType.Weapon);
        if (character.Offhand == null) character.Offhand = new EquipmentSlot(ItemSO.ItemType.Shield);

        for (int i = 0; i < character.Rings.Length; i++)
        {
            if (character.Rings[i] == null)
                character.Rings[i] = new EquipmentSlot(ItemSO.ItemType.Ring);
        }

        Debug.Log("✅ Equipment slots initialized.");
    }
}
