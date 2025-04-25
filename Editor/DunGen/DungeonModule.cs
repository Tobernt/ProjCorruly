using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DungeonModule", menuName = "Dungeon Generator/Dungeon Module", order = 1)]
public class DungeonModule : ScriptableObject
{
    [Header("Basic Info")]
    public int seed;
    public int numberOfFloors;
    public DungeonGenerator.AlgorithmType algorithmUsed;
    public Vector2Int roomSizeRange;
    public bool allowCircular;
    public bool allowOverlap;
    public bool allowElevation;

    [Header("Layout Data")]
    public List<RoomData> rooms = new List<RoomData>();
    public List<Connector> connectors = new List<Connector>();
    public List<FloorCellData> floorCells = new List<FloorCellData>();

    [System.Serializable]
    public class RoomData
    {
        public int id;
        public int level;
        public Vector2Int position;
        public Vector2Int size;
        public bool isCircular;
        public List<Vector2Int> cells = new List<Vector2Int>();
    }

    [System.Serializable]
    public class FloorCellData
    {
        public int level;
        public List<Vector2Int> cells = new List<Vector2Int>();
    }

    public void SaveFromLayout(DungeonLayout layout, DungeonGenSettings settings)
    {
        seed = settings.seed;
        numberOfFloors = layout.floors;
        algorithmUsed = settings.algorithm;
        roomSizeRange = new Vector2Int(settings.minSize, settings.maxSize);
        allowCircular = settings.allowCircular;
        allowOverlap = settings.allowOverlap;
        allowElevation = settings.allowElevation;

        rooms.Clear();
        foreach (Room r in layout.rooms)
        {
            RoomData data = new RoomData
            {
                id = r.id,
                level = r.level,
                position = r.position,
                size = r.size,
                isCircular = r.isCircular,
                cells = new List<Vector2Int>(r.cells)
            };
            rooms.Add(data);
        }

        connectors = new List<Connector>(layout.connectors);

        floorCells.Clear();
        foreach (var kvp in layout.floorCellsByLevel)
        {
            FloorCellData f = new FloorCellData
            {
                level = kvp.Key,
                cells = new List<Vector2Int>(kvp.Value)
            };
            floorCells.Add(f);
        }
    }

    public DungeonLayout ToDungeonLayout()
    {
        DungeonLayout layout = new DungeonLayout(numberOfFloors);

        foreach (RoomData data in rooms)
        {
            Room r = new Room
            {
                id = data.id,
                level = data.level,
                position = data.position,
                size = data.size,
                isCircular = data.isCircular,
                cells = new HashSet<Vector2Int>(data.cells)
            };
            layout.rooms.Add(r);

            foreach (Vector2Int cell in r.cells)
            {
                layout.floorCellsByLevel[r.level].Add(cell);
                layout.roomLookup[new Vector3Int(cell.x, r.level, cell.y)] = r.id;
            }
        }

        layout.connectors = new List<Connector>(connectors);

        return layout;
    }
}
