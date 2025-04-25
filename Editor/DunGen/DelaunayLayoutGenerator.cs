using UnityEngine;
using System.Collections.Generic;
using System;

public class DelaunayLayoutGenerator : ILayoutGenerator
{
    public DungeonLayout GenerateLayout(DungeonGenSettings settings)
    {
        DungeonLayout layout = new DungeonLayout(1);
        int roomCount = settings.roomCount;
        int minSize = settings.minSize;
        int maxSize = settings.maxSize;
        bool allowCircle = settings.allowCircular;
        bool onlyRound = settings.onlyRound;
        bool allowOverlap = settings.allowOverlap;

        System.Random prng = new System.Random();  // deterministic random for placement
        List<Room> rooms = new List<Room>();

        // Helper to create a room at given center
        Func<int, int, bool, Room> createRoomAt = (cx, cy, circle) =>
        {
            Room room = new Room();
            room.id = rooms.Count;
            room.level = 0;
            room.isCircular = circle;
            room.position = new Vector2Int(cx, cy);
            room.cells = new HashSet<Vector2Int>();
            if (!circle)
            {
                int w = UnityEngine.Random.Range(minSize, maxSize + 1);
                int h = UnityEngine.Random.Range(minSize, maxSize + 1);
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
                int diameter = UnityEngine.Random.Range(minSize, maxSize + 1);
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

        // Randomly scatter rooms in a bounding area, avoiding overlaps if required
        float spreadRadius = roomCount * ((minSize + maxSize) / 2f) * 0.5f;
        for (int i = 0; i < roomCount; i++)
        {
            int attempt = 0;
            Room newRoom = null;
            bool placed = false;
            while (!placed && attempt < 50)
            {
                int cx = (int)Math.Round((prng.NextDouble() * 2 - 1) * spreadRadius);
                int cy = (int)Math.Round((prng.NextDouble() * 2 - 1) * spreadRadius);

                // Determine shape based on settings
                bool makeCircle = onlyRound ? true : (allowCircle && UnityEngine.Random.value < 0.5f);

                newRoom = createRoomAt(cx, cy, makeCircle);
                bool overlaps = false;
                foreach (Room r in rooms)
                {
                    Vector2 diff = (Vector2)(newRoom.position - r.position);
                    float distSqr = diff.sqrMagnitude;
                    float r1 = 0.5f * Mathf.Sqrt(newRoom.size.x * newRoom.size.x + newRoom.size.y * newRoom.size.y);
                    float r2 = 0.5f * Mathf.Sqrt(r.size.x * r.size.x + r.size.y * r.size.y);
                    if (distSqr < (r1 + r2 + 0.5f) * (r1 + r2 + 0.5f))
                    {
                        overlaps = true;
                        break;
                    }
                }
                if (allowOverlap || !overlaps)
                {
                    placed = true;
                    rooms.Add(newRoom);
                }
                attempt++;
            }
            if (!placed && newRoom != null)
            {
                // Could not find non-overlapping spot, place anyway
                rooms.Add(newRoom);
            }
        }

        // Add all room floor cells to layout
        foreach (Room room in rooms)
        {
            layout.rooms.Add(room);
            foreach (Vector2Int cell in room.cells)
            {
                layout.floorCellsByLevel[0].Add(cell);
                Vector3Int key = new Vector3Int(cell.x, room.level, cell.y);
                if (!layout.roomLookup.ContainsKey(key))
                    layout.roomLookup[key] = room.id;
            }
        }

        // Compute MST on room centers for minimal connector paths
        int n = rooms.Count;
        List<(float dist, int i, int j)> edges = new List<(float, int, int)>();
        for (int i = 0; i < n; i++)
        {
            Vector2 pi = (Vector2)rooms[i].position;
            for (int j = i + 1; j < n; j++)
            {
                Vector2 pj = (Vector2)rooms[j].position;
                float d2 = (pi - pj).sqrMagnitude;
                edges.Add((d2, i, j));
            }
        }
        edges.Sort((a, b) => a.dist.CompareTo(b.dist));
        int[] ufParent = new int[n];
        for (int i = 0; i < n; i++) ufParent[i] = i;
        Func<int, int> find = null;
        find = u => (ufParent[u] == u ? u : (ufParent[u] = find(ufParent[u])));
        Action<int, int> unite = (u, v) => { ufParent[find(u)] = find(v); };

        List<(int, int)> connections = new List<(int, int)>();
        foreach (var edge in edges)
        {
            int i = edge.i, j = edge.j;
            if (find(i) != find(j))
            {
                unite(i, j);
                connections.Add((i, j));
            }
            if (connections.Count == n - 1) break;
        }
        // Add extra connections for looping paths (randomly add some short edges)
        HashSet<(int, int)> addedEdges = new HashSet<(int, int)>(connections);
        foreach (var edge in edges)
        {
            int i = edge.i, j = edge.j;
            if (!addedEdges.Contains((i, j)) && UnityEngine.Random.value < 0.1f)
            {
                connections.Add((i, j));
                addedEdges.Add((i, j));
            }
        }

        // Carve corridors for each connection (short L-shaped path via nearest edges)
        foreach ((int a, int b) in connections)
        {
            Room roomA = rooms[a];
            Room roomB = rooms[b];
            // Determine attachment points on room edges for smoother corridor
            Vector2Int attachA, attachB;
            // Compute room boundary extents
            int A_minX = int.MaxValue, A_maxX = int.MinValue, A_minY = int.MaxValue, A_maxY = int.MinValue;
            foreach (Vector2Int cell in roomA.cells)
            {
                if (cell.x < A_minX) A_minX = cell.x;
                if (cell.x > A_maxX) A_maxX = cell.x;
                if (cell.y < A_minY) A_minY = cell.y;
                if (cell.y > A_maxY) A_maxY = cell.y;
            }
            int B_minX = int.MaxValue, B_maxX = int.MinValue, B_minY = int.MaxValue, B_maxY = int.MinValue;
            foreach (Vector2Int cell in roomB.cells)
            {
                if (cell.x < B_minX) B_minX = cell.x;
                if (cell.x > B_maxX) B_maxX = cell.x;
                if (cell.y < B_minY) B_minY = cell.y;
                if (cell.y > B_maxY) B_maxY = cell.y;
            }
            bool horizontalFirst = Mathf.Abs(roomB.position.x - roomA.position.x) >= Mathf.Abs(roomB.position.y - roomA.position.y);
            if (horizontalFirst)
            {
                if (roomB.position.x >= roomA.position.x)
                {
                    // East from A to West of B
                    List<Vector2Int> A_edgeCells = new List<Vector2Int>();
                    foreach (Vector2Int c in roomA.cells) if (c.x == A_maxX) A_edgeCells.Add(c);
                    attachA = A_edgeCells[0];
                    foreach (Vector2Int c in A_edgeCells)
                        if (Mathf.Abs(c.y - roomB.position.y) < Mathf.Abs(attachA.y - roomB.position.y))
                            attachA = c;
                    List<Vector2Int> B_edgeCells = new List<Vector2Int>();
                    foreach (Vector2Int c in roomB.cells) if (c.x == B_minX) B_edgeCells.Add(c);
                    attachB = B_edgeCells[0];
                    foreach (Vector2Int c in B_edgeCells)
                        if (Mathf.Abs(c.y - attachA.y) < Mathf.Abs(attachB.y - attachA.y))
                            attachB = c;
                }
                else
                {
                    // West from A to East of B
                    List<Vector2Int> A_edgeCells = new List<Vector2Int>();
                    foreach (Vector2Int c in roomA.cells) if (c.x == A_minX) A_edgeCells.Add(c);
                    attachA = A_edgeCells[0];
                    foreach (Vector2Int c in A_edgeCells)
                        if (Mathf.Abs(c.y - roomB.position.y) < Mathf.Abs(attachA.y - roomB.position.y))
                            attachA = c;
                    List<Vector2Int> B_edgeCells = new List<Vector2Int>();
                    foreach (Vector2Int c in roomB.cells) if (c.x == B_maxX) B_edgeCells.Add(c);
                    attachB = B_edgeCells[0];
                    foreach (Vector2Int c in B_edgeCells)
                        if (Mathf.Abs(c.y - attachA.y) < Mathf.Abs(attachB.y - attachA.y))
                            attachB = c;
                }
            }
            else
            {
                if (roomB.position.y >= roomA.position.y)
                {
                    // North from A to South of B
                    List<Vector2Int> A_edgeCells = new List<Vector2Int>();
                    foreach (Vector2Int c in roomA.cells) if (c.y == A_maxY) A_edgeCells.Add(c);
                    attachA = A_edgeCells[0];
                    foreach (Vector2Int c in A_edgeCells)
                        if (Mathf.Abs(c.x - roomB.position.x) < Mathf.Abs(attachA.x - roomB.position.x))
                            attachA = c;
                    List<Vector2Int> B_edgeCells = new List<Vector2Int>();
                    foreach (Vector2Int c in roomB.cells) if (c.y == B_minY) B_edgeCells.Add(c);
                    attachB = B_edgeCells[0];
                    foreach (Vector2Int c in B_edgeCells)
                        if (Mathf.Abs(c.x - attachA.x) < Mathf.Abs(attachB.x - attachA.x))
                            attachB = c;
                }
                else
                {
                    // South from A to North of B
                    List<Vector2Int> A_edgeCells = new List<Vector2Int>();
                    foreach (Vector2Int c in roomA.cells) if (c.y == A_minY) A_edgeCells.Add(c);
                    attachA = A_edgeCells[0];
                    foreach (Vector2Int c in A_edgeCells)
                        if (Mathf.Abs(c.x - roomB.position.x) < Mathf.Abs(attachA.x - roomB.position.x))
                            attachA = c;
                    List<Vector2Int> B_edgeCells = new List<Vector2Int>();
                    foreach (Vector2Int c in roomB.cells) if (c.y == B_maxY) B_edgeCells.Add(c);
                    attachB = B_edgeCells[0];
                    foreach (Vector2Int c in B_edgeCells)
                        if (Mathf.Abs(c.x - attachA.x) < Mathf.Abs(attachB.x - attachA.x))
                            attachB = c;
                }
            }
            // Carve corridor path
            if (horizontalFirst)
            {
                // Horizontal segment
                int startX = attachA.x;
                int endX = attachB.x;
                int cy = attachA.y;
                if (startX <= endX)
                {
                    for (int x = startX; x <= endX; x++)
                        layout.floorCellsByLevel[0].Add(new Vector2Int(x, cy));
                }
                else
                {
                    for (int x = startX; x >= endX; x--)
                        layout.floorCellsByLevel[0].Add(new Vector2Int(x, cy));
                }
                // Vertical segment
                int cx = attachB.x;
                if (attachA.y <= attachB.y)
                {
                    for (int y = attachA.y; y <= attachB.y; y++)
                        layout.floorCellsByLevel[0].Add(new Vector2Int(cx, y));
                }
                else
                {
                    for (int y = attachA.y; y >= attachB.y; y--)
                        layout.floorCellsByLevel[0].Add(new Vector2Int(cx, y));
                }
            }
            else
            {
                // Vertical segment first
                int startY = attachA.y;
                int endY = attachB.y;
                int cx = attachA.x;
                if (startY <= endY)
                {
                    for (int y = startY; y <= endY; y++)
                        layout.floorCellsByLevel[0].Add(new Vector2Int(cx, y));
                }
                else
                {
                    for (int y = startY; y >= endY; y--)
                        layout.floorCellsByLevel[0].Add(new Vector2Int(cx, y));
                }
                // Horizontal segment
                int cy = attachB.y;
                if (attachA.x <= attachB.x)
                {
                    for (int x = attachA.x; x <= attachB.x; x++)
                        layout.floorCellsByLevel[0].Add(new Vector2Int(x, cy));
                }
                else
                {
                    for (int x = attachA.x; x >= attachB.x; x--)
                        layout.floorCellsByLevel[0].Add(new Vector2Int(x, cy));
                }
            }
            // Record a connector between room A and B
            Connector door = new Connector();
            door.type = ConnectorType.Doorway;
            door.fromRoomId = roomA.id;
            door.toRoomId = roomB.id;
            door.fromLevel = 0;
            door.toLevel = 0;
            door.position = new Vector3((attachA.x + attachB.x) / 2f, 0, (attachA.y + attachB.y) / 2f);
            if (horizontalFirst)
                door.normal = (roomB.position.x >= roomA.position.x ? Vector3.right : Vector3.left);
            else
                door.normal = (roomB.position.y >= roomA.position.y ? Vector3.forward : Vector3.back);
            layout.connectors.Add(door);
        }

        return layout;
    }
}
