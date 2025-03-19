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
        if (item == null)
        {
            return;
        }

        // ✅ Only allow valid types (Weapon, Shield, Ring)
        if (item.itemType != ItemSO.ItemType.Weapon &&
            item.itemType != ItemSO.ItemType.Shield &&
            item.itemType != ItemSO.ItemType.Ring)
        {
            return;
        }

        // If an item is already equipped, unequip it first
        if (IsOccupied)
        {
            UnequipItem();
        }

        ItemID = itemId;
        CharacterData.Current.Save(); // ✅ Auto-save when item is equipped

        // ✅ Notify PlayerEquipmentTracker correctly
        PlayerEquipmentTracker.Instance?.EquipItem(item.itemType.ToString(), itemId);
    }


    public void UnequipItem()
    {
        if (!IsOccupied) return;

        // ✅ Handle Rings Separately
        if (AllowedItemType == ItemSO.ItemType.Ring)
        {
            int ringIndex = PlayerEquipmentTracker.Instance.FindRingSlot(ItemID);
            if (ringIndex != -1)
            {
                PlayerEquipmentTracker.Instance.UnequipRing(ringIndex);
            }
        }
        else
        {
            PlayerEquipmentTracker.Instance?.UnequipItem(AllowedItemType.ToString());
        }

        ItemID = "";
        CharacterData.Current.Save(); // ✅ Auto-save when item is unequipped
    }
}
