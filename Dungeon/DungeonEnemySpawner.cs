using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Realm
{
    // Per-DungeonState enemy spawner — an instance class (unlike the static
    // EnemySpawner used by the open Realm), since each dungeon needs its own
    // DungeonMap/room reference rather than one shared global state. Spawns
    // existing enemy types only; a dungeon has no biome-ring concept to
    // restrict which types are eligible.
    public class DungeonEnemySpawner
    {
        // Mirrors EnemySpawner.cs's own wave-cooldown idiom
        // (waveCooldownRemaining, ticked down every Update() and reset after
        // firing).
        private const int SpawnIntervalFrames = 180; // ~3 seconds at 60fps.
        private const int PopulationCap = 20;
        private const int MinEnemiesPerWave = 2;
        private const int MaxEnemiesPerWave = 4;

        private static readonly Func<Vector2, Enemy>[] EnemyFactories =
        {
            Enemy.CreateWanderer,
            Enemy.CreateSeeker,
            Enemy.CreateSnake,
            Enemy.CreateSlime,
        };

        private readonly DungeonMap map;
        private readonly DungeonPathfindingController pathfinding;
        private readonly Random rand = new();

        private int waveCooldownRemaining;

        public DungeonEnemySpawner(DungeonMap map, DungeonPathfindingController pathfinding)
        {
            this.map = map;
            this.pathfinding = pathfinding;
        }

        public void Update()
        {
            if (waveCooldownRemaining > 0)
            {
                waveCooldownRemaining--;
                return;
            }

            waveCooldownRemaining = SpawnIntervalFrames;

            // Room 0 is the player's start room (see DungeonState's
            // constructor) — excluded so enemies don't spawn on top of the
            // player the moment they enter.
            if (map.Rooms.Count <= 1)
                return;

            if (EntityManager.OfEnemyType<Enemy>().Count() >= PopulationCap)
                return;

            Rectangle room = map.Rooms[rand.Next(1, map.Rooms.Count)];
            int spawnCount = rand.Next(MinEnemiesPerWave, MaxEnemiesPerWave + 1);

            for (int i = 0; i < spawnCount; i++)
            {
                Vector2 position = RandomWorldPositionInRoom(room);
                Func<Vector2, Enemy> factory = EnemyFactories[rand.Next(EnemyFactories.Length)];
                Enemy enemy = factory(position);

                EntityManager.Add(enemy);
                pathfinding.Register(enemy);
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
