using UnityEngine;
using System.Collections.Generic;

public class EquipmentUI : MonoBehaviour
{
    public static EquipmentUI Instance { get; private set; } // ✅ Singleton

    public List<EquipmentSlotUI> equipmentSlotUIs = new List<EquipmentSlotUI>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (CharacterData.Current == null)
        {
            Debug.LogError("❌ No character data found when initializing Equipment UI.");
            return;
        }

        InitializeEquipment();
        RefreshUI();
    }

    public void InitializeEquipment()
    {
        if (CharacterData.Current == null)
        {
            Debug.LogError("❌ No character data available for equipment.");
            return;
        }

        for (int i = 0; i < equipmentSlotUIs.Count; i++)
        {
            EquipmentSlot slot = GetEquipmentSlotByIndex(i);

            if (slot == null)
            {
                Debug.LogError($"❌ No EquipmentSlot found for index {i}");
                continue;
            }

            Debug.Log($"✅ Assigning EquipmentSlotUI[{i}] to {slot.AllowedItemType}");
            equipmentSlotUIs[i].Setup(i, slot);
        }

        Debug.Log($"✅ Initialized {equipmentSlotUIs.Count} equipment slots.");
    }





    public void RefreshUI()
    {
        if (CharacterData.Current == null)
        {
            Debug.LogError("❌ Attempted to refresh Equipment UI, but no character data found.");
            return;
        }

        Debug.Log($"🔄 Refreshing Equipment UI ({equipmentSlotUIs.Count} slots)...");

        foreach (EquipmentSlotUI slotUI in equipmentSlotUIs)
        {
            slotUI?.UpdateSlot();
        }
    }
    public EquipmentSlot GetEquipmentSlotByIndex(int index)
    {
        var character = CharacterData.Current;
        return index switch
        {
            0 => character.MainHand,  // ✅ Weapon Slot
            1 => character.Offhand,   // ✅ Shield Slot
            _ => (index >= 2 && index < 7) ? character.Rings[index - 2] : null // ✅ Rings 2-6 → Rings[0-4]
        };
    }


    public int GetEquipmentSlotIndex(ItemSO.ItemType itemType)
    {
        return itemType switch
        {
            ItemSO.ItemType.Weapon => 0,
            ItemSO.ItemType.Shield => 1,
            ItemSO.ItemType.Ring => FindFirstAvailableRingSlot(),
            _ => -1 // Return -1 if no matching slot found
        };
    }
    public int FindFirstAvailableRingSlot()
    {
        for (int i = 2; i <= 6; i++) // ✅ Check UI slots 2-6
        {
            int ringArrayIndex = i - 2; // ✅ Convert 2-6 to 0-4

            if (ringArrayIndex >= 0 && ringArrayIndex < CharacterData.Current.Rings.Length &&
                (CharacterData.Current.Rings[ringArrayIndex] == null || !CharacterData.Current.Rings[ringArrayIndex].IsOccupied))
            {
                Debug.Log($"✅ Found available ring slot at UI index {i} (Rings[{ringArrayIndex}])");
                return i; // ✅ Return UI index (2-6)
            }
        }

        Debug.LogWarning("⚠ No available ring slots detected in FindFirstAvailableRingSlot()!");
        return -1;
    }
    public void SwapEquipment(int inventorySlotIndex, int equipmentSlotIndex)
    {
        var character = CharacterData.Current;
        if (character == null || character.Inventory == null)
        {
            Debug.LogError("❌ Character data or inventory is missing.");
            return;
        }

        InventorySlot inventorySlot = character.Inventory[inventorySlotIndex];
        EquipmentSlot equipmentSlot = character.GetEquipmentSlotByIndex(equipmentSlotIndex);

        if (inventorySlot == null || equipmentSlot == null)
        {
            Debug.LogError($"❌ Invalid swap: InventorySlot[{inventorySlotIndex}] or EquipmentSlot[{equipmentSlotIndex}] is null.");
            return;
        }

        ItemSO item = ItemDatabaseSO.Instance?.GetItemById(inventorySlot.ItemID);
        if (item == null)
        {
            Debug.LogError($"❌ Item data not found for ID {inventorySlot.ItemID}");
            return;
        }

        // ✅ Ensure the item is compatible with the slot
        if (item.itemType != equipmentSlot.AllowedItemType)
        {
            Debug.LogWarning($"⚠ Cannot equip: {item.itemName} is not allowed in Equipment Slot {equipmentSlotIndex}");
            return;
        }

        string uiSlotName = item.itemType switch
        {
            ItemSO.ItemType.Weapon => "Weapon",
            ItemSO.ItemType.Shield => "Shield",
            ItemSO.ItemType.Ring => $"Ring{equipmentSlotIndex - 2}",
            _ => null
        };

        if (string.IsNullOrEmpty(uiSlotName))
        {
            Debug.LogError($"❌ Could not resolve UI slot name for equipmentSlotIndex: {equipmentSlotIndex}");
            return;
        }

        Debug.Log($"🛠 Swapping: Equipping {item.itemName} from Inventory[{inventorySlotIndex}] into {uiSlotName}");

        // ✅ Call central logic — PlayerEquipmentTracker will:
        // - unequip any old item if necessary
        // - update character data
        // - handle inventory slot changes
        // - spawn visuals
        PlayerEquipmentTracker.Instance.EquipItem(uiSlotName, inventorySlot.ItemID);

        // ✅ Clear inventory slot after moving (since EquipItem will unequip to inventory first if needed)
        inventorySlot.ClearItem();

        CharacterData.Current.Save();
        InventoryUI.Instance.RefreshUI();
        RefreshUI();
    }

}
