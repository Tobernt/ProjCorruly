using UnityEngine;

[System.Serializable]
public class EquipmentSlot
{
    public string ItemID;
    public bool IsOccupied => !string.IsNullOrEmpty(ItemID); // ✅ Use property instead of method
    public ItemSO.ItemType AllowedItemType; // ✅ Keep item type allowance

    public EquipmentSlot(ItemSO.ItemType allowedType)
    {
        ItemID = "";
        AllowedItemType = allowedType;
    }

    public bool IsEmpty()
    {
        return !IsOccupied;
    }

    public void EquipItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;

        ItemSO item = ItemDatabaseSO.Instance.GetItemById(itemId);
        if (item == null) return;

        if (item.itemType != ItemSO.ItemType.Weapon &&
            item.itemType != ItemSO.ItemType.Shield &&
            item.itemType != ItemSO.ItemType.Ring)
        {
            Debug.LogWarning($"❌ Cannot equip item {item.itemName} of type {item.itemType}.");
            return;
        }

        // ✅ Return previously equipped item to inventory
        if (IsOccupied)
        {
            int emptySlotIndex = CharacterData.Current.Inventory.FindIndex(s => s.IsEmpty());
            if (emptySlotIndex != -1)
            {
                CharacterData.Current.Inventory[emptySlotIndex].SetItem(ItemID, 1);
            }
            else
            {
                Debug.LogWarning("⚠ No space to unequip current item.");
            }
        }

        ItemID = itemId;
        CharacterData.Current.Save();

        PlayerEquipmentTracker.Instance?.EquipItem(item.itemType.ToString(), itemId);
    }

    public void UnequipItem()
    {
        if (!IsOccupied) return;

        ItemSO item = ItemDatabaseSO.Instance?.GetItemById(ItemID);
        if (item == null)
        {
            Debug.LogError($"❌ Cannot unequip: item ID {ItemID} not found.");
            return;
        }

        // ✅ Add back to inventory
        int emptySlotIndex = CharacterData.Current.Inventory.FindIndex(s => s.IsEmpty());
        if (emptySlotIndex != -1)
        {
            CharacterData.Current.Inventory[emptySlotIndex].SetItem(ItemID, 1);
        }
        else
        {
            Debug.LogWarning("⚠ No empty inventory slot available to unequip item.");
        }

        // ✅ Notify PlayerEquipmentTracker
        switch (item.itemType)
        {
            case ItemSO.ItemType.Ring:
                int ringIndex = PlayerEquipmentTracker.Instance.FindRingSlot(ItemID);
                if (ringIndex != -1)
                    PlayerEquipmentTracker.Instance.UnequipRing(ringIndex);
                break;

            case ItemSO.ItemType.Weapon:
            case ItemSO.ItemType.Shield:
                PlayerEquipmentTracker.Instance?.UnequipItem(item.itemType.ToString());
                break;
        }

        ItemID = "";
        CharacterData.Current.Save();
    }
}
