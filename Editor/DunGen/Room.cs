using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a room or a distinct region in the dungeon layout.
/// </summary>
public class Room
{
    public int id;
    public int level;
    public bool isCircular;
    public Vector2Int position;
    public Vector2Int size;
    public HashSet<Vector2Int> cells;
}
