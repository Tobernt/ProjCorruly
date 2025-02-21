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


    // ✅ Reset stats to base values
    private void ResetStats()
    {
        Health = baseHealth;
        Speed = baseSpeed;
        Jump = baseJump;
        runMultiplier = 1f;
    }

    // ✅ Equip an item (for non-rings)
    public void EquipItem(string slot, string itemId)
    {
        if (slot.StartsWith("Ring"))
        {
            EquipRing(itemId);
            return;
        }

        // If something is already equipped in this slot, unequip it first
        UnequipItem(slot);

        ApplyItemEffects(itemId);
        LoadItemScript(slot, itemId);
    }

    // ✅ Unequip an item (for non-rings)
    public void UnequipItem(string slot)
    {
        if (slot.StartsWith("Ring"))
        {
            int ringIndex = GetRingIndex(slot);
            UnequipRing(ringIndex);
            return;
        }

        EquipmentSlot equipmentSlot = slot switch
        {
            "MainHand" => CharacterData.Current.MainHand,
            "Offhand" => CharacterData.Current.Offhand,
            _ => null
        };

        if (equipmentSlot == null || !equipmentSlot.IsOccupied)
        {
            Debug.LogWarning($"⚠ No item equipped in {slot}");
            return;
        }

        string itemId = equipmentSlot.ItemID;
        equipmentSlot.UnequipItem();

        // Add unequipped item back to inventory
        int emptySlotIndex = CharacterData.Current.Inventory.FindIndex(s => s.IsEmpty());
        if (emptySlotIndex != -1)
        {
            CharacterData.Current.Inventory[emptySlotIndex].SetItem(itemId);
        }
        else
        {
            Debug.LogWarning($"⚠ Inventory full! Unequipped {itemId} but no empty slot available.");
        }

        CharacterData.Current.Save();
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

    // ✅ Load an effect script tied to a specific slot
    private void LoadItemScript(string slot, string itemId)
    {
        ItemSO item = ItemDatabaseSO.Instance.GetItemById(itemId);
        if (item == null || item.itemScript == null) return;

        System.Type scriptType = item.itemScript.GetClass();
        if (scriptType == null || !typeof(MonoBehaviour).IsAssignableFrom(scriptType)) return;

        GameObject itemScriptContainer = new GameObject($"{item.itemName}_Script_{slot}");
        itemScriptContainer.transform.SetParent(transform);

        MonoBehaviour scriptInstance = itemScriptContainer.AddComponent(scriptType) as MonoBehaviour;
        if (scriptInstance != null)
        {
            ringEffects[int.Parse(slot)] = scriptInstance;
            Debug.Log($"🛠 {item.itemName} script applied to slot {slot}");
        }
    }

    // ✅ Unload a script from a specific slot
    private void UnloadItemScript(string slot)
    {
        if (!ringEffects.ContainsKey(int.Parse(slot))) return;

        Destroy(ringEffects[int.Parse(slot)].gameObject);
        ringEffects.Remove(int.Parse(slot));
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
