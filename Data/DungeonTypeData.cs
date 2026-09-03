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

        // Optional "cove" generation mode (DungeonGenerator.Generate()) —
        // first real use: Pirate Cave, matching its own wiki lore of "a
        // network of wooden planks around the cove." Left null/0 (the
        // default), generation behaves exactly as before: a random wall
        // tile background and a random same-category floor pick per cell.
        // Set, each room is typed once to SandFloorTileName or
        // WoodFloorTileName (whichever are non-null — both means "randomly
        // one or the other per room," only one means "every room is that
        // one"), corridors are carved as a WoodFloorTileName walkway, the
        // background fill is BackgroundTileName instead of a random wall,
        // and any wood placement (room or corridor) has a PathGapChance
        // chance of becoming BackgroundTileName instead — a missing plank
        // over water. All four tile names are looked up by TileDefData.Name
        // within this dungeon type's own TileSetName.
        public string SandFloorTileName { get; set; }
        public string WoodFloorTileName { get; set; }
        public string BackgroundTileName { get; set; }
        public float PathGapChance { get; set; }

        // Chance (0.0-1.0) that this dungeon instance gets a Treasure Room
        // — see Dungeon/TreasureRoomController.cs. 0 (the default) means
        // "never," unchanged for every dungeon type that doesn't opt in;
        // first real use: Snake Pit, matching its own wiki's "there is a
        // chance that at least one treasure room will appear." No change to
        // the room's own tile carving when it does — one of the normally-
        // generated rooms is picked and handed to the controller as-is, a
        // deliberate simplification from the wiki's own distinctly-shaped
        // "long room."
        public float TreasureRoomChance { get; set; } = 0f;

        // Rooms carved as circles (inscribed within their own placement
        // rectangle) instead of the default filled rectangle — first real
        // use: Snake Pit, matching its own wiki's "a series of circular
        // rooms." false (the default) is the original behavior, unchanged
        // for every dungeon type that doesn't opt in. The Treasure Room
        // (Dungeon/TreasureRoomController.cs) is deliberately exempt even
        // when this is true — the wiki's own Treasure Room is described as
        // visually distinct from the dungeon's circular rooms, not another
        // circle, so it keeps its normal rectangular footprint regardless.
        public bool CircularRooms { get; set; } = false;

        // Optional hallway fill — first real use: Snake Pit, matching its
        // own wiki's "hallways... filled in with easily destructible brown
        // blocks." Left null (the default), corridors behave exactly as
        // before (a random same-category floor pick, or the cove walkway
        // above). Set, every corridor cell outside any room's own footprint
        // is carved as this tile instead — expected to be a real
        // IsDestructible wall tile (e.g. Crypt's "Breakable Wall") so the
        // hallway has to be broken through rather than walked straight down;
        // nothing here enforces that, it's just what makes the feature make
        // sense. Looked up by TileDefData.Name within this dungeon type's
        // own TileSetName, same as SandFloorTileName/WoodFloorTileName/
        // BackgroundTileName above.
        public string CorridorTileName { get; set; }
    }
}
