using UnityEngine;

/// <summary>
/// Represents a connection point or passage between rooms/floors in the dungeon layout.
/// </summary>
public class Connector
{
    public ConnectorType type;
    public int fromRoomId;
    public int toRoomId;
    public int fromLevel;
    public int toLevel;
    public Vector3 position;
    public Vector3 normal;
}

public enum ConnectorType
{
    Doorway,
    Stairs,        // existing (assume straight stairs)
    Ladder,        // existing
    StraightRamp,  // new
    CurvedRamp,    // new
    SpiralStairs   // new
}
