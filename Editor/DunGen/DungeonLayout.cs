using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds the result of a dungeon generation: floor map, rooms, and connectors.
/// </summary>
public class DungeonLayout
{
    public int floors;
    public Dictionary<int, HashSet<Vector2Int>> floorCellsByLevel;
    public List<Room> rooms;
    public List<Connector> connectors;
    // Lookup to find which room a given cell originally belongs to (includes level info)
    public Dictionary<Vector3Int, int> roomLookup;

    public DungeonLayout(int floorCount)
    {
        this.floors = floorCount;
        floorCellsByLevel = new Dictionary<int, HashSet<Vector2Int>>();
        for (int i = 0; i < floorCount; i++)
        {
            floorCellsByLevel[i] = new HashSet<Vector2Int>();
        }
        rooms = new List<Room>();
        connectors = new List<Connector>();
        roomLookup = new Dictionary<Vector3Int, int>();
    }
}
