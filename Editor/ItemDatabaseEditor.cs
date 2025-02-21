using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

[CustomEditor(typeof(ItemDatabaseSO))]
public class ItemDatabaseEditor : Editor
{
    private enum Tab
    {
        Create,
        Edit,
        ItemList
    }

    private Tab currentTab = Tab.Create;

    // Variables for creating a new item
    private string newItemName = "";
    private string newItemDescription = "";
    private Sprite newItemIcon;
    private ItemSO.ItemType newItemType = ItemSO.ItemType.Misc;
    private bool newItemStackable = false;
    private int newItemMaxStackSize = 1;
    private GameObject newItemPrefab;
    private int newItemDamage = 0;
    private float newItemAttackSpeed = 0;
    private int newItemDefense = 0;
    private ItemSO.RarityType newItemRarity = ItemSO.RarityType.Common;
    private MonoScript newItemScript;

    // Search & filter
    private string searchQuery = "";
    private ItemSO.ItemType filterItemType = ItemSO.ItemType.Misc;
    private bool filterByType = false;
    private bool sortByAlphabet = true;

    public override void OnInspectorGUI()
    {
        ItemDatabaseSO database = (ItemDatabaseSO)target;

        currentTab = (Tab)GUILayout.Toolbar((int)currentTab, new string[] { "Create", "Edit", "Item List" });

        GUILayout.Space(10);

        switch (currentTab)
        {
            case Tab.Create:
                DrawCreateTab(database);
                break;
            case Tab.Edit:
                DrawEditTab(database);
                break;
            case Tab.ItemList:
                DrawItemListTab(database);
                break;
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets(); // ✅ Forces Unity to save the ScriptableObject
            AssetDatabase.Refresh(); // ✅ Ensures the database updates immediately
        }
    }


    private void DrawCreateTab(ItemDatabaseSO database)
    {
        GUILayout.Label("Create New Item", EditorStyles.boldLabel);

        newItemName = EditorGUILayout.TextField("Item Name", newItemName);
        int newItemId = GetNextAvailableId(database);
        EditorGUILayout.LabelField("Item ID", newItemId.ToString());
        newItemDescription = EditorGUILayout.TextField("Description", newItemDescription);
        newItemIcon = (Sprite)EditorGUILayout.ObjectField("Icon", newItemIcon, typeof(Sprite), false);
        newItemType = (ItemSO.ItemType)EditorGUILayout.EnumPopup("Item Type", newItemType);
        newItemStackable = EditorGUILayout.Toggle("Is Stackable", newItemStackable);
        newItemMaxStackSize = EditorGUILayout.IntField("Max Stack Size", newItemMaxStackSize);
        newItemPrefab = (GameObject)EditorGUILayout.ObjectField("Item Prefab", newItemPrefab, typeof(GameObject), false);
        newItemDamage = EditorGUILayout.IntField("Damage", newItemDamage);
        newItemAttackSpeed = EditorGUILayout.FloatField("Attack Speed", newItemAttackSpeed);
        newItemDefense = EditorGUILayout.IntField("Defense", newItemDefense);
        newItemRarity = (ItemSO.RarityType)EditorGUILayout.EnumPopup("Rarity", newItemRarity);
        newItemScript = (MonoScript)EditorGUILayout.ObjectField("Item Script", newItemScript, typeof(MonoScript), false);

        if (GUILayout.Button("Add Item"))
        {
            if (string.IsNullOrWhiteSpace(newItemName))
            {
                Debug.LogError("Item name cannot be empty!");
                return;
            }

            ItemSO newItem = ScriptableObject.CreateInstance<ItemSO>();
            newItem.itemName = newItemName;
            newItem.itemId = newItemId.ToString();
            newItem.itemDescription = newItemDescription;
            newItem.itemIcon = newItemIcon;
            newItem.itemType = newItemType;
            newItem.isStackable = newItemStackable;
            newItem.maxStackSize = newItemMaxStackSize;
            newItem.itemPrefab = newItemPrefab;
            newItem.damage = newItemDamage;
            newItem.attackSpeed = newItemAttackSpeed;
            newItem.defense = newItemDefense;
            newItem.rarity = newItemRarity;
            newItem.itemScript = newItemScript;

            string folderPath = "Assets/Items/";
            if (!AssetDatabase.IsValidFolder(folderPath))
                AssetDatabase.CreateFolder("Assets", "Items");

            string path = AssetDatabase.GenerateUniqueAssetPath(folderPath + newItemName + ".asset");
            AssetDatabase.CreateAsset(newItem, path);
            AssetDatabase.ImportAsset(path);

            database.items.Add(newItem);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();  // ✅ Force save after adding item
            AssetDatabase.Refresh();  // ✅ Ensure updated reference

            Debug.Log($"✅ Item '{newItemName}' added with ID '{newItemId}'.");

            newItemName = "";
            newItemDescription = "";
            newItemIcon = null;
            newItemType = ItemSO.ItemType.Misc;
            newItemStackable = false;
            newItemMaxStackSize = 1;
            newItemPrefab = null;
            newItemDamage = 0;
            newItemAttackSpeed = 0;
            newItemDefense = 0;
            newItemRarity = ItemSO.RarityType.Common;
            newItemScript = null;
        }
    }

    private void DrawEditTab(ItemDatabaseSO database)
    {
        GUILayout.Label("Edit Existing Items", EditorStyles.boldLabel);
        DrawSearchAndFilterControls();

        List<ItemSO> filteredItems = GetFilteredAndSortedItems(database);

        if (filteredItems.Count == 0)
        {
            GUILayout.Label("No items match your search or filter.");
            return;
        }

        foreach (var item in filteredItems)
        {
            EditorGUILayout.BeginVertical("box");

            item.itemName = EditorGUILayout.TextField("Item Name", item.itemName);
            EditorUtility.SetDirty(item); // Mark as dirty after change

            item.itemId = EditorGUILayout.TextField("Item ID", item.itemId);
            EditorUtility.SetDirty(item);

            item.itemDescription = EditorGUILayout.TextField("Description", item.itemDescription);
            EditorUtility.SetDirty(item);

            item.itemIcon = (Sprite)EditorGUILayout.ObjectField("Icon", item.itemIcon, typeof(Sprite), false);
            EditorUtility.SetDirty(item);

            item.itemType = (ItemSO.ItemType)EditorGUILayout.EnumPopup("Item Type", item.itemType);
            EditorUtility.SetDirty(item);

            item.isStackable = EditorGUILayout.Toggle("Is Stackable", item.isStackable);
            EditorUtility.SetDirty(item);

            item.maxStackSize = EditorGUILayout.IntField("Max Stack Size", item.maxStackSize);
            EditorUtility.SetDirty(item);

            item.itemPrefab = (GameObject)EditorGUILayout.ObjectField("Item Prefab", item.itemPrefab, typeof(GameObject), false);
            EditorUtility.SetDirty(item);

            item.damage = EditorGUILayout.IntField("Damage", item.damage);
            EditorUtility.SetDirty(item);

            item.attackSpeed = EditorGUILayout.FloatField("Attack Speed", item.attackSpeed);
            EditorUtility.SetDirty(item);

            item.defense = EditorGUILayout.IntField("Defense", item.defense);
            EditorUtility.SetDirty(item);

            item.rarity = (ItemSO.RarityType)EditorGUILayout.EnumPopup("Rarity", item.rarity);
            EditorUtility.SetDirty(item);

            item.itemScript = (MonoScript)EditorGUILayout.ObjectField("Item Script", item.itemScript, typeof(MonoScript), false);
            EditorUtility.SetDirty(item);

            if (GUILayout.Button("Remove Item"))
            {
                database.items.Remove(item);
                EditorUtility.SetDirty(database);
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(item));
                AssetDatabase.SaveAssets();  // ✅ Force save after removing item
                AssetDatabase.Refresh();  // ✅ Ensure updated reference

                Debug.Log($"❌ Item '{item.itemName}' removed.");
                break;
            }

            EditorGUILayout.EndVertical();
        }
    }


    private string[] GetAvailableScriptNames()
    {
        List<string> scriptNames = new List<string> { "None" }; // Default option
        MonoScript[] allScripts = Resources.FindObjectsOfTypeAll<MonoScript>();

        foreach (MonoScript script in allScripts)
        {
            Type scriptType = script.GetClass();
            if (scriptType != null && typeof(MonoBehaviour).IsAssignableFrom(scriptType))
            {
                scriptNames.Add(scriptType.FullName);
            }
        }

        return scriptNames.ToArray();
    }

    private void DrawItemListTab(ItemDatabaseSO database)
    {
        GUILayout.Label("Item List", EditorStyles.boldLabel);
        DrawSearchAndFilterControls();
        List<ItemSO> filteredItems = GetFilteredAndSortedItems(database);

        if (filteredItems.Count == 0)
        {
            GUILayout.Label("No items match your search or filter.");
            return;
        }

        foreach (var item in filteredItems)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(item.itemName, GUILayout.Width(200));
            GUILayout.Label($"ID: {item.itemId}", GUILayout.Width(100));
            GUILayout.Label($"Type: {item.itemType}");
            GUILayout.EndHorizontal();
        }
    }

    private void DrawSearchAndFilterControls()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Search", GUILayout.Width(50));
        searchQuery = GUILayout.TextField(searchQuery, GUILayout.Width(200));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        filterByType = EditorGUILayout.Toggle("Filter by Type", filterByType);
        if (filterByType)
        {
            filterItemType = (ItemSO.ItemType)EditorGUILayout.EnumPopup("Item Type", filterItemType);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        sortByAlphabet = EditorGUILayout.Toggle("Sort Alphabetically", sortByAlphabet);
        GUILayout.EndHorizontal();
    }

    private List<ItemSO> GetFilteredAndSortedItems(ItemDatabaseSO database)
    {
        IEnumerable<ItemSO> items = database.items;
        if (!string.IsNullOrWhiteSpace(searchQuery))
            items = items.Where(item => item.itemName.ToLower().Contains(searchQuery.ToLower()) || item.itemId.Contains(searchQuery));

        if (filterByType)
            items = items.Where(item => item.itemType == filterItemType);

        return sortByAlphabet ? items.OrderBy(item => item.itemName).ToList() : items.OrderBy(item => int.Parse(item.itemId)).ToList();
    }

    private int GetNextAvailableId(ItemDatabaseSO database)
    {
        HashSet<int> existingIds = new HashSet<int>(database.items.Select(item => int.Parse(item.itemId)));
        int nextId = 1;
        while (existingIds.Contains(nextId)) nextId++;
        return nextId;
    }
}
