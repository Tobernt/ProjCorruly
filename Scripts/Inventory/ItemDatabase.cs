using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabaseSO : ScriptableObject
{
    public static ItemDatabaseSO Instance { get; set; } // Singleton Instance

    public List<ItemSO> items = new List<ItemSO>();

    private void OnEnable()
    {
        Instance = this; // Assign Instance when enabled
    }

    public ItemSO GetItemById(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogError($"❌ Cannot retrieve item. ItemID is null or empty!");
            return null;
        }

        foreach (var item in items)
        {
            if (item.itemId == itemId)
                return item;
        }

        Debug.LogError($"❌ Item with ID '{itemId}' not found in the database. Check ItemDatabaseSO.");
        return null;
    }

}
