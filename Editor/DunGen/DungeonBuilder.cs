using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Static utility to build dungeon geometry (floor tiles, walls, connectors) from a DungeonLayout.
/// </summary>
public static class DungeonBuilder
{
    // Dimensions for geometry
    private static float cellSize = 1f;
    private static float wallHeight = 3f;
    private static float wallThickness = 0.2f;
    private static float floorThickness = 0.2f;

    /// <summary>
    /// Build the dungeon described by layout into the scene. Returns the root GameObject of the dungeon.
    /// </summary>
    public static GameObject BuildDungeon(
        DungeonLayout layout, Transform parent = null,
        GameObject floorPrefab = null, GameObject wallPrefab = null,
        GameObject doorPrefab = null,
        GameObject stairsPrefab = null, GameObject spiralStairsPrefab = null,
        GameObject curvedRampPrefab = null,
        GameObject roofPrefab = null, GameObject holePrefab = null,
        bool addRoofs = false)
    {
        GameObject dungeonRoot = parent ? parent.gameObject : new GameObject("Dungeon");
        // Create parent containers for organization
        GameObject floorsParent = new GameObject("Floors");
        floorsParent.transform.SetParent(dungeonRoot.transform);
        GameObject wallsParent = new GameObject("Walls");
        wallsParent.transform.SetParent(dungeonRoot.transform);
        GameObject connectorsParent = new GameObject("Connectors");
        connectorsParent.transform.SetParent(dungeonRoot.transform);
        GameObject roofsParent = null;
        if (addRoofs)
        {
            roofsParent = new GameObject("Roofs");
            roofsParent.transform.SetParent(dungeonRoot.transform);
        }

        // Prepare sets for vertical connector coordinates
        HashSet<Vector3Int> connectorDestinations = new HashSet<Vector3Int>();
        HashSet<Vector3Int> connectorSources = new HashSet<Vector3Int>();
        foreach (Connector conn in layout.connectors)
        {
            if (conn.type != ConnectorType.Doorway && conn.fromLevel != conn.toLevel)
            {
                // Mark the upper and lower cells involved in this vertical connection
                int lowerLevel = Mathf.Min(conn.fromLevel, conn.toLevel);
                int upperLevel = Mathf.Max(conn.fromLevel, conn.toLevel);
                Vector3Int src = new Vector3Int(
                    Mathf.RoundToInt(conn.position.x),
                    lowerLevel,
                    Mathf.RoundToInt(conn.position.z)
                );

                Vector3Int dst = new Vector3Int(
                    Mathf.RoundToInt(conn.position.x),
                    upperLevel,
                    Mathf.RoundToInt(conn.position.z)
                );
                connectorSources.Add(src);
                connectorDestinations.Add(dst);
            }
        }

        float cellSize = 1f;
        float wallHeight = 3f;
        float floorThickness = 0.2f;
        float wallThickness = 0.2f;

        //  Floor Tile Placement
        foreach (var kvp in layout.floorCellsByLevel)
        {
            int level = kvp.Key;
            foreach (Vector2Int cell in kvp.Value)
            {
                Vector3Int cellKey = new Vector3Int(cell.x, level, cell.y);
                if (connectorDestinations.Contains(cellKey))
                {
                    // This cell is where an upstairs connector arrives – remove floor and optionally place trapdoor/hole
                    if (holePrefab != null)
                    {
                        GameObject holeObj = GameObject.Instantiate(holePrefab);
                        holeObj.name = $"Hole_{level}_{cell.x}_{cell.y}";
                                                                                // Assume the prefab is roughly 1x1 floor-sized; scale if needed
                        holeObj.transform.localScale = new Vector3(cellSize, floorThickness, cellSize);
                        // Position it where the floor would have been (flush with floor level)
                        float baseY = level * wallHeight;
                        holeObj.transform.position = new Vector3(cell.x * cellSize, baseY + floorThickness / 2f, cell.y * cellSize);
                        holeObj.transform.SetParent(floorsParent.transform);
                    }
                    else
                    {
                        //// No prefab provided: use a thin cube as a placeholder trapdoor
                        //GameObject holeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        //holeObj.name = $"Hole_{level}_{cell.x}_{cell.y}";
                        //holeObj.transform.localScale = new Vector3(cellSize, floorThickness, cellSize);
                        //float baseY = level * wallHeight;
                        //holeObj.transform.position = new Vector3(cell.x * cellSize, baseY + floorThickness / 2f, cell.y * cellSize);
                        //holeObj.transform.SetParent(floorsParent.transform);
                    }
                    // Skip creating a normal floor tile here
                    continue;
                }

                // Normal floor tile placement (unchanged)
                GameObject floorObj;
                if (floorPrefab != null)
                    floorObj = GameObject.Instantiate(floorPrefab);
                else
                    floorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorObj.name = $"Floor_{level}_{cell.x}_{cell.y}";
                // Scale to cell size (X,Z) and thin thickness (Y)
                floorObj.transform.localScale = new Vector3(cellSize, floorThickness, cellSize);
                float worldX = cell.x * cellSize;
                float worldY = level * wallHeight;
                float worldZ = cell.y * cellSize;
                floorObj.transform.position = new Vector3(worldX, worldY + floorThickness / 2f, worldZ);
                floorObj.transform.SetParent(floorsParent.transform);
            }
        }

        //  Wall Placement around each floor cell (unchanged logic)
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        foreach (var kvp in layout.floorCellsByLevel)
        {
            int level = kvp.Key;
            HashSet<Vector2Int> floorCells = kvp.Value;
            foreach (Vector2Int cell in floorCells)
            {
                float baseX = cell.x * cellSize;
                float baseY = level * wallHeight;
                float baseZ = cell.y * cellSize;
                foreach (Vector2Int dir in directions)
                {
                    Vector2Int neighbor = cell + dir;
                    if (!floorCells.Contains(neighbor))
                    {
                        // Place a wall segment at this boundary
                        GameObject wallObj = (wallPrefab != null)
                            ? GameObject.Instantiate(wallPrefab)
                            : GameObject.CreatePrimitive(PrimitiveType.Cube);
                        wallObj.name = $"Wall_{level}_{cell.x}_{cell.y}_{dir.x}_{dir.y}";
                        // Set wall dimensions and position based on direction
                        if (dir == Vector2Int.up || dir == Vector2Int.down)
                        {
                            // North/South wall
                            wallObj.transform.localScale = new Vector3(cellSize, wallHeight, wallThickness);
                            float offsetZ = (dir == Vector2Int.up) ? cellSize / 2f : -cellSize / 2f;
                            wallObj.transform.position = new Vector3(baseX, baseY + wallHeight / 2f, baseZ + offsetZ);
                        }
                        else
                        {
                            // East/West wall
                            wallObj.transform.localScale = new Vector3(wallThickness, wallHeight, cellSize);
                            float offsetX = (dir == Vector2Int.right) ? cellSize / 2f : -cellSize / 2f;
                            wallObj.transform.position = new Vector3(baseX + offsetX, baseY + wallHeight / 2f, baseZ);
                        }
                        wallObj.transform.SetParent(wallsParent.transform);
                    }
                }
            }
        }

        //  Connector Placement (doors, stairs, ramps) – unchanged except using provided prefabs/placeholders
        foreach (Connector conn in layout.connectors)
        {
            if (conn.type == ConnectorType.Doorway)
            {
                // (Doorway handling as before – instantiate doorPrefab or leave open gap)
                if (doorPrefab != null)
                {
                    GameObject doorObj = GameObject.Instantiate(doorPrefab);
                    doorObj.name = $"Doorway_{conn.fromRoomId}_{conn.toRoomId}";
                    // Scale and position door in the doorway opening
                    doorObj.transform.localScale = new Vector3(cellSize, wallHeight, wallThickness);
                    Vector3 pos = conn.position;
                    pos.y = conn.fromLevel * wallHeight;
                    doorObj.transform.position = new Vector3(pos.x * cellSize, pos.y, pos.z * cellSize);
                    if (conn.normal != Vector3.zero)
                    {
                        // Orient door to face the connector normal (wall direction)
                        doorObj.transform.rotation = Quaternion.LookRotation(conn.normal);
                    }
                    doorObj.transform.SetParent(connectorsParent.transform);
                }
                // If no doorPrefab, leave the gap as an open doorway (no object needed)
            }
            else
            {
                // Vertical connector (stairs/ramps/ladders)
                GameObject connectorObj;
                switch (conn.type)
                {
                    case ConnectorType.SpiralStairs:
                        connectorObj = (spiralStairsPrefab != null)
                            ? GameObject.Instantiate(spiralStairsPrefab)
                            : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        connectorObj.name = $"SpiralStairs_{conn.fromLevel}_{conn.toLevel}";
                        break;
                    case ConnectorType.CurvedRamp:
                        connectorObj = (curvedRampPrefab != null)
                            ? GameObject.Instantiate(curvedRampPrefab)
                            : GameObject.CreatePrimitive(PrimitiveType.Cube);
                        connectorObj.name = $"CurvedRamp_{conn.fromLevel}_{conn.toLevel}";
                        break;
                    case ConnectorType.StraightRamp:
                        connectorObj = (stairsPrefab != null)
                            ? GameObject.Instantiate(stairsPrefab)
                            : GameObject.CreatePrimitive(PrimitiveType.Cube);
                        connectorObj.name = $"Ramp_{conn.fromLevel}_{conn.toLevel}";
                        break;
                    case ConnectorType.Stairs:
                    case ConnectorType.Ladder:
                    default:
                        connectorObj = (stairsPrefab != null)
                            ? GameObject.Instantiate(stairsPrefab)
                            : GameObject.CreatePrimitive(PrimitiveType.Cube);
                        connectorObj.name = $"Stairs_{conn.fromLevel}_{conn.toLevel}";
                        break;
                }
                // Determine vertical span between levels
                float baseLevelY = conn.fromLevel * wallHeight;
                float topLevelY = conn.toLevel * wallHeight;
                float verticalSpan = Mathf.Abs(topLevelY - baseLevelY);
                // Scale connector to span the vertical distance (assuming model is 1 unit high by default)
                Vector3 baseScale = connectorObj.transform.localScale;
                connectorObj.transform.localScale = new Vector3(baseScale.x, verticalSpan, baseScale.z);
                // Position the connector between the two levels
                float midY = Mathf.Min(baseLevelY, topLevelY) + verticalSpan / 2f;
                connectorObj.transform.position = new Vector3(conn.position.x * cellSize, midY, conn.position.z * cellSize);
                // (Optional orientation for ramps – e.g., tilt on X-axis if needed)
                if (conn.type == ConnectorType.StraightRamp || conn.type == ConnectorType.CurvedRamp || conn.type == ConnectorType.Stairs)
                {
                    connectorObj.transform.rotation = Quaternion.Euler(-45f, 0f, 0f);
                }
                connectorObj.transform.SetParent(connectorsParent.transform);
            }
        }

        //  Roof Tile Placement (Ceilings)
        if (addRoofs)
        {
            // Place a roof tile above every floor cell on lower levels (exclude topmost level)
            int topLevelIndex = layout.floors;
            foreach (var kvp in layout.floorCellsByLevel)
            {
                int level = kvp.Key;
                if (level == topLevelIndex) continue;  // Skip the topmost floor
                foreach (Vector2Int cell in kvp.Value)
                {
                    Vector3Int cellKey = new Vector3Int(cell.x, level, cell.y);
                    if (connectorSources.Contains(cellKey))
                    {
                        // Skip placing a roof if a stair/ramp goes up from this cell (leave opening for stairs)
                        continue;
                    }
                    // Instantiate roof prefab or placeholder
                    GameObject roofObj;
                    if (roofPrefab != null)
                        roofObj = GameObject.Instantiate(roofPrefab);
                    else
                        roofObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    roofObj.name = $"Roof_{level}_{cell.x}_{cell.y}";
                    // Scale to cover one cell (thin like floor)
                    roofObj.transform.localScale = new Vector3(cellSize, floorThickness, cellSize);
                    // Position at the ceiling height of this level (top of walls)
                    float baseY = level * wallHeight;
                    // Place roof at base of next level (baseY + wallHeight) minus half its thickness to sit flush
                    float roofY = baseY + wallHeight - floorThickness / 2f;
                    roofObj.transform.position = new Vector3(cell.x * cellSize, roofY, cell.y * cellSize);
                    roofObj.transform.SetParent(roofsParent.transform);
                }
            }
        }

        return dungeonRoot;
    }
}
