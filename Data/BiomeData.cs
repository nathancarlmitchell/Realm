namespace Realm.Data
{
    // Concentric distance band around wherever the player entered the
    // current Realm instance (EnemySpawner.EntryPosition) — MinDistance/
    // MaxDistance are in world pixels, same unit Vector2.Distance already
    // works in elsewhere (EnemySpawner's own distanceFactor). No separate
    // runtime type the way Weapon/Armor/Tome etc. have (Data/{X}Data.cs +
    // a matching {X}.cs) — a biome isn't an equippable Item with a texture
    // slot, just plain config, so the catalog is used directly as-is.
    public class BiomeData
    {
        public string Name { get; set; }
        public float MinDistance { get; set; }
        public float MaxDistance { get; set; }

        // Every biome besides Beach still points at the same shared
        // placeholder "tile" texture (Art.Tile's own content path) and is
        // told apart purely by TintR/G/B below — swapping in real
        // per-biome ground art for one of them later is just changing this
        // string, the ring-drawing code (RealmState.DrawBiomeRings())
        // already treats it as "whatever texture this biome uses," not
        // specifically Tile. Beach is the first with its own real ground
        // art (a real sand tile cropped from the placeholder Beach
        // tileset's own "Sand" cell — see Content/Biomes/Beach/Sand.png
        // and Data/TileSet_Beach.json), so its own Tint is left at
        // 255/255/255 (a no-op multiply) rather than the old sandy-tan
        // placeholder tint every biome without real art still uses to fake
        // its own color.
        public string GroundTileImageName { get; set; }
        public int TintR { get; set; }
        public int TintG { get; set; }
        public int TintB { get; set; }

        // Cross-referenced against EnemySpawner.BasicEnemyPool's own
        // names — a biome doesn't define new enemy types or override
        // level requirements, it just narrows which of the
        // already-level-gated basic types are eligible while the player
        // is standing in this ring. An enemy needs both: level-unlocked
        // AND biome-eligible.
        public string[] EnemyNames { get; set; }

        // Scattered terrain features (water pools, obstacle clusters) —
        // see Beach/BeachTerrainGenerator.cs and RealmState.cs's own
        // wiring. Null/0 (the default) for every biome that doesn't set
        // TerrainTileSetName is a complete no-op — first real use: Beach.
        // Looked up by Data/TileSet_{TerrainTileSetName}.json, same
        // Util.LoadTileSetData() convention every dungeon tileset uses.
        public string TerrainTileSetName { get; set; }
        public string WaterTileName { get; set; }
        public string ObstacleTileName { get; set; }

        // Feature counts are rolled once per Realm instance as
        // rand.Next(Min, Max + 1) — inclusive of both ends, same
        // convention DungeonGenerator.PlaceRooms() uses for room count.
        public int WaterPoolCountMin { get; set; }
        public int WaterPoolCountMax { get; set; }
        public float WaterPoolMinRadius { get; set; } // world px
        public float WaterPoolMaxRadius { get; set; }

        public int ObstacleClusterCountMin { get; set; }
        public int ObstacleClusterCountMax { get; set; }
        public float ObstacleMinRadius { get; set; }
        public float ObstacleMaxRadius { get; set; }

        // Minimum gap (world px) kept between any two features' own
        // edges, regardless of type — see BeachTerrainGenerator.PlaceOne().
        public float FeaturePadding { get; set; }
    }
}
