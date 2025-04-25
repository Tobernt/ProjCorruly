using UnityEngine;
using System.Collections.Generic;

public class RandomWalkLayoutGenerator : ILayoutGenerator
{
    public DungeonLayout GenerateLayout(DungeonGenSettings settings)
    {
        DungeonLayout layout = new DungeonLayout(1);  // single-floor layout
        int roomCount = settings.roomCount;
        int minSize = settings.minSize;
        int maxSize = settings.maxSize;
        bool allowCircle = settings.allowCircular;
        bool onlyRound = settings.onlyRound;
        bool allowOverlap = settings.allowOverlap;

        // Helper: create a room at origin (0,0) with given shape
        System.Func<int, int, bool, Room> createRoom = (cx, cy, circle) =>
        {
            Room room = new Room();
            room.id = layout.rooms.Count;
            room.level = 0;
            room.isCircular = circle;
            room.size = Vector2Int.zero;
            room.position = new Vector2Int(cx, cy);
            room.cells = new HashSet<Vector2Int>();
            if (!circle)
            {
                int w = Random.Range(minSize, maxSize + 1);
                int h = Random.Range(minSize, maxSize + 1);
                room.size = new Vector2Int(w, h);
                int halfW = w / 2;
                int halfH = h / 2;
                for (int dx = -halfW; dx < w - halfW; dx++)
                {
                    for (int dy = -halfH; dy < h - halfH; dy++)
                    {
                        room.cells.Add(new Vector2Int(cx + dx, cy + dy));
                    }
                }
            }
            else
            {
                int diameter = Random.Range(minSize, maxSize + 1);
                int radius = diameter / 2;
                room.size = new Vector2Int(diameter, diameter);
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (dx * dx + dy * dy <= radius * radius)
                        {
                            room.cells.Add(new Vector2Int(cx + dx, cy + dy));
                        }
                    }
                }
            }
            return room;
        };

        // Create the first room at (0,0)
        bool firstCircle = onlyRound ? true : (allowCircle && Random.value < 0.5f);
        Room firstRoom = createRoom(0, 0, firstCircle);
        layout.rooms.Add(firstRoom);
        // Add its cells to floor map
        foreach (Vector2Int cell in firstRoom.cells)
        {
            layout.floorCellsByLevel[0].Add(cell);
            layout.roomLookup[new Vector3Int(cell.x, firstRoom.level, cell.y)] = firstRoom.id;
        }

        // Iteratively add rooms by attaching to existing ones
        for (int i = 1; i < roomCount; i++)
        {
            bool placedRoom = false;
            Room newRoom = null;
            Room baseRoom = null;
            int dir = 0;
            Vector2Int attachCell = Vector2Int.zero;
            for (int attempt = 0; attempt < 10 && !placedRoom; attempt++)
            {
                baseRoom = layout.rooms[Random.Range(0, layout.rooms.Count)];
                dir = Random.Range(0, 4);
                // Choose an attach point on baseRoom edge (random cell on that edge)
                if (dir == 0)
                {
                    // North edge
                    int maxY = int.MinValue;
                    List<Vector2Int> edgeCells = new List<Vector2Int>();
                    foreach (var c in baseRoom.cells) { if (c.y > maxY) maxY = c.y; }
                    foreach (var c in baseRoom.cells) { if (c.y == maxY) edgeCells.Add(c); }
                    attachCell = edgeCells[Random.Range(0, edgeCells.Count)];
                }
                else if (dir == 2)
                {
                    // South edge
                    int minY = int.MaxValue;
                    List<Vector2Int> edgeCells = new List<Vector2Int>();
                    foreach (var c in baseRoom.cells) { if (c.y < minY) minY = c.y; }
                    foreach (var c in baseRoom.cells) { if (c.y == minY) edgeCells.Add(c); }
                    attachCell = edgeCells[Random.Range(0, edgeCells.Count)];
                }
                else if (dir == 1)
                {
                    // East edge
                    int maxX = int.MinValue;
                    List<Vector2Int> edgeCells = new List<Vector2Int>();
                    foreach (var c in baseRoom.cells) { if (c.x > maxX) maxX = c.x; }
                    foreach (var c in baseRoom.cells) { if (c.x == maxX) edgeCells.Add(c); }
                    attachCell = edgeCells[Random.Range(0, edgeCells.Count)];
                }
                else
                {
                    // West edge
                    int minX = int.MaxValue;
                    List<Vector2Int> edgeCells = new List<Vector2Int>();
                    foreach (var c in baseRoom.cells) { if (c.x < minX) minX = c.x; }
                    foreach (var c in baseRoom.cells) { if (c.x == minX) edgeCells.Add(c); }
                    attachCell = edgeCells[Random.Range(0, edgeCells.Count)];
                }

                int newCenterX = attachCell.x;
                int newCenterY = attachCell.y;
                bool newCircle = onlyRound ? true : (allowCircle && Random.value < 0.5f);
                newRoom = createRoom(0, 0, newCircle);
                int overlapAmount = 1;  // ensure at least one-cell contact
                Vector2Int offset = Vector2Int.zero;
                if (dir == 0)
                {
                    int baseTop = attachCell.y;
                    int newMinY = int.MaxValue;
                    foreach (var c in newRoom.cells) { if (c.y < newMinY) newMinY = c.y; }
                    offset = new Vector2Int(newCenterX, baseTop + overlapAmount - newMinY);
                }
                else if (dir == 2)
                {
                    int baseBottom = attachCell.y;
                    int newMaxY = int.MinValue;
                    foreach (var c in newRoom.cells) { if (c.y > newMaxY) newMaxY = c.y; }
                    offset = new Vector2Int(newCenterX, baseBottom - overlapAmount - newMaxY);
                }
                else if (dir == 1)
                {
                    int baseRight = attachCell.x;
                    int newMinX = int.MaxValue;
                    foreach (var c in newRoom.cells) { if (c.x < newMinX) newMinX = c.x; }
                    offset = new Vector2Int(baseRight + overlapAmount - newMinX, newCenterY);
                }
                else
                {
                    int baseLeft = attachCell.x;
                    int newMaxX = int.MinValue;
                    foreach (var c in newRoom.cells) { if (c.x > newMaxX) newMaxX = c.x; }
                    offset = new Vector2Int(baseLeft - overlapAmount - newMaxX, newCenterY);
                }
                // Translate new room by offset
                HashSet<Vector2Int> translatedCells = new HashSet<Vector2Int>();
                foreach (var c in newRoom.cells)
                {
                    translatedCells.Add(new Vector2Int(c.x + offset.x, c.y + offset.y));
                }
                newRoom.position = new Vector2Int(offset.x, offset.y);
                newRoom.cells = translatedCells;

                // Check overlap conflict with other rooms (not counting baseRoom)
                bool conflict = false;
                foreach (Vector2Int cell in newRoom.cells)
                {
                    Vector3Int cellKey = new Vector3Int(cell.x, newRoom.level, cell.y);
                    if (layout.roomLookup.ContainsKey(cellKey) && layout.roomLookup[cellKey] != baseRoom.id)
                    {
                        conflict = true;
                        break;
                    }
                }
                if (!allowOverlap && conflict)
                {
                    // try another attachment
                    continue;
                }

                // Place the new room
                newRoom.id = layout.rooms.Count;
                layout.rooms.Add(newRoom);
                foreach (Vector2Int cell in newRoom.cells)
                {
                    layout.floorCellsByLevel[0].Add(cell);
                    Vector3Int cellKey = new Vector3Int(cell.x, newRoom.level, cell.y);
                    if (!layout.roomLookup.ContainsKey(cellKey))
                    {
                        layout.roomLookup[cellKey] = newRoom.id;
                    }
                }
                // Record connector between baseRoom and newRoom
                Connector door = new Connector();
                door.type = ConnectorType.Doorway;
                door.fromRoomId = baseRoom.id;
                door.toRoomId = newRoom.id;
                door.fromLevel = 0;
                door.toLevel = 0;
                Vector3 doorPosWorld = new Vector3(attachCell.x, 0, attachCell.y);
                if (dir == 0) doorPosWorld = new Vector3(attachCell.x, 0, attachCell.y + overlapAmount);
                if (dir == 2) doorPosWorld = new Vector3(attachCell.x, 0, attachCell.y - overlapAmount);
                if (dir == 1) doorPosWorld = new Vector3(attachCell.x + overlapAmount, 0, attachCell.y);
                if (dir == 3) doorPosWorld = new Vector3(attachCell.x - overlapAmount, 0, attachCell.y);
                door.position = doorPosWorld;
                if (dir == 0) door.normal = Vector3.forward;
                if (dir == 2) door.normal = Vector3.back;
                if (dir == 1) door.normal = Vector3.right;
                if (dir == 3) door.normal = Vector3.left;
                layout.connectors.Add(door);
                placedRoom = true;
            }
            if (!placedRoom && newRoom != null)
            {
                // Could not place after attempts, place anyway
                newRoom.id = layout.rooms.Count;
                layout.rooms.Add(newRoom);
                foreach (Vector2Int cell in newRoom.cells)
                {
                    layout.floorCellsByLevel[0].Add(cell);
                    if (!layout.roomLookup.ContainsKey(new Vector3Int(cell.x, newRoom.level, cell.y)))
                    {
                        layout.roomLookup[new Vector3Int(cell.x, newRoom.level, cell.y)] = newRoom.id;
                    }
                }
                // Connector for forced placement
                Connector door = new Connector();
                door.type = ConnectorType.Doorway;
                door.fromRoomId = baseRoom != null ? baseRoom.id : -1;
                door.toRoomId = newRoom.id;
                door.fromLevel = 0;
                door.toLevel = 0;
                Vector3 dpw = new Vector3(attachCell.x, 0, attachCell.y);
                if (dir == 0) dpw = new Vector3(attachCell.x, 0, attachCell.y + 1);
                if (dir == 2) dpw = new Vector3(attachCell.x, 0, attachCell.y - 1);
                if (dir == 1) dpw = new Vector3(attachCell.x + 1, 0, attachCell.y);
                if (dir == 3) dpw = new Vector3(attachCell.x - 1, 0, attachCell.y);
                door.position = dpw;
                if (dir == 0) door.normal = Vector3.forward;
                if (dir == 2) door.normal = Vector3.back;
                if (dir == 1) door.normal = Vector3.right;
                if (dir == 3) door.normal = Vector3.left;
                layout.connectors.Add(door);
            }
        }

        return layout;
    }
}
