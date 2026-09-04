using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Realm.Data
{
    // Describes one biome/dungeon's own tile atlas image, tile-by-tile — the
    // data-driven catalog the walled-dungeon feature (see Dungeon/DungeonMap.cs)
    // reads instead of a hardcoded Wall/Floor pair, so tilesets can be authored
    // (and re-skinned) purely as content, no code changes needed. Same
    // one-POCO-plus-one-JSON-file shape as every other catalog in this project
    // (Data/BiomeData.cs, Data/RingData.cs, etc.), except each tileset gets its
    // own named file (Data/TileSet_{Name}.json) rather than one shared array —
    // tile Ids below are only meaningful within their own tileset, unlike
    // RingData.json's flat list of globally-distinct rings.
    public class TileSetData
    {
        // e.g. "Crypt" — matches the {Name} in Data/TileSet_{Name}.json and is
        // what DungeonState passes to Util.LoadTileSetData().
        public string Name { get; set; }

        // The atlas PNG's content path, loaded via content.Load<Texture2D>() —
        // needs a matching Content/Content.mgcb TextureImporter/TextureProcessor
        // block, same as every other texture in the game (see CLAUDE.md).
        public string ImageName { get; set; }

        public int TileWidth { get; set; } = 32;
        public int TileHeight { get; set; } = 32;

        public List<TileDefData> Tiles { get; set; } = [];
    }

    // One tile in a TileSetData's atlas. OffsetX/OffsetY are in tile units
    // (column/row within the atlas), not pixels — DungeonMap.Draw() multiplies
    // by TileWidth/TileHeight to get the actual source Rectangle.
    public class TileDefData
    {
        // Referenced by DungeonMap's grid — only unique within this tileset.
        public int Id { get; set; }

        // e.g. "Cracked Floor", "Stone Wall", "Lava" — authoring/debugging aid,
        // not read by any game logic.
        public string Name { get; set; }

        public int OffsetX { get; set; }
        public int OffsetY { get; set; }

        // Walkable. This is what a plain Wall/Floor boolean generalizes into
        // once a tileset can have more than two categories (e.g. several wall
        // variants) — DungeonGenerator partitions candidate tiles by this flag,
        // and DungeonMap.ResolveCircleCollision() treats every tile where this
        // is false as solid.
        public bool CanPassThrough { get; set; } = true;

        // Excludes this tile from DungeonGenerator's initial full-canvas
        // wall/background fill (every !CanPassThrough tile is normally a
        // candidate there, for free per-cell wall-texture variety — see
        // DungeonGenerator.Generate()'s own comment). A deliberately-placed
        // obstacle that happens to also be !CanPassThrough (e.g. Sprite
        // World's own "Sprite Trees", scattered only via DungeonTypeData.
        // ObstacleTileChance within actual rooms) sets this true so it
        // doesn't also get randomly speckled across the inaccessible void
        // background outside any room/corridor. False (the default) is the
        // original behavior, unchanged for every existing wall tile.
        public bool ExcludeFromBackgroundFill { get; set; } = false;

        // A tile where this is false blocks projectiles — DungeonState's
        // ExpireWallBlockedProjectiles() expires any player or enemy
        // projectile that crosses into one, entirely externally to the
        // shared EntityManager/Projectile.cs/EnemyProjectile.cs collision
        // code (see docs/DEVLOG.md's wall-blocked-projectiles entry).
        public bool CanShootThrough { get; set; } = true;

        // Breakable by player projectile fire — see DungeonMap.DamageTile().
        public bool IsDestructible { get; set; }

        // Only meaningful when IsDestructible is true — how many points of
        // projectile damage this tile takes before it breaks. Runtime
        // remaining health is tracked separately, per-cell, by DungeonMap
        // (see its DamageTile()); this is just the starting value each fresh
        // instance of the tile begins with. Ignored entirely for a
        // non-destructible tile (default 0 — never meant to be read).
        public int DestructibleHealth { get; set; }

        public bool HarmsPlayer { get; set; }
        public float DamagePerSecond { get; set; }

        public bool SlowsPlayer { get; set; }
        public float SlowMultiplier { get; set; } = 1f;

        // Reuses Entity's existing debuff taxonomy directly — no new enum
        // needed just for tiles.
        public List<Entity.DebuffType> AppliedDebuffs { get; set; } = [];

        // A tile where ConveyorSpeed is nonzero pushes the player by
        // ConveyorDirection * ConveyorSpeed (world px/tick) every frame they
        // stand on it — see DungeonState.ApplyTileEffects(). First real use:
        // Sprite World's own "multicolored conveyor belts... constantly
        // pushing players" (realmeye.com/wiki/sprite-world). Split into two
        // plain floats rather than one Vector2 property deliberately —
        // System.Text.Json's default settings (no IncludeFields configured
        // anywhere in this project's Util.cs Load*Data() calls) only ever
        // populates *properties*, and MonoGame's Vector2 exposes X/Y as
        // public *fields*, not properties, so a `Vector2 ConveyorDirection`
        // property here would always deserialize to (0,0) regardless of
        // what the JSON says. ConveyorDirection below is a convenience
        // accessor over the two real, JSON-backed floats, not itself
        // read/written by the serializer.
        public float ConveyorDirectionX { get; set; }
        public float ConveyorDirectionY { get; set; }
        public float ConveyorSpeed { get; set; }

        public Vector2 ConveyorDirection => new(ConveyorDirectionX, ConveyorDirectionY);
    }
}
