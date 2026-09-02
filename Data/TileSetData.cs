using System.Collections.Generic;

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

        // Schema-only for now — no code reads this yet. Exists so a tileset can
        // be authored complete from day one; wiring it up (wall-blocked
        // projectiles) is real, separate follow-up work — see the "Follow-up
        // work" section of the walled-dungeon plan.
        public bool CanShootThrough { get; set; } = true;

        // Schema-only for now — no code reads this yet. See CanShootThrough's
        // comment above; the follow-up work here is breakable tiles with
        // runtime health/destroyed state, not just this flag.
        public bool IsDestructible { get; set; }

        public bool HarmsPlayer { get; set; }
        public float DamagePerSecond { get; set; }

        public bool SlowsPlayer { get; set; }
        public float SlowMultiplier { get; set; } = 1f;

        // Reuses Entity's existing debuff taxonomy directly — no new enum
        // needed just for tiles.
        public List<Entity.DebuffType> AppliedDebuffs { get; set; } = [];
    }
}
