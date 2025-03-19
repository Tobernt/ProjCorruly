using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Inventory
{
    public List<InventorySlot> Slots;

    public Inventory(int size)
    {
        Slots = new List<InventorySlot>();
        for (int i = 0; i < size; i++)
            Slots.Add(new InventorySlot());
    }
}
