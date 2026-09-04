using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm.Data;

namespace Realm
{
    // A generated (see DungeonGenerator.cs) grid of tile IDs plus the
    // TileSetData catalog that gives each ID its meaning — the walled-dungeon
    // feature's whole notion of "the level." Stateless collision/lookup
    // methods here are called from both the player and every enemy (see
    // DungeonState.Update()), so there's exactly one place this logic lives.
    public class DungeonMap
    {
        public TileSetData TileSet { get; }
        public int WidthInTiles { get; }
        public int HeightInTiles { get; }

        public int WorldWidth => WidthInTiles * TileSet.TileWidth;
        public int WorldHeight => HeightInTiles * TileSet.TileHeight;

        // Tile-space rectangles carved by DungeonGenerator — Rooms[0] is
        // always the start room (see DungeonState's constructor).
        public List<Rectangle> Rooms { get; } = [];

        private readonly int[,] tiles;

        // Returned by TileAt() for any out-of-bounds lookup — solid and
        // impassable, so a caller never needs its own bounds check just to
        // stay safe near a map edge.
        private static readonly TileDefData OutOfBoundsTile = new()
        {
            Id = -1,
            Name = "Out Of Bounds",
            CanPassThrough = false,
            CanShootThrough = false,
        };

        private readonly Dictionary<int, TileDefData> tilesById;

        // Every CanPassThrough tile in this tileset, excluding conveyors —
        // computed once here rather than per-call, since DamageTile() below
        // needs it every time a destructible tile breaks (picks a random
        // one to become, same "random among same-category candidates"
        // principle DungeonGenerator already uses when carving). A
        // conveyor tile is CanPassThrough too, but must never be picked
        // here — a broken Sprite Trees obstacle turning into a stray,
        // ungrouped conveyor tile would be the exact same leak
        // DungeonGenerator.Generate()'s own floorCandidates filter (see its
        // comment) was fixed to close, just reached through tile
        // destruction instead of initial generation.
        private readonly List<TileDefData> floorCandidates;

        // Remaining hit points for a destructible tile that's taken at least
        // one hit — absent (not zero) means "still at its full starting
        // DestructibleHealth," so a tile that's never been hit costs no
        // memory here. Removed once a tile breaks (see DamageTile()).
        private readonly Dictionary<Point, int> destructibleHealthRemaining = [];

        private readonly Random rand = new();

        public DungeonMap(TileSetData tileSet, int widthInTiles, int heightInTiles)
        {
            TileSet = tileSet;
            WidthInTiles = widthInTiles;
            HeightInTiles = heightInTiles;
            tiles = new int[widthInTiles, heightInTiles];

            tilesById = new Dictionary<int, TileDefData>();
            foreach (TileDefData tile in tileSet.Tiles)
            {
                tilesById[tile.Id] = tile;
            }

            floorCandidates = tileSet
                .Tiles.Where(t => t.CanPassThrough && t.ConveyorSpeed == 0)
                .ToList();
        }

        public int this[int x, int y]
        {
            get => InBounds(x, y) ? tiles[x, y] : OutOfBoundsTile.Id;
            set
            {
                if (InBounds(x, y))
                    tiles[x, y] = value;
            }
        }

        private bool InBounds(int x, int y) =>
            x >= 0 && x < WidthInTiles && y >= 0 && y < HeightInTiles;

        public TileDefData TileAt(int tileX, int tileY)
        {
            if (!InBounds(tileX, tileY))
                return OutOfBoundsTile;

            int id = tiles[tileX, tileY];
            return tilesById.TryGetValue(id, out TileDefData tile) ? tile : OutOfBoundsTile;
        }

        // World-space convenience — TileAt() above is tile-space.
        public TileDefData TileAtWorldPosition(Vector2 worldPosition) =>
            TileAt(
                (int)(worldPosition.X / TileSet.TileWidth),
                (int)(worldPosition.Y / TileSet.TileHeight)
            );

        // Applies projectile damage to a destructible tile — a no-op (returns
        // false) for a tile that isn't IsDestructible. Once the tile's
        // DestructibleHealth is exhausted, it breaks: replaced in the grid by
        // a randomly-picked floor candidate (the same "random among
        // same-category candidates" principle DungeonGenerator already uses
        // when carving, so a tileset with several floor variants gets the
        // same free visual variety here too), returning true so the caller
        // (DungeonState) knows a break just happened.
        public bool DamageTile(int tileX, int tileY, int damage)
        {
            TileDefData tile = TileAt(tileX, tileY);
            if (!tile.IsDestructible)
                return false;

            Point cell = new(tileX, tileY);
            int remaining = destructibleHealthRemaining.GetValueOrDefault(
                cell,
                tile.DestructibleHealth
            );
            remaining -= damage;

            // Same DamageNumber/prefix/color/settings-gate convention as an
            // enemy's own hit number (Enemy.WasShot()) — "-" so it reads as
            // damage dealt, not a gain, and gated by the same toggle
            // (Settings > Graphics > "Show Enemy Damage Numbers") rather than
            // a new dedicated setting, since this is the same "damage the
            // player just dealt" moment, just aimed at a tile instead of an
            // enemy. World-space tile-center position, not raw tile
            // coordinates — a bare (tileX, tileY) would draw the number at a
            // handful of pixels from the world origin instead of on the tile
            // that was actually hit.
            if (Player.Instance.ShowEnemyDamageNumbersEnabled && damage != 0)
            {
                Vector2 tileCenter = new(
                    tileX * TileSet.TileWidth + TileSet.TileWidth / 2f,
                    tileY * TileSet.TileHeight + TileSet.TileHeight / 2f
                );
                EntityManager.Add(new DamageNumber(tileCenter, damage, Color.Red, prefix: "-"));
            }

            if (remaining > 0)
            {
                destructibleHealthRemaining[cell] = remaining;
                return false;
            }

            destructibleHealthRemaining.Remove(cell);
            this[tileX, tileY] = floorCandidates[rand.Next(floorCandidates.Count)].Id;
            return true;
        }

        // Pushes a circle (player or enemy) out of any overlapping
        // !CanPassThrough tile. Checks only the small tile neighborhood the
        // circle's bounding box can possibly overlap, and resolves each
        // overlap along whichever axis has the smaller penetration — the
        // standard "minimum translation vector" approach for circle-vs-AABB.
        // Multiple passes let a corner (overlapping two wall tiles at once)
        // settle into the actual gap between them instead of only resolving
        // one axis and still penetrating the other tile.
        public Vector2 ResolveCircleCollision(Vector2 position, float radius)
        {
            const int passes = 3;

            for (int pass = 0; pass < passes; pass++)
            {
                int minTileX = (int)MathF.Floor((position.X - radius) / TileSet.TileWidth);
                int maxTileX = (int)MathF.Floor((position.X + radius) / TileSet.TileWidth);
                int minTileY = (int)MathF.Floor((position.Y - radius) / TileSet.TileHeight);
                int maxTileY = (int)MathF.Floor((position.Y + radius) / TileSet.TileHeight);

                for (int ty = minTileY; ty <= maxTileY; ty++)
                {
                    for (int tx = minTileX; tx <= maxTileX; tx++)
                    {
                        TileDefData tile = TileAt(tx, ty);
                        if (tile.CanPassThrough)
                            continue;

                        Rectangle tileRect = new(
                            tx * TileSet.TileWidth,
                            ty * TileSet.TileHeight,
                            TileSet.TileWidth,
                            TileSet.TileHeight
                        );

                        position = PushOutOfRectangle(position, radius, tileRect);
                    }
                }
            }

            return position;
        }

        private static Vector2 PushOutOfRectangle(Vector2 position, float radius, Rectangle rect)
        {
            // Closest point on the rectangle to the circle's center.
            float closestX = MathHelper.Clamp(position.X, rect.Left, rect.Right);
            float closestY = MathHelper.Clamp(position.Y, rect.Top, rect.Bottom);

            float dx = position.X - closestX;
            float dy = position.Y - closestY;
            float distSq = dx * dx + dy * dy;

            if (distSq >= radius * radius)
                return position; // not actually overlapping this tile.

            if (distSq > 0f)
            {
                // Center is outside the rectangle — push straight out along
                // the vector from the closest edge point to the center.
                float dist = MathF.Sqrt(distSq);
                float push = radius - dist;
                return position + new Vector2(dx / dist, dy / dist) * push;
            }

            // Center is exactly on or inside the rectangle (fully embedded) —
            // push out along whichever axis needs the smallest correction.
            float pushLeft = position.X - rect.Left;
            float pushRight = rect.Right - position.X;
            float pushTop = position.Y - rect.Top;
            float pushBottom = rect.Bottom - position.Y;

            float min = MathF.Min(MathF.Min(pushLeft, pushRight), MathF.Min(pushTop, pushBottom));

            if (min == pushLeft)
                return new Vector2(rect.Left - radius, position.Y);
            if (min == pushRight)
                return new Vector2(rect.Right + radius, position.Y);
            if (min == pushTop)
                return new Vector2(position.X, rect.Top - radius);
            return new Vector2(position.X, rect.Bottom + radius);
        }

        // Draws only the tiles overlapping worldBounds (in world pixels) —
        // never the whole grid — so per-frame draw cost stays bounded
        // regardless of total dungeon size.
        public void Draw(SpriteBatch spriteBatch, Rectangle worldBounds, Texture2D atlas)
        {
            int minTileX = Math.Max(0, worldBounds.Left / TileSet.TileWidth);
            int maxTileX = Math.Min(WidthInTiles - 1, worldBounds.Right / TileSet.TileWidth);
            int minTileY = Math.Max(0, worldBounds.Top / TileSet.TileHeight);
            int maxTileY = Math.Min(HeightInTiles - 1, worldBounds.Bottom / TileSet.TileHeight);

            for (int ty = minTileY; ty <= maxTileY; ty++)
            {
                for (int tx = minTileX; tx <= maxTileX; tx++)
                {
                    TileDefData tile = TileAt(tx, ty);
                    if (tile == OutOfBoundsTile)
                        continue;

                    Rectangle destRect = new(
                        tx * TileSet.TileWidth,
                        ty * TileSet.TileHeight,
                        TileSet.TileWidth,
                        TileSet.TileHeight
                    );
                    Rectangle sourceRect = new(
                        tile.OffsetX * TileSet.TileWidth,
                        tile.OffsetY * TileSet.TileHeight,
                        TileSet.TileWidth,
                        TileSet.TileHeight
                    );

                    spriteBatch.Draw(atlas, destRect, sourceRect, Color.White);
                }
            }
        }
    }
}
