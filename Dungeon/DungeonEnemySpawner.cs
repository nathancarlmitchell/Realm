using System;
using Microsoft.Xna.Framework;

namespace Realm
{
    // Per-DungeonState enemy spawner — an instance class (unlike the static
    // EnemySpawner used by the open Realm), since each dungeon needs its own
    // DungeonMap/room reference rather than one shared global state. Spawns
    // existing enemy types only, picked from whichever factories the current
    // dungeon type's own Data/DungeonType_{Name}.json allows (EnemyNames,
    // resolved via EnemySpawner.ResolveFactories() — see DungeonState's
    // constructor) — the dungeon-type equivalent of a biome's EnemyNames.
    //
    // Unlike the open Realm's continuous EnemySpawner, this spawns everything
    // a dungeon will ever have exactly once (SpawnAll(), called from
    // DungeonState's constructor) and never respawns afterward — so clearing
    // every enemy actually clears the dungeon, rather than more trickling in
    // forever.
    public class DungeonEnemySpawner
    {
        private const int MinEnemiesPerRoom = 2;
        private const int MaxEnemiesPerRoom = 4;

        private readonly DungeonMap map;
        private readonly DungeonPathfindingController pathfinding;
        private readonly Func<Vector2, Enemy>[] enemyFactories;
        private readonly Random rand = new();

        // internal, not public — enemyFactories' element type involves
        // Enemy, itself an internal type (same reason DungeonPathfinding
        // Controller.Register(Enemy) is internal). enemyFactories comes from
        // EnemySpawner.ResolveFactories(dungeonType.EnemyNames) — see
        // DungeonState's constructor — rather than a fixed list here, so
        // different dungeon types can have different eligible enemies.
        internal DungeonEnemySpawner(
            DungeonMap map,
            DungeonPathfindingController pathfinding,
            Func<Vector2, Enemy>[] enemyFactories
        )
        {
            this.map = map;
            this.pathfinding = pathfinding;
            this.enemyFactories = enemyFactories;
        }

        // Populates every room except Room 0 (the player's start room — see
        // DungeonState's constructor) with a handful of enemies. Called once;
        // there is no Update() — nothing spawns after this.
        public void SpawnAll()
        {
            for (int i = 1; i < map.Rooms.Count; i++)
            {
                Rectangle room = map.Rooms[i];
                int spawnCount = rand.Next(MinEnemiesPerRoom, MaxEnemiesPerRoom + 1);

                for (int j = 0; j < spawnCount; j++)
                {
                    Vector2 position = RandomWorldPositionInRoom(room);
                    Func<Vector2, Enemy> factory = enemyFactories[rand.Next(enemyFactories.Length)];
                    Enemy enemy = factory(position);

                    EntityManager.Add(enemy);
                    pathfinding.Register(enemy);
                }
            }
        }

        // Every cell inside a room rectangle is already carved to floor by
        // DungeonGenerator, so any point strictly inside it is safe — no
        // CanPassThrough check needed here.
        private Vector2 RandomWorldPositionInRoom(Rectangle room)
        {
            int tileX = rand.Next(room.Left, room.Right);
            int tileY = rand.Next(room.Top, room.Bottom);
            return new Vector2(
                tileX * map.TileSet.TileWidth + map.TileSet.TileWidth / 2f,
                tileY * map.TileSet.TileHeight + map.TileSet.TileHeight / 2f
            );
        }
    }
}
