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
    //
    // Optional "cove" mode (sandFloorTileName/woodFloorTileName/
    // backgroundTileName/pathGapChance below — first real use: Pirate Cave,
    // matching its own wiki lore of "a network of wooden planks around the
    // cove"): each room is randomly typed Sand or Wood and carved uniformly
    // to that one tile instead of a per-cell random floor pick, corridors are
    // carved as a wood walkway instead of generic floor, the background fill
    // is a specific tile (water) instead of a random wall, and any wood
    // placement has a small chance of being a "missing plank" (background
    // tile) instead. All four are optional and independent — a dungeon type
    // that sets none of them (e.g. Snake Pit) gets the original behavior
    // unchanged.
    public static class DungeonGenerator
    {
        // Algorithm-robustness knobs — padding/retry budget, not part of a
        // dungeon type's "personality" the way room size/count/corridor
        // width are, so these stay fixed engine constants rather than
        // DungeonTypeData fields.
        private const int EdgeBuffer = 1; // 1-tile gap kept clear at the world border.
        private const int RoomPadding = 3; // 1-tile gap required between any two rooms.
        private const int MaxPlacementAttempts = 500;

        // Resolved cove-mode tiles, bundled so PlaceRooms()/ConnectRooms()/
        // CarveCorridor()/CarveHorizontal()/CarveVertical() don't each need
        // four more loose parameters. null fields mean "cove mode doesn't
        // apply to this aspect — fall back to the original behavior."
        private readonly record struct CoveOptions(
            TileDefData SandFloorTile,
            TileDefData WoodFloorTile,
            TileDefData BackgroundTile,
            float PathGapChance
        );

        // Resolved room-interior scatter tiles, same "bundle the optional
        // extras" reasoning as CoveOptions above — first real use: Sprite
        // World's conveyor belts (DungeonTypeData.ConveyorTileChance/
        // ConveyorTileNames) and Sprite Trees (ObstacleTileChance/
        // ObstacleTileName). Empty ConveyorTiles / null ObstacleTile means
        // "off," same "empty/null = feature off" contract every other
        // optional tile-name field already has.
        private readonly record struct ScatterOptions(
            List<TileDefData> ConveyorTiles,
            float ConveyorChance,
            TileDefData ObstacleTile,
            float ObstacleChance
        );

        public static DungeonMap Generate(
            TileSetData tileSet,
            int widthInTiles,
            int heightInTiles,
            int minRoomSize,
            int maxRoomSize,
            int minRoomCount,
            int maxRoomCount,
            int corridorWidth,
            string sandFloorTileName = null,
            string woodFloorTileName = null,
            string backgroundTileName = null,
            float pathGapChance = 0f,
            bool circularRooms = false,
            string corridorTileName = null,
            float conveyorTileChance = 0f,
            string[] conveyorTileNames = null,
            float obstacleTileChance = 0f,
            string obstacleTileName = null,
            int? seed = null
        )
        {
            Random rand = seed.HasValue ? new Random(seed.Value) : new Random();
            var map = new DungeonMap(tileSet, widthInTiles, heightInTiles);

            List<TileDefData> wallCandidates = tileSet
                .Tiles.Where(t => !t.CanPassThrough && !t.ExcludeFromBackgroundFill)
                .ToList();
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

            CoveOptions cove = new(
                ResolveTileByName(tileSet, sandFloorTileName),
                ResolveTileByName(tileSet, woodFloorTileName),
                ResolveTileByName(tileSet, backgroundTileName),
                pathGapChance
            );

            ScatterOptions scatter = new(
                (conveyorTileNames ?? [])
                    .Select(name => ResolveTileByName(tileSet, name))
                    .ToList(),
                conveyorTileChance,
                ResolveTileByName(tileSet, obstacleTileName),
                obstacleTileChance
            );

            // Resolved once here rather than threaded through as a raw
            // string — null means "corridors behave exactly as before" (the
            // cove walkway above, or a random floor pick), same "null =
            // feature off" contract every other optional tile name already
            // has.
            TileDefData corridorTile = ResolveTileByName(tileSet, corridorTileName);

            // Start fully solid (or, in cove mode, fully background/water) —
            // free per-cell variety on the non-cove wall block, since a
            // single background tile is deliberately uniform.
            for (int y = 0; y < heightInTiles; y++)
            for (int x = 0; x < widthInTiles; x++)
                map[x, y] = cove.BackgroundTile?.Id ?? RandomPick(wallCandidates, rand).Id;

            List<Rectangle> rooms = PlaceRooms(
                map,
                widthInTiles,
                heightInTiles,
                minRoomSize,
                maxRoomSize,
                minRoomCount,
                maxRoomCount,
                floorCandidates,
                cove,
                circularRooms,
                scatter,
                rand
            );

            if (rooms.Count > 1)
                ConnectRooms(
                    map,
                    rooms,
                    corridorWidth,
                    floorCandidates,
                    cove,
                    circularRooms,
                    corridorTile,
                    rand
                );

            foreach (Rectangle room in rooms)
                map.Rooms.Add(room);

            return map;
        }

        // Whether world cell (x, y) falls inside `room`'s own footprint —
        // the whole rectangle when circularRooms is false (today's original
        // behavior), or the circle inscribed within it (diameter =
        // room.Width, always square-bounded — see PlaceRooms() below) when
        // true. Shared by room carving itself and by corridor carving's own
        // "don't stamp a hallway tile over a room's own floor" check.
        private static bool RoomContains(Rectangle room, bool circularRooms, int x, int y)
        {
            if (!circularRooms)
                return room.Contains(x, y);

            float radius = room.Width / 2f;
            Vector2 center = new(room.Center.X, room.Center.Y);
            Vector2 cell = new(x + 0.5f, y + 0.5f);
            return Vector2.DistanceSquared(cell, center) <= radius * radius;
        }

        private static bool IsInsideAnyRoom(List<Rectangle> rooms, bool circularRooms, int x, int y)
        {
            foreach (Rectangle room in rooms)
                if (RoomContains(room, circularRooms, x, y))
                    return true;
            return false;
        }

        private static TileDefData ResolveTileByName(TileSetData tileSet, string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            TileDefData tile = tileSet.Tiles.FirstOrDefault(t => t.Name == name);
            if (tile == null)
                throw new InvalidOperationException(
                    $"DungeonGenerator.Generate: tileset '{tileSet.Name}' has no tile named '{name}'."
                );

            return tile;
        }

        // Picks tile.Id, except a WoodFloorTile has a PathGapChance chance of
        // becoming BackgroundTile instead — a "missing plank" over water.
        // Only ever applies to the wood tile specifically, not sand — a gap
        // is still fully walkable (water only slows the player, never
        // blocks), so this never risks disconnecting a room.
        private static int PlaceCoveTile(TileDefData tile, CoveOptions cove, Random rand)
        {
            if (
                tile == cove.WoodFloorTile
                && cove.BackgroundTile != null
                && rand.NextDouble() < cove.PathGapChance
            )
                return cove.BackgroundTile.Id;

            return tile.Id;
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
            CoveOptions cove,
            bool circularRooms,
            ScatterOptions scatter,
            Random rand
        )
        {
            List<Rectangle> rooms = [];

            int maxFittableSize = Math.Min(
                maxRoomSize,
                Math.Min(widthInTiles, heightInTiles) - 2 * EdgeBuffer
            );
            if (maxFittableSize < minRoomSize)
                return rooms; // canvas too small to fit even one room — terminate with none.

            int targetRoomCount = rand.Next(minRoomCount, maxRoomCount + 1);
            int attempts = 0;

            while (rooms.Count < targetRoomCount && attempts < MaxPlacementAttempts)
            {
                attempts++;

                // circularRooms: w/h always match (a square bounding box),
                // so the circle inscribed within it (see RoomContains()
                // above) is a true circle, not an ellipse — MinRoomSize/
                // MaxRoomSize become "diameter" range in this mode, the same
                // fields reused rather than adding a separate name for them.
                int w,
                    h;
                if (circularRooms)
                {
                    w = h = rand.Next(minRoomSize, maxFittableSize + 1);
                }
                else
                {
                    w = rand.Next(minRoomSize, maxFittableSize + 1);
                    h = rand.Next(minRoomSize, maxFittableSize + 1);
                }

                int x = rand.Next(EdgeBuffer, widthInTiles - EdgeBuffer - w + 1);
                int y = rand.Next(EdgeBuffer, heightInTiles - EdgeBuffer - h + 1);

                Rectangle candidate = new(x, y, w, h);

                // Circular: overlap is a center-to-center distance check
                // against the sum of both radii (plus padding) instead of a
                // padded-rectangle intersection — a bounding-box overlap
                // check would reject plenty of candidates whose actual
                // circles don't overlap at all (two circles' bounding
                // squares touch at the corners well before the circles
                // themselves do).
                bool overlaps;
                if (circularRooms)
                {
                    float newRadius = w / 2f;
                    Vector2 newCenter = new(candidate.Center.X, candidate.Center.Y);
                    overlaps = rooms.Any(existing =>
                    {
                        float existingRadius = existing.Width / 2f;
                        Vector2 existingCenter = new(existing.Center.X, existing.Center.Y);
                        float minDist = newRadius + existingRadius + RoomPadding;
                        return Vector2.DistanceSquared(newCenter, existingCenter)
                            < minDist * minDist;
                    });
                }
                else
                {
                    Rectangle padded = new(
                        x - RoomPadding,
                        y - RoomPadding,
                        w + RoomPadding * 2,
                        h + RoomPadding * 2
                    );
                    overlaps = rooms.Any(existing => padded.Intersects(existing));
                }

                if (overlaps)
                    continue;

                rooms.Add(candidate);

                // Each room is typed once (not per cell) — "some rooms will
                // be only sand, some rooms will be wood textured." Falls
                // back to the original per-cell random floorCandidate pick
                // when cove mode isn't set up for either tile.
                TileDefData roomTile = (cove.SandFloorTile, cove.WoodFloorTile) switch
                {
                    (not null, not null) => rand.Next(2) == 0
                        ? cove.WoodFloorTile
                        : cove.SandFloorTile,
                    (not null, null) => cove.SandFloorTile,
                    (null, not null) => cove.WoodFloorTile,
                    _ => null,
                };

                for (int cy = candidate.Top; cy < candidate.Bottom; cy++)
                for (int cx = candidate.Left; cx < candidate.Right; cx++)
                {
                    if (!RoomContains(candidate, circularRooms, cx, cy))
                        continue; // outside the inscribed circle — leave as background/wall.

                    // Sprite Trees checked before the conveyor roll — see
                    // DungeonTypeData.ObstacleTileChance's own comment for
                    // why the destructible obstacle wins a same-cell tie
                    // over a directional push tile. Both independent of
                    // cove mode (unlike roomTile above), and both no-ops
                    // (their tiles are null / chances 0) for every dungeon
                    // type that doesn't set them.
                    if (
                        scatter.ObstacleTile != null
                        && rand.NextDouble() < scatter.ObstacleChance
                    )
                    {
                        map[cx, cy] = scatter.ObstacleTile.Id;
                    }
                    else if (
                        scatter.ConveyorTiles.Count > 0
                        && rand.NextDouble() < scatter.ConveyorChance
                    )
                    {
                        map[cx, cy] = RandomPick(scatter.ConveyorTiles, rand).Id;
                    }
                    else
                    {
                        map[cx, cy] =
                            roomTile != null
                                ? PlaceCoveTile(roomTile, cove, rand)
                                : RandomPick(floorCandidates, rand).Id;
                    }
                }
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
            CoveOptions cove,
            bool circularRooms,
            TileDefData corridorTile,
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

                CarveCorridor(
                    map,
                    centers[bestFrom],
                    centers[bestTo],
                    corridorWidth,
                    floorCandidates,
                    cove,
                    rooms,
                    circularRooms,
                    corridorTile,
                    rand
                );
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
            CoveOptions cove,
            List<Rectangle> rooms,
            bool circularRooms,
            TileDefData corridorTile,
            Random rand
        )
        {
            if (rand.Next(2) == 0)
            {
                CarveHorizontal(
                    map,
                    a.X,
                    b.X,
                    a.Y,
                    corridorWidth,
                    floorCandidates,
                    cove,
                    rooms,
                    circularRooms,
                    corridorTile,
                    rand
                );
                CarveVertical(
                    map,
                    a.Y,
                    b.Y,
                    b.X,
                    corridorWidth,
                    floorCandidates,
                    cove,
                    rooms,
                    circularRooms,
                    corridorTile,
                    rand
                );
            }
            else
            {
                CarveVertical(
                    map,
                    a.Y,
                    b.Y,
                    a.X,
                    corridorWidth,
                    floorCandidates,
                    cove,
                    rooms,
                    circularRooms,
                    corridorTile,
                    rand
                );
                CarveHorizontal(
                    map,
                    a.X,
                    b.X,
                    b.Y,
                    corridorWidth,
                    floorCandidates,
                    cove,
                    rooms,
                    circularRooms,
                    corridorTile,
                    rand
                );
            }
        }

        // corridorTile set: every cell gets that tile (e.g. a breakable
        // wall — see DungeonTypeData.CorridorTileName's own doc comment),
        // except cells that already fall inside some room's own footprint
        // (RoomContains/IsInsideAnyRoom) — those are left untouched rather
        // than stamped over, since a corridor's straight-line path always
        // starts and ends inside a room (and can graze a third room along
        // the way — see docs/DEVLOG.md entry 315), and overwriting a room's
        // own already-carved floor with a hallway-fill tile would leave a
        // visible, wrong-looking block of "rubble" sitting inside an
        // otherwise open room.
        private static void CarveHorizontal(
            DungeonMap map,
            int x1,
            int x2,
            int y,
            int corridorWidth,
            List<TileDefData> floorCandidates,
            CoveOptions cove,
            List<Rectangle> rooms,
            bool circularRooms,
            TileDefData corridorTile,
            Random rand
        )
        {
            int minX = Math.Min(x1, x2);
            int maxX = Math.Max(x1, x2);
            for (int x = minX; x <= maxX; x++)
            for (int dy = 0; dy < corridorWidth; dy++)
            {
                int cy = y + dy;
                if (corridorTile != null)
                {
                    if (!IsInsideAnyRoom(rooms, circularRooms, x, cy))
                        map[x, cy] = corridorTile.Id;
                }
                else
                {
                    map[x, cy] =
                        cove.WoodFloorTile != null
                            ? PlaceCoveTile(cove.WoodFloorTile, cove, rand)
                            : RandomPick(floorCandidates, rand).Id;
                }
            }
        }

        private static void CarveVertical(
            DungeonMap map,
            int y1,
            int y2,
            int x,
            int corridorWidth,
            List<TileDefData> floorCandidates,
            CoveOptions cove,
            List<Rectangle> rooms,
            bool circularRooms,
            TileDefData corridorTile,
            Random rand
        )
        {
            int minY = Math.Min(y1, y2);
            int maxY = Math.Max(y1, y2);
            for (int y = minY; y <= maxY; y++)
            for (int dx = 0; dx < corridorWidth; dx++)
            {
                int cx = x + dx;
                if (corridorTile != null)
                {
                    if (!IsInsideAnyRoom(rooms, circularRooms, cx, y))
                        map[cx, y] = corridorTile.Id;
                }
                else
                {
                    map[cx, y] =
                        cove.WoodFloorTile != null
                            ? PlaceCoveTile(cove.WoodFloorTile, cove, rand)
                            : RandomPick(floorCandidates, rand).Id;
                }
            }
        }

        private static TileDefData RandomPick(List<TileDefData> candidates, Random rand) =>
            candidates[rand.Next(candidates.Count)];
    }
}
