using System;
using Microsoft.Xna.Framework;

namespace Realm
{
    // Per-DungeonState enemy spawner — an instance class (unlike the static
    // EnemySpawner used by the open Realm), since each dungeon needs its own
    // DungeonMap/room reference rather than one shared global state. Spawns
    // existing enemy types only; a dungeon has no biome-ring concept to
    // restrict which types are eligible.
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

        public DungeonEnemySpawner(DungeonMap map, DungeonPathfindingController pathfinding)
        {
            this.map = map;
            this.pathfinding = pathfinding;
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
                    Func<Vector2, Enemy> factory = EnemyFactories[rand.Next(EnemyFactories.Length)];
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
