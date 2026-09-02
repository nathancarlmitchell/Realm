using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Realm.Data;

namespace Realm
{
    // Builds a DungeonMap from a TileSetData: random rooms connected by an
    // MST (Prim's) over their centers via L-shaped corridors, everything else
    // left as wall. Picks tile IDs by property (CanPassThrough) rather than a
    // hardcoded Wall/Floor pair, so a tileset with several wall/floor variants
    // gets free visual variety — same "pick randomly among same-category
    // options" principle as ItemSpawner.cs's own tier-drop rolls.
    public static class DungeonGenerator
    {
        // Algorithm-robustness knobs — padding/retry budget, not part of a
        // dungeon type's "personality" the way room size/count/corridor
        // width are, so these stay fixed engine constants rather than
        // DungeonTypeData fields.
        private const int EdgeBuffer = 1; // 1-tile gap kept clear at the world border.
        private const int RoomPadding = 1; // 1-tile gap required between any two rooms.
        private const int MaxPlacementAttempts = 500;

        public static DungeonMap Generate(
            TileSetData tileSet,
            int widthInTiles,
            int heightInTiles,
            int minRoomSize,
            int maxRoomSize,
            int minRoomCount,
            int maxRoomCount,
            int corridorWidth,
            int? seed = null
        )
        {
            Random rand = seed.HasValue ? new Random(seed.Value) : new Random();
            var map = new DungeonMap(tileSet, widthInTiles, heightInTiles);

            List<TileDefData> wallCandidates = tileSet.Tiles.Where(t => !t.CanPassThrough).ToList();
            List<TileDefData> floorCandidates = tileSet.Tiles.Where(t => t.CanPassThrough).ToList();

            // Util.LoadTileSetData() already guarantees both are non-empty for
            // any tileset loaded the normal way, but Generate() is public and
            // callable directly (e.g. tests) with a hand-built TileSetData —
            // fail the same clear way rather than crashing deeper below.
            if (wallCandidates.Count == 0)
                throw new InvalidOperationException(
                    "DungeonGenerator.Generate: tileset has no CanPassThrough-false (wall) tile."
                );
            if (floorCandidates.Count == 0)
                throw new InvalidOperationException(
                    "DungeonGenerator.Generate: tileset has no CanPassThrough (floor) tile."
                );

            // Start fully solid — free per-cell variety on the wall block
            // before anything is carved, rather than one repeated tile ID.
            for (int y = 0; y < heightInTiles; y++)
            for (int x = 0; x < widthInTiles; x++)
                map[x, y] = RandomPick(wallCandidates, rand).Id;

            List<Rectangle> rooms = PlaceRooms(
                map,
                widthInTiles,
                heightInTiles,
                minRoomSize,
                maxRoomSize,
                minRoomCount,
                maxRoomCount,
                floorCandidates,
                rand
            );

            if (rooms.Count > 1)
                ConnectRooms(map, rooms, corridorWidth, floorCandidates, rand);

            foreach (Rectangle room in rooms)
                map.Rooms.Add(room);

            return map;
        }

        private static List<Rectangle> PlaceRooms(
            DungeonMap map,
            int widthInTiles,
            int heightInTiles,
            int minRoomSize,
            int maxRoomSize,
            int minRoomCount,
            int maxRoomCount,
            List<TileDefData> floorCandidates,
            Random rand
        )
        {
            List<Rectangle> rooms = [];

            int maxFittableSize = Math.Min(maxRoomSize, Math.Min(widthInTiles, heightInTiles) - 2 * EdgeBuffer);
            if (maxFittableSize < minRoomSize)
                return rooms; // canvas too small to fit even one room — terminate with none.

            int targetRoomCount = rand.Next(minRoomCount, maxRoomCount + 1);
            int attempts = 0;

            while (rooms.Count < targetRoomCount && attempts < MaxPlacementAttempts)
            {
                attempts++;

                int w = rand.Next(minRoomSize, maxFittableSize + 1);
                int h = rand.Next(minRoomSize, maxFittableSize + 1);
                int x = rand.Next(EdgeBuffer, widthInTiles - EdgeBuffer - w + 1);
                int y = rand.Next(EdgeBuffer, heightInTiles - EdgeBuffer - h + 1);

                Rectangle candidate = new(x, y, w, h);
                Rectangle padded = new(
                    x - RoomPadding,
                    y - RoomPadding,
                    w + RoomPadding * 2,
                    h + RoomPadding * 2
                );

                if (rooms.Any(existing => padded.Intersects(existing)))
                    continue;

                rooms.Add(candidate);

                for (int cy = candidate.Top; cy < candidate.Bottom; cy++)
                for (int cx = candidate.Left; cx < candidate.Right; cx++)
                    map[cx, cy] = RandomPick(floorCandidates, rand).Id;
            }

            return rooms;
        }

        // Minimum spanning tree (Prim's) over room centers — guarantees every
        // room is reachable, with natural branching instead of one long
        // sequential chain, for about the same code.
        private static void ConnectRooms(
            DungeonMap map,
            List<Rectangle> rooms,
            int corridorWidth,
            List<TileDefData> floorCandidates,
            Random rand
        )
        {
            List<Point> centers = rooms.Select(r => new Point(r.Center.X, r.Center.Y)).ToList();
            HashSet<int> connected = new() { 0 };
            List<int> remaining = Enumerable.Range(1, centers.Count - 1).ToList();

            while (remaining.Count > 0)
            {
                int bestFrom = -1,
                    bestTo = -1;
                float bestDistSq = float.MaxValue;

                foreach (int from in connected)
                {
                    foreach (int to in remaining)
                    {
                        float dx = centers[from].X - centers[to].X;
                        float dy = centers[from].Y - centers[to].Y;
                        float distSq = dx * dx + dy * dy;
                        if (distSq < bestDistSq)
                        {
                            bestDistSq = distSq;
                            bestFrom = from;
                            bestTo = to;
                        }
                    }
                }

                CarveCorridor(map, centers[bestFrom], centers[bestTo], corridorWidth, floorCandidates, rand);
                connected.Add(bestTo);
                remaining.Remove(bestTo);
            }
        }

        // L-shaped, corridorWidth tiles wide, with the horizontal/vertical leg
        // order randomized per edge so corridors don't all bend the same way.
        private static void CarveCorridor(
            DungeonMap map,
            Point a,
            Point b,
            int corridorWidth,
            List<TileDefData> floorCandidates,
            Random rand
        )
        {
            if (rand.Next(2) == 0)
            {
                CarveHorizontal(map, a.X, b.X, a.Y, corridorWidth, floorCandidates, rand);
                CarveVertical(map, a.Y, b.Y, b.X, corridorWidth, floorCandidates, rand);
            }
            else
            {
                CarveVertical(map, a.Y, b.Y, a.X, corridorWidth, floorCandidates, rand);
                CarveHorizontal(map, a.X, b.X, b.Y, corridorWidth, floorCandidates, rand);
            }
        }

        private static void CarveHorizontal(
            DungeonMap map,
            int x1,
            int x2,
            int y,
            int corridorWidth,
            List<TileDefData> floorCandidates,
            Random rand
        )
        {
            int minX = Math.Min(x1, x2);
            int maxX = Math.Max(x1, x2);
            for (int x = minX; x <= maxX; x++)
            for (int dy = 0; dy < corridorWidth; dy++)
                map[x, y + dy] = RandomPick(floorCandidates, rand).Id;
        }

        private static void CarveVertical(
            DungeonMap map,
            int y1,
            int y2,
            int x,
            int corridorWidth,
            List<TileDefData> floorCandidates,
            Random rand
        )
        {
            int minY = Math.Min(y1, y2);
            int maxY = Math.Max(y1, y2);
            for (int y = minY; y <= maxY; y++)
            for (int dx = 0; dx < corridorWidth; dx++)
                map[x + dx, y] = RandomPick(floorCandidates, rand).Id;
        }

        private static TileDefData RandomPick(List<TileDefData> candidates, Random rand) =>
            candidates[rand.Next(candidates.Count)];
    }
}
