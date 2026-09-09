using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class DungeonGeneratorWindow : EditorWindow
{
    // Generation parameters (with default values)
    private int roomCount = 10;
    private Vector2Int roomSizeRange = new Vector2Int(4, 8);
    private bool allowCircularRooms = true;
    private DungeonGenerator.AlgorithmType algorithmType = DungeonGenerator.AlgorithmType.RandomWalk;
    private int floors = 1;
    private bool allowOverlapRooms = false;
    private bool allowElevationDifference = false;
    private int randomSeed = 0;
    private DungeonLayout lastLayout = null;
    private GameObject lastDungeonRoot = null;
    // New fields at class scope
    private bool autoConnectVertical = true;
    private DungeonGenerator.VerticalConnectorType verticalConnectorType = DungeonGenerator.VerticalConnectorType.StraightRamp;
    private bool onlyRoundRooms = false;
    private GameObject floorPrefab, wallPrefab, doorPrefab;
    private GameObject stairsPrefab, spiralStairsPrefab, curvedRampPrefab;
    private bool addRoofs = false;
    private GameObject roofPrefab = null, holePrefab = null;

    [MenuItem("Tools/Procedural Dungeon Generator")]
    public static void ShowWindow()
    {
        GetWindow<DungeonGeneratorWindow>("Dungeon Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Dungeon Generation Settings", EditorStyles.boldLabel);

        roomCount = EditorGUILayout.IntField(new GUIContent("Room Count", "Number of rooms to generate"), roomCount);
        roomSizeRange.x = EditorGUILayout.IntField(new GUIContent("Min Room Size", "Minimum room diameter/width"), roomSizeRange.x);
        roomSizeRange.y = EditorGUILayout.IntField(new GUIContent("Max Room Size", "Maximum room diameter/width"), roomSizeRange.y);
        allowCircularRooms = EditorGUILayout.Toggle(new GUIContent(
        "Allow Circular Rooms", "If true, some rooms can be round"), allowCircularRooms);
        addRoofs = EditorGUILayout.Toggle(new GUIContent("Add Roof Ceilings", "Place roof tiles (ceilings) on each level except the topmost"), addRoofs);

        // New toggle: Only Round Rooms
        onlyRoundRooms = EditorGUILayout.Toggle(new GUIContent(
            "Round Rooms Only", "If true, generate only circular rooms"), onlyRoundRooms);
        if (onlyRoundRooms)
        {
            // If only round rooms, ensure circular rooms are allowed
            allowCircularRooms = true;
        }
        algorithmType = (DungeonGenerator.AlgorithmType)EditorGUILayout.EnumPopup(new GUIContent("Layout Algorithm", "Algorithm for layout generation"), algorithmType);
        floors = EditorGUILayout.IntField(new GUIContent("Floors (Levels)", "Number of vertical floors/levels"), floors);
        allowOverlapRooms = EditorGUILayout.Toggle(new GUIContent("Allow Overlapping Rooms", "If true, rooms may overlap each other"), allowOverlapRooms);
        allowElevationDifference = EditorGUILayout.Toggle(new GUIContent("Enable Elevation Differences", "If true, allow multiple vertical levels"), allowElevationDifference);
        randomSeed = EditorGUILayout.IntField(new GUIContent("Random Seed (0 = random)", "Seed for repeatable generation"), randomSeed);
        roofPrefab = (GameObject)EditorGUILayout.ObjectField(
        new GUIContent("Roof Prefab", "Prefab for roof/ceiling tiles"),
        roofPrefab, typeof(GameObject), false);
        holePrefab = (GameObject)EditorGUILayout.ObjectField(
        new GUIContent("Trapdoor/Hole Prefab", "Prefab for hole or trapdoor at stair openings"),
        holePrefab, typeof(GameObject), false);
        allowElevationDifference = EditorGUILayout.Toggle(new GUIContent(
    "Enable Elevation Differences", "Allow multiple vertical levels"), allowElevationDifference);

        // New toggle for auto vertical connectors (stairs/ramps between levels)
        autoConnectVertical = EditorGUILayout.Toggle(new GUIContent(
            "Auto-Connect Vertical", "Automatically place stairs/ramps between different levels"),
            autoConnectVertical);

        // New dropdown for vertical connector type selection
        verticalConnectorType = (DungeonGenerator.VerticalConnectorType)EditorGUILayout.EnumPopup(
            new GUIContent("Vertical Connector Type", "Type of connector between floors"),
            verticalConnectorType);

        // New prefab object fields for custom geometry
        floorPrefab = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Floor Prefab", "Prefab for floor tiles (1x1 unit)"),
            floorPrefab, typeof(GameObject), false);
        wallPrefab = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Wall Prefab", "Prefab for wall segments (1 unit width)"),
            wallPrefab, typeof(GameObject), false);
        doorPrefab = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Door Prefab", "Prefab for doorway connector"),
            doorPrefab, typeof(GameObject), false);
        stairsPrefab = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Straight Stairs/Ramp Prefab", "Prefab for straight vertical connector"),
            stairsPrefab, typeof(GameObject), false);
        spiralStairsPrefab = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Spiral Stair Prefab", "Prefab for spiral staircase connector"),
            spiralStairsPrefab, typeof(GameObject), false);
        curvedRampPrefab = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Curved Ramp Prefab", "Prefab for curved ramp connector"),
            curvedRampPrefab, typeof(GameObject), false);

        GUILayout.Space(5);
        if (GUILayout.Button("Generate Dungeon"))
        {
            GenerateDungeonInEditor();
        }

        if (lastLayout != null)
        {
            if (GUILayout.Button("Save Dungeon as Module Asset"))
            {
                SaveDungeonModule();
            }
        }
    }

    // Generate dungeon using current settings, instantiate in scene
    private void GenerateDungeonInEditor()
    {
        // Clean up previous dungeon
        if (lastDungeonRoot != null)
        {
            DestroyImmediate(lastDungeonRoot);
            lastDungeonRoot = null;
        }
        // Prepare generation settings
        DungeonGenSettings settings = new DungeonGenSettings();
        settings.roomCount = Mathf.Max(1, roomCount);
        settings.minSize = Mathf.Max(1, roomSizeRange.x);
        settings.maxSize = Mathf.Max(settings.minSize, roomSizeRange.y);
        settings.allowCircular = allowCircularRooms;
        settings.onlyRound = onlyRoundRooms;
        settings.allowOverlap = allowOverlapRooms;
        settings.floors = Mathf.Max(1, floors);
        settings.allowElevation = allowElevationDifference;
        settings.autoConnectVertical = autoConnectVertical;
        settings.verticalConnectorStyle = verticalConnectorType;
        settings.seed = randomSeed;
        settings.algorithm = algorithmType;
        settings.addRoofs = addRoofs;

        // Generate layout data
        DungeonLayout layout = DungeonGenerator.GenerateLayoutInternal(settings);

        // Build the dungeon GameObject hierarchy in the scene with assigned prefabs
        GameObject dungeonRoot = DungeonBuilder.BuildDungeon(
            layout, null, floorPrefab, wallPrefab, doorPrefab,
            stairsPrefab, spiralStairsPrefab, curvedRampPrefab,
            roofPrefab, holePrefab, addRoofs);
        dungeonRoot.name = "Dungeon_Generated";
        lastDungeonRoot = dungeonRoot;
        lastLayout = layout;

        // Focus selection on the new dungeon root
        Selection.activeGameObject = dungeonRoot;
    }

    private void SaveDungeonModule()
    {
        // Save the last generated layout as a DungeonModule asset
        DungeonModule module = ScriptableObject.CreateInstance<DungeonModule>();
        module.numberOfFloors = lastLayout.floors;
        module.connectors = new List<Connector>(lastLayout.connectors);
        AssetDatabase.CreateAsset(module, "Assets/DungeonModule.asset");
        AssetDatabase.SaveAssets();
        Debug.Log("Dungeon saved as DungeonModule asset.");
    }
}
