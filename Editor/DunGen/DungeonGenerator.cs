using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Main MonoBehaviour to generate a dungeon at runtime or in editor (holds settings).
/// </summary>
public class DungeonGenerator : MonoBehaviour
{
    public enum AlgorithmType { RandomWalk, Delaunay };

    [Header("Generation Parameters")]
    public AlgorithmType algorithmType = AlgorithmType.RandomWalk;
    public int roomCount = 10;
    public int minRoomSize = 4;
    public int maxRoomSize = 8;
    public bool allowCircularRooms = true;
    public int floors = 1;
    public bool allowOverlapRooms = false;
    public bool allowElevationDifference = false;
    public int randomSeed = 0;
    public bool generateOnStart = false;

    // The last generated layout data (for reference or debugging)
    public DungeonLayout latestLayout;

    private void Start()
    {
        if (generateOnStart)
        {
            Generate();
        }
    }

    /// <summary>
    /// Public method to generate the dungeon (can be called at runtime).
    /// </summary>
    public void Generate()
    {
        // Prepare settings
        DungeonGenSettings settings = new DungeonGenSettings();
        settings.roomCount = Mathf.Max(1, roomCount);
        settings.minSize = Mathf.Max(1, minRoomSize);
        settings.maxSize = Mathf.Max(settings.minSize, maxRoomSize);
        settings.allowCircular = allowCircularRooms;
        settings.allowOverlap = allowOverlapRooms;
        settings.floors = Mathf.Max(1, floors);
        settings.allowElevation = allowElevationDifference;
        settings.seed = randomSeed;
        settings.algorithm = algorithmType;
        if (!allowElevationDifference)
        {
            settings.floors = 1;
        }
        // Generate layout data
        latestLayout = GenerateLayoutInternal(settings);
        // Build the dungeon geometry as child objects under this GameObject
        DungeonBuilder.BuildDungeon(latestLayout, this.transform);
    }
    public enum VerticalConnectorType
    {
        None,
        StraightRamp,
        SpiralStairs,
        CurvedRamp,
        Random
    }

    /// <summary>
    /// Editor-only method to generate the dungeon in edit mode (called from custom inspector).
    /// </summary>
    public void GenerateInEditor()
    {
        // Destroy existing generated children (if any)
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in transform)
        {
            toDestroy.Add(child.gameObject);
        }
#if UNITY_EDITOR
        foreach (GameObject go in toDestroy)
        {
            DestroyImmediate(go);
        }
#endif
        Generate();  // reuse the runtime generate logic
    }

    /// <summary>
    /// Internal helper to select and run the appropriate algorithm and produce a DungeonLayout (including multi-floor handling).
    /// </summary>
    public static DungeonLayout GenerateLayoutInternal(DungeonGenSettings settings)
    {
        // Set up random seed for Unity's Random (for shapes and corridor randomness)
        if (settings.seed != 0)
        {
            Random.InitState(settings.seed);
        }
        else
        {
            // If seed is 0, use a random seed for unpredictability
            Random.InitState(System.Environment.TickCount);
        }

        // Select algorithm for base layout
        ILayoutGenerator algo;
        if (settings.algorithm == AlgorithmType.Delaunay)
            algo = new DelaunayLayoutGenerator();
        else
            algo = new RandomWalkLayoutGenerator();
        // Generate base (level 0) layout
        DungeonLayout baseLayout = algo.GenerateLayout(settings);

        // If multiple floors requested and elevation differences allowed, generate additional floors
        if (settings.floors > 1 && settings.allowElevation)
        {
            for (int lvl = 1; lvl < settings.floors; lvl++)
            {
                // Prepare settings for this upper floor
                DungeonGenSettings floorSettings = new DungeonGenSettings();
                floorSettings.roomCount = settings.roomCount;
                floorSettings.minSize = settings.minSize;
                floorSettings.maxSize = settings.maxSize;
                floorSettings.allowCircular = settings.allowCircular;
                floorSettings.allowOverlap = settings.allowOverlap;
                floorSettings.floors = 1;
                floorSettings.allowElevation = false;
                floorSettings.seed = (settings.seed != 0 ? settings.seed + lvl : 0);
                floorSettings.algorithm = settings.algorithm;

                // Generate layout for floor 'lvl'
                ILayoutGenerator floorAlgo = (floorSettings.algorithm == AlgorithmType.Delaunay)
                                            ? (ILayoutGenerator)new DelaunayLayoutGenerator()
                                            : new RandomWalkLayoutGenerator();
                DungeonLayout floorLayout = floorAlgo.GenerateLayout(floorSettings);

                // Align the new floor above a random room of the previous level
                if (baseLayout.rooms.Count > 0 && floorLayout.rooms.Count > 0)
                {
                    Room baseAnchor = baseLayout.rooms[Random.Range(0, baseLayout.rooms.Count)];
                    Room newAnchor = floorLayout.rooms[Random.Range(0, floorLayout.rooms.Count)];
                    Vector2Int anchorOffset = baseAnchor.position - newAnchor.position;
                    // Shift all cells and room positions by the anchor offset
                    HashSet<Vector2Int> shiftedCells = new HashSet<Vector2Int>();
                    foreach (Vector2Int cell in floorLayout.floorCellsByLevel[0])
                    {
                        shiftedCells.Add(new Vector2Int(cell.x + anchorOffset.x, cell.y + anchorOffset.y));
                    }
                    floorLayout.floorCellsByLevel[0] = shiftedCells;
                    foreach (Room r in floorLayout.rooms)
                    {
                        HashSet<Vector2Int> shiftedRoomCells = new HashSet<Vector2Int>();
                        foreach (Vector2Int cell in r.cells)
                        {
                            shiftedRoomCells.Add(new Vector2Int(cell.x + anchorOffset.x, cell.y + anchorOffset.y));
                        }
                        r.position += anchorOffset;
                        r.cells = shiftedRoomCells;
                    }
                }

                // Remove any cells on this new floor that are not supported by a floor cell below
                HashSet<Vector2Int> prevFloorCells = baseLayout.floorCellsByLevel[lvl - 1];
                foreach (Vector2Int cell in new HashSet<Vector2Int>(floorLayout.floorCellsByLevel[0]))
                {
                    if (!prevFloorCells.Contains(cell))
                    {
                        floorLayout.floorCellsByLevel[0].Remove(cell);
                    }
                }

                // Remove any room that lost all its cells after support filtering
                List<Room> remainingRooms = new List<Room>();
                foreach (Room r in floorLayout.rooms)
                {
                    r.cells.IntersectWith(floorLayout.floorCellsByLevel[0]);
                    if (r.cells.Count > 0)
                    {
                        r.level = lvl;
                        remainingRooms.Add(r);
                    }
                }
                floorLayout.rooms = remainingRooms;

                // Add the new floor's cells and rooms into the baseLayout
                baseLayout.floorCellsByLevel[lvl] = new HashSet<Vector2Int>(floorLayout.floorCellsByLevel[0]);
                foreach (Room r in floorLayout.rooms)
                {
                    r.id = baseLayout.rooms.Count;
                    baseLayout.rooms.Add(r);
                }

                // Merge room lookup for new floor cells
                foreach (Vector2Int cell in floorLayout.floorCellsByLevel[0])
                {
                    Vector3Int cellKey = new Vector3Int(cell.x, lvl, cell.y);
                    // Find which new room this cell belongs to
                    int roomIndex = baseLayout.rooms.FindIndex(room => room.level == lvl && room.cells.Contains(cell));
                    if (roomIndex >= 0)
                        baseLayout.roomLookup[cellKey] = roomIndex;
                }

                // Transfer intra-floor connectors (doorways) of the new floor
                foreach (Connector conn in floorLayout.connectors)
                {
                    bool fromExists = conn.fromRoomId < floorLayout.rooms.Count;
                    bool toExists = conn.toRoomId < floorLayout.rooms.Count;
                    if (fromExists && toExists)
                    {
                        Connector newConn = new Connector();
                        newConn.type = conn.type;
                        newConn.fromRoomId = baseLayout.rooms.Count - floorLayout.rooms.Count + conn.fromRoomId;
                        newConn.toRoomId = baseLayout.rooms.Count - floorLayout.rooms.Count + conn.toRoomId;
                        newConn.fromLevel = lvl;
                        newConn.toLevel = lvl;
                        newConn.position = new Vector3(conn.position.x, conn.position.y, conn.position.z);
                        newConn.normal = conn.normal;
                        baseLayout.connectors.Add(newConn);
                    }
                }

                // Add a vertical connector (stairs/ramps) between level (lvl-1) and lvl
                if (settings.autoConnectVertical && baseLayout.floorCellsByLevel[lvl].Count > 0)
                {
                    Vector2Int stairCell = new List<Vector2Int>(baseLayout.floorCellsByLevel[lvl])[0];
                    Connector vertConn = new Connector();

                    // Determine connector type based on user selection
                    DungeonGenerator.VerticalConnectorType style = settings.verticalConnectorStyle;
                    ConnectorType connType;
                    if (style == DungeonGenerator.VerticalConnectorType.Random)
                    {
                        int r = Random.Range(0, 3);
                        connType = (r == 0 ? ConnectorType.StraightRamp
                                   : r == 1 ? ConnectorType.SpiralStairs
                                            : ConnectorType.CurvedRamp);
                    }
                    else
                    {
                        switch (style)
                        {
                            case DungeonGenerator.VerticalConnectorType.StraightRamp:
                                connType = ConnectorType.StraightRamp; break;
                            case DungeonGenerator.VerticalConnectorType.SpiralStairs:
                                connType = ConnectorType.SpiralStairs; break;
                            case DungeonGenerator.VerticalConnectorType.CurvedRamp:
                                connType = ConnectorType.CurvedRamp; break;
                            default:
                                connType = ConnectorType.Stairs; break;
                        }
                    }

                    vertConn.type = connType;
                    vertConn.fromLevel = lvl - 1;
                    vertConn.toLevel = lvl;
                    vertConn.fromRoomId = -1;
                    vertConn.toRoomId = -1;

                    Vector3Int belowKey = new Vector3Int(stairCell.x, lvl - 1, stairCell.y);
                    if (baseLayout.roomLookup.ContainsKey(belowKey))
                        vertConn.fromRoomId = baseLayout.roomLookup[belowKey];
                    Vector3Int aboveKey = new Vector3Int(stairCell.x, lvl, stairCell.y);
                    if (baseLayout.roomLookup.ContainsKey(aboveKey))
                        vertConn.toRoomId = baseLayout.roomLookup[aboveKey];

                    vertConn.position = new Vector3(stairCell.x, 0, stairCell.y);
                    vertConn.normal = Vector3.up;
                    baseLayout.connectors.Add(vertConn);
                }


                baseLayout.floors = settings.floors;
            }
        }

        return baseLayout;
    }
}
