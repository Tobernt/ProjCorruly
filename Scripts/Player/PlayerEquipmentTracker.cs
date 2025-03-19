using System.Collections.Generic;
using UnityEngine;

public class PlayerEquipmentTracker : MonoBehaviour
{
    public static PlayerEquipmentTracker Instance { get; private set; }

    // Base Stats
    private float baseHealth = 100f;
    private float baseSpeed = 5f;
    private float baseJump = 3f;

    // Final Stats
    public float Health { get; private set; }
    public float Speed { get; private set; }
    public float Jump { get; private set; }

    // Running Multiplier
    private float runMultiplier = 1f;

    // Ring slots (each ring can have its own effect)
    private string[] ringSlots = new string[5]; // Stores ring itemIDs
    private Dictionary<int, MonoBehaviour> ringEffects = new Dictionary<int, MonoBehaviour>(); // Maps slot to effect
    private void Awake()
    {
        if (Instance == null) Instance = this;

        ResetStats(); // ✅ Ensure stats are reset before applying bonuses
        LoadEquippedRings(); // ✅ Ensure equipped rings are applied on load
    }
    public string GetEquippedWeaponID()
    {
        if (CharacterData.Current == null || CharacterData.Current.MainHand == null)
            return null;

        // ✅ If unequipped, set the ID to null
        if (string.IsNullOrEmpty(CharacterData.Current.MainHand.ItemID))
            return null;

        return CharacterData.Current.MainHand.ItemID;
    }


    // ✅ Reset stats to base values
    private void ResetStats()
    {
        Health = baseHealth;
        Speed = baseSpeed;
        Jump = baseJump;
        runMultiplier = 1f;
    }

    public void EquipItem(string slot, string itemId)
    {
        Debug.Log($"🛠 EquipItem called for slot: {slot} with item: {itemId}");

        if (slot.StartsWith("Ring"))
        {
            EquipRing(itemId);
            return;
        }

        // ✅ Convert UI Slot Names to CharacterData Slot Names
        string characterSlot = slot switch
        {
            "Weapon" => "MainHand",   // UI "Weapon" corresponds to CharacterData "MainHand"
            "Shield" => "Offhand",    // UI "Shield" corresponds to CharacterData "Offhand"
            _ => null
        };

        if (characterSlot == null)
        {
            Debug.LogError($"❌ Invalid slot name: {slot} (Only Weapon and Shield are valid in UI)");
            return;
        }

        // ✅ Check if the slot is already occupied
        string currentlyEquipped = (characterSlot == "MainHand")
            ? CharacterData.Current.MainHand.ItemID
            : CharacterData.Current.Offhand.ItemID;

        if (!string.IsNullOrEmpty(currentlyEquipped))
        {
            Debug.Log($"🔄 {slot} is already occupied with {currentlyEquipped}. Unequipping first...");
            UnequipItem(slot);
        }

        // ✅ Equip the new item
        if (characterSlot == "MainHand")
        {
            CharacterData.Current.MainHand.ItemID = itemId;
            Debug.Log($"✅ {slot} now equipped with {itemId}");
        }
        else if (characterSlot == "Offhand")
        {
            CharacterData.Current.Offhand.ItemID = itemId;
            Debug.Log($"✅ {slot} now equipped with {itemId}");
        }

        // ✅ Save CharacterData after equipping
        CharacterData.Current.Save();

        // ✅ Update WeaponController immediately after equipping
        Debug.Log("🔄 Updating WeaponController after equip...");
        FindObjectOfType<WeaponController>()?.UpdateWeapon();

        // ✅ Refresh UI after changes
        InventoryUI.Instance.RefreshUI();
        EquipmentUI.Instance.RefreshUI();
    }

    public GameObject GetEquippedWeaponGameObject()
    {
        string weaponID = GetEquippedWeaponID();
        if (string.IsNullOrEmpty(weaponID)) return null;

        foreach (Transform child in transform) // ✅ Searches equipped items
        {
            if (child.CompareTag("Weapon")) // ✅ Ensure weapon prefabs have "Weapon" tag
            {
                return child.gameObject;
            }
        }

        return null; // ✅ No equipped weapon found
    }

    public void UnequipItem(string slot)
    {
        Debug.Log($"🔄 UnequipItem called for slot: {slot}");

        if (slot.StartsWith("Ring"))
        {
            int ringIndex = GetRingIndex(slot);
            UnequipRing(ringIndex);
            return;
        }

        // ✅ Convert UI Slot Names to CharacterData Slot Names
        string characterSlot = slot switch
        {
            "Weapon" => "MainHand",   // UI "Weapon" corresponds to CharacterData "MainHand"
            "Shield" => "Offhand",    // UI "Shield" corresponds to CharacterData "Offhand"
            _ => null
        };

        if (characterSlot == null)
        {
            Debug.LogError($"❌ Invalid slot name: {slot} (Only Weapon and Shield are valid in UI)");
            return;
        }

        // ✅ Get Equipped Item ID Before Unequipping
        string itemId = (characterSlot == "MainHand")
            ? CharacterData.Current.MainHand.ItemID
            : CharacterData.Current.Offhand.ItemID;

        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogWarning($"⚠ No item equipped in {slot} ({characterSlot}), skipping unequip.");
            return;
        }

        Debug.Log($"✅ Unequipping {slot}: {itemId}");

        // ✅ Remove the item from the correct CharacterData slot
        if (characterSlot == "MainHand")
        {
            CharacterData.Current.MainHand.ItemID = null;
            Debug.Log($"❌ {slot} cleared! CharacterData.Current.MainHand.ItemID is now NULL");
        }
        else if (characterSlot == "Offhand")
        {
            CharacterData.Current.Offhand.ItemID = null;
            Debug.Log($"❌ {slot} cleared! CharacterData.Current.Offhand.ItemID is now NULL");
        }

        // ✅ Add Unequipped Item Back to Inventory if Space is Available
        int emptySlotIndex = CharacterData.Current.Inventory.FindIndex(s => s.IsEmpty());
        if (emptySlotIndex != -1)
        {
            CharacterData.Current.Inventory[emptySlotIndex].SetItem(itemId);
            Debug.Log($"✅ {itemId} moved to inventory slot {emptySlotIndex}");
        }
        else
        {
            Debug.LogWarning($"⚠ Inventory full! {itemId} not stored.");
        }

        // ✅ Save CharacterData After Unequip
        CharacterData.Current.Save();

        // ✅ Ensure WeaponController Updates Immediately After Unequipping
        Debug.Log("🔄 Updating WeaponController after unequip...");
        FindObjectOfType<WeaponController>()?.UpdateWeapon();

        // ✅ Refresh UI After Changes
        InventoryUI.Instance.RefreshUI();
        EquipmentUI.Instance.RefreshUI();
    }


    // ✅ Equip a ring into an available slot
    public void EquipRing(string itemId)
    {
        int availableSlot = FindAvailableRingSlot();
        if (availableSlot == -1)
        {
            Debug.LogWarning("⚠ No available ring slots left! Unequip a ring first.");
            return;
        }

        ringSlots[availableSlot] = itemId;
        ApplyRingEffect(availableSlot, itemId);
        LoadItemScript(availableSlot.ToString(), itemId);
    }

    // ✅ Find which ring slot a specific ring occupies
    public int FindRingSlot(string itemId)
    {
        for (int i = 0; i < ringSlots.Length; i++)
        {
            if (ringSlots[i] == itemId)
            {
                return i;
            }
        }
        return -1;
    }

    // ✅ Unequip a ring by slot index and remove only its own effect
    public void UnequipRing(int ringIndex)
    {
        if (ringIndex < 0 || ringIndex >= ringSlots.Length || string.IsNullOrEmpty(ringSlots[ringIndex]))
            return;

        string itemId = ringSlots[ringIndex];

        RemoveRingEffect(ringIndex, itemId);
        UnloadItemScript(ringIndex.ToString());
        ringSlots[ringIndex] = null;

        Debug.Log($"❌ Unequipped ring {itemId} from slot {ringIndex}.");
    }


    // ✅ Apply item stats when equipped
    private void ApplyItemEffects(string itemId)
    {
        ItemSO item = ItemDatabaseSO.Instance.GetItemById(itemId);
        if (item == null) return;

        Health += item.healthBonus;
        Speed += item.speedBonus;
        Jump += item.jumpBonus;
    }

    // ✅ Remove item stats when unequipped
    private void RemoveItemEffects(string itemId)
    {
        ItemSO item = ItemDatabaseSO.Instance.GetItemById(itemId);
        if (item == null) return;

        Health -= item.healthBonus;
        Speed -= item.speedBonus;
        Jump -= item.jumpBonus;
    }

    // ✅ Apply a running speed multiplier
    public void ApplyRunMultiplier(float multiplier)
    {
        runMultiplier *= multiplier;
    }

    // ✅ Remove a running speed multiplier
    public void RemoveRunMultiplier(float multiplier)
    {
        runMultiplier /= multiplier;
        if (runMultiplier < 1f) runMultiplier = 1f; // Ensure it never goes below normal running speed
    }

    // ✅ Get movement speed with the run multiplier
    public float GetMovementSpeed(bool isRunning)
    {
        return isRunning ? Speed * runMultiplier : Speed;
    }

    private void LoadItemScript(string slot, string itemId)
    {
        ItemSO item = ItemDatabaseSO.Instance.GetItemById(itemId);
        if (item == null || item.effectPrefab == null) return;

        // ✅ Instantiate the prefab and make it a child of the player
        GameObject effectInstance = Instantiate(item.effectPrefab, transform);
        ringEffects[int.Parse(slot)] = effectInstance.GetComponent<MonoBehaviour>();

        Debug.Log($"🛠 Effect {item.effectPrefab.name} applied from {item.itemName}");
    }
    private void UnloadItemScript(string slot)
    {
        if (!ringEffects.TryGetValue(int.Parse(slot), out MonoBehaviour effectInstance)) return;

        Destroy(effectInstance.gameObject);
        ringEffects.Remove(int.Parse(slot));

        Debug.Log($"❌ Effect removed from slot {slot}");
    }

    // ✅ Find the next available ring slot (0-4)
    private int FindAvailableRingSlot()
    {
        for (int i = 0; i < ringSlots.Length; i++)
        {
            if (string.IsNullOrEmpty(ringSlots[i])) return i;
        }
        return -1; // No available slots
    }

    // ✅ Get ring index from slot name
    private int GetRingIndex(string slot)
    {
        if (slot.StartsWith("Ring"))
        {
            int index;
            if (int.TryParse(slot.Substring(4), out index))
                return index;
        }
        return -1;
    }

    private void LoadEquippedRings()
    {
        Debug.Log("🔄 Loading all equipped rings...");

        for (int i = 0; i < CharacterData.Current.Rings.Length; i++)
        {
            EquipmentSlot ringSlot = CharacterData.Current.Rings[i];

            if (ringSlot != null && ringSlot.IsOccupied)
            {
                string itemId = ringSlot.ItemID;

                // ✅ Store in the ringSlots array (ensures consistency)
                ringSlots[i] = itemId;

                Debug.Log($"✅ Found equipped ring: {itemId} in Ring Slot {i}");

                // ✅ Apply its effect
                ApplyRingEffect(i, itemId);
                LoadItemScript(i.ToString(), itemId);
            }
        }
    }


    // ✅ Apply ring effect when equipped
    private void ApplyRingEffect(int slotIndex, string itemId)
    {
        ItemSO item = ItemDatabaseSO.Instance.GetItemById(itemId);
        if (item == null) return;

        Health += item.healthBonus;
        Speed += item.speedBonus;
        Jump += item.jumpBonus;
    }

    // ✅ Remove the ring effect when unequipped
    private void RemoveRingEffect(int slotIndex, string itemId)
    {
        ItemSO item = ItemDatabaseSO.Instance.GetItemById(itemId);
        if (item == null) return;

        Health -= item.healthBonus;
        Speed -= item.speedBonus;
        Jump -= item.jumpBonus;
    }
}
