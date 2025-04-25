using UnityEditor;
using UnityEngine;

public class ItemDatabaseWindow : EditorWindow
{
    private SerializedObject serializedDatabase;
    private ItemDatabaseEditor fakeEditor; // 👈 Reuse logic
    private ItemDatabaseSO database;
    private Vector2 scrollPos;

    [MenuItem("Tools/Item Database Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<ItemDatabaseWindow>("Item Database");
        window.minSize = new Vector2(600, 400);
    }

    private void OnEnable()
    {
        // Load your actual database asset here
        string[] guids = AssetDatabase.FindAssets("t:ItemDatabaseSO");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            database = AssetDatabase.LoadAssetAtPath<ItemDatabaseSO>(path);

            if (database != null)
            {
                serializedDatabase = new SerializedObject(database);

                // Trick to use the existing CustomEditor logic
                fakeEditor = (ItemDatabaseEditor)Editor.CreateEditor(database, typeof(ItemDatabaseEditor));
            }
        }
    }

    private void OnGUI()
    {
        if (fakeEditor != null && database != null)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            fakeEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("ItemDatabaseSO not found in project.", MessageType.Warning);
        }
    }
}
