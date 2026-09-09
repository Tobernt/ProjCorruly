[System.Serializable]
public class InventorySlot
{
    public string ItemID; // Unique ID of item
    public int Quantity;  // Stackable items
    public bool IsOccupied; // Now tracks if a slot has an item

    public InventorySlot()
    {
        ItemID = "";
        Quantity = 0;
        IsOccupied = false; // Default to empty
    }

    public bool IsEmpty()
    {
        return !IsOccupied || string.IsNullOrEmpty(ItemID);
    }

    public void SetItem(string itemId, int quantity = 1)
    {
        ItemID = itemId;
        Quantity = quantity;
        IsOccupied = true;
    }

    public void ClearItem()
    {
        ItemID = "";
        Quantity = 0;
        IsOccupied = false;
    }
}
