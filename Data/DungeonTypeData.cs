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

        // Each room independently rolls 0..ConveyorMaxStripsPerRoom (inclusive)
        // conveyor strips — first real use: Sprite World, matching its own
        // wiki's "multicolored conveyor belts... often found in the rooms."
        // Each strip is a full row or column of the room, not a scattered
        // per-cell pick: ConveyorTileNames entries whose ConveyorDirection
        // is horizontal (Left/Right) lay down a horizontal strip (a single
        // row spanning the room's width), and vertical ones (Up/Down) lay
        // down a vertical strip (a single column spanning the room's
        // height) — DungeonGenerator.PlaceRooms() picks the orientation by
        // inspecting each resolved tile's own ConveyorDirection, so no
        // separate horizontal/vertical name list is needed here. 0/null
        // (the default) is a no-op, unchanged for every dungeon type that
        // doesn't opt in. Looked up by TileDefData.Name within this dungeon
        // type's own TileSetName, same lookup convention as CorridorTileName.
        public int ConveyorMaxStripsPerRoom { get; set; } = 0;
        public string[] ConveyorTileNames { get; set; }

        // Same shape as ConveyorTileChance/ConveyorTileNames immediately
        // above, for a single scattered destructible obstacle instead of a
        // directional push tile — first real use: Sprite World's own
        // "almost all rooms are littered with destructible Sprite Tree
        // obstacles." Checked first, when both are set for the same
        // dungeon type (Sprite World has both) — see PlaceRooms()'s own
        // comment for why Sprite Trees wins a same-cell tie over a
        // conveyor.
        public float ObstacleTileChance { get; set; } = 0f;
        public string ObstacleTileName { get; set; }

        // A single, dedicated mini-boss enemy — looked up by name against
        // EnemySpawner's own BasicEnemyPool, same as EnemyNames above, but
        // resolved and placed separately by DungeonState's constructor
        // rather than folded into DungeonEnemySpawner's own uniform
        // per-room roll. First real use: Sprite World's own Native Sprite
        // God — a 3500 HP mini-boss the per-room spawner's uniform pick
        // would otherwise make absurdly common if it were just another
        // entry in EnemyNames. Null/0 (the default) is a no-op, unchanged
        // for every dungeon type that doesn't opt in.
        public string EliteEnemyName { get; set; }
        public int EliteEnemyCount { get; set; } = 0;
    }
}
