using UnityEngine;

public class PlayerPickup : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // ✅ Check if the collided object has a PickupItem component
        PickupItem pickupItem = other.GetComponent<PickupItem>();

        if (pickupItem != null)
        {
            Debug.Log($"✅ Player collided with item: {pickupItem.itemId}");

            // ✅ Try to add the item to inventory
            if (AddItemToInventory(pickupItem.itemId, pickupItem.quantity))
            {
                // ✅ Save character data after adding item
                CharacterData.Current.Save();

                // ✅ Call the networked destroy function
                pickupItem.CmdDestroyPickup();
            }
        }
    }

    private bool AddItemToInventory(int itemId, int quantity)
    {
        if (CharacterData.Current == null || CharacterData.Current.Inventory == null)
        {
            Debug.LogError("❌ No active character or inventory found!");
            return false;
        }

        ItemSO itemData = ItemDatabaseSO.Instance.GetItemById(itemId.ToString());
        if (itemData == null)
        {
            Debug.LogError($"❌ Item ID {itemId} not found in database!");
            return false;
        }

        int remainingQuantity = quantity; // ✅ Tracks how much is left to stack

        // ✅ First, try stacking into existing stacks
        foreach (InventorySlot slot in CharacterData.Current.Inventory)
        {
            if (slot.ItemID == itemId.ToString() && slot.Quantity < itemData.maxStackSize)
            {
                int availableSpace = itemData.maxStackSize - slot.Quantity;
                int amountToAdd = Mathf.Min(availableSpace, remainingQuantity);

                slot.Quantity += amountToAdd;
                remainingQuantity -= amountToAdd;

                if (remainingQuantity <= 0)
                {
                    Debug.Log($"✅ Successfully stacked {quantity}x {itemData.itemName}.");
                    return true;
                }
            }
        }

        // ✅ If there is still remaining quantity, create new stacks in empty slots
        foreach (InventorySlot slot in CharacterData.Current.Inventory)
        {
            if (slot.IsEmpty())
            {
                int amountToAdd = Mathf.Min(remainingQuantity, itemData.maxStackSize);
                slot.SetItem(itemId.ToString(), amountToAdd);
                remainingQuantity -= amountToAdd;

                if (remainingQuantity <= 0)
                {
                    Debug.Log($"✅ Created new stack with {amountToAdd}x {itemData.itemName}.");
                    return true;
                }
            }
        }

        // ✅ If there's no room left, warn the player
        if (remainingQuantity > 0)
        {
            Debug.LogWarning($"❌ Not enough space for {remainingQuantity}x {itemData.itemName}! Inventory is full.");
            return false;
        }

        return true;
    }
}
