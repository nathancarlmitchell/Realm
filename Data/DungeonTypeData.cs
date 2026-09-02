namespace Realm.Data
{
    // Everything that varies per dungeon type, as data — the same JSON-catalog
    // shape as every other Data/*.cs class (one file per type: Data/
    // DungeonType_{Name}.json, mirroring Data/TileSetData.cs's own per-tileset
    // file convention). Adding a second dungeon type is authoring a new JSON
    // file, not touching DungeonState.cs/DungeonGenerator.cs again.
    public class DungeonTypeData
    {
        // e.g. "Snake Pit" — also what the entry portal displays
        // (Portal.Destination's DungeonDestination.DisplayName).
        public string Name { get; set; }

        // Which TileSetData this dungeon type renders with — passed to
        // Util.LoadTileSetData(). Several dungeon types can share one
        // tileset; a tileset doesn't know or care which dungeon types use it.
        public string TileSetName { get; set; }

        public int MinRoomSize { get; set; } = 5;
        public int MaxRoomSize { get; set; } = 12;
        public int MinRoomCount { get; set; } = 10;
        public int MaxRoomCount { get; set; } = 15;
        public int CorridorWidth { get; set; } = 2;

        // Cross-referenced against EnemySpawner's own basic-enemy pool (see
        // EnemySpawner.ResolveFactories()) — same "reference by name" idiom
        // BiomeData.EnemyNames already uses against that same pool.
        public string[] EnemyNames { get; set; }

        // Cross-referenced against Portal.Destination.BossesByName — a short
        // stable key ("Stheno"), not the boss's display name.
        public string BossName { get; set; }
    }
}
