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

        // Placeholder art strategy for now: every biome points at the same
        // existing "tile" texture (Art.Tile's own content path) and is
        // told apart purely by GroundTint below. Swapping in real
        // per-biome ground art later is just changing this string — the
        // ring-drawing code (RealmState.DrawBiomeRings()) already treats
        // it as "whatever texture this biome uses," not specifically Tile.
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
    }
}
