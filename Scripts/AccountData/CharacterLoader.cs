using System.Collections.Generic;
using UnityEngine;
using Mirror;
using CustomNamespace;

public class CharacterLoader : MonoBehaviour
{
    private void Start()
    {
        if (CharacterData.Current == null)
        {
            Debug.LogError("❌ No character data loaded in memory! Ensure a character is selected before entering the game.");
            return;
        }

        Debug.Log($"✅ Character {CharacterData.Current.Name} is now active in the scene.");
        Debug.Log($"📍 Expected scene to spawn in: {CharacterData.Current.LastSceneName}");

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != CharacterData.Current.LastSceneName)
        {
            Debug.LogWarning("⚠️ Current scene does not match saved scene — consider triggering scene load.");
        }

        if (CharacterData.Current.Inventory == null || CharacterData.Current.Inventory.Count == 0)
        {
            Debug.LogWarning("⚠ Inventory was null or empty. Initializing new inventory.");
            CharacterData.Current.Inventory = new List<InventorySlot>();
            for (int i = 0; i < 40; i++)
                CharacterData.Current.Inventory.Add(new InventorySlot());
        }

        InitializeEquipment();

        if (CharacterData.Current.HotbarLayout == null || CharacterData.Current.HotbarLayout.Count != 9)
        {
            CharacterData.Current.HotbarLayout = new List<int> { -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        }

        // Defer hotbar data injection until the player is spawned and has components assigned
        Invoke(nameof(InitializeHotbarFromCharacterData), 0.3f);
        Invoke(nameof(InitializeInventoryUI), 0.1f);
        Invoke(nameof(InitializeEquipmentUI), 0.2f);
    }

    private void InitializeHotbarFromCharacterData()
    {
        var player = NetworkClient.connection?.identity?.GetComponent<CustomPlayerController>();
        if (player != null && player.hotbar != null)
        {
            for (int i = 0; i < CharacterData.Current.HotbarLayout.Count; i++)
            {
                player.hotbar.hotbarIndices[i] = CharacterData.Current.HotbarLayout[i];
            }

            player.hotbarUI?.RefreshUI();
            Debug.Log("✅ Hotbar initialized from CharacterData.");
        }
        else
        {
            Debug.LogWarning("⚠ Hotbar or Player not found when trying to apply CharacterData hotbar layout.");
        }
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
            equipmentUI.RefreshUI();
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
