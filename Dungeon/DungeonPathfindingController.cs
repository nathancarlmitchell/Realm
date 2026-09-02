using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Realm
{
    // Steers registered enemies around dungeon walls toward the player,
    // entirely from outside Enemy.cs — Enemy.AddBehaviour()/
    // AddAttackBehaviour() are protected, but Position/Velocity are public
    // and EntityManager.OfEnemyType<Enemy>() is public static, so this needs
    // zero changes to Enemy.cs. Dictionary membership (Register()) is the
    // opt-in gate: an enemy never registered here (i.e. every open-Realm
    // enemy) is provably unaffected, since nothing here ever touches an
    // enemy this class doesn't track.
    public class DungeonPathfindingController
    {
        private class PathState
        {
            public List<Point> Path;
            public int NextWaypointIndex;
            public int FramesUntilReplan;
        }

        // Not every tick, for performance — a dungeon can have many
        // registered enemies, and a full A* replan per enemy per frame would
        // add up. 30 frames (~0.5s at this codebase's fixed 60fps
        // convention) is frequent enough that a moving player doesn't make
        // the path look stale.
        private const int ReplanIntervalFrames = 30;

        // Same acceleration FollowPlayer() (Enemy.cs) already uses for its
        // own per-frame Velocity += direction.ScaleTo(...) steering — the
        // existing Velocity *= 0.8f friction (Enemy.cs) damps this the same
        // way it damps every other coroutine's steering.
        private const float SteerAcceleration = 0.5f;

        // Waypoints closer than this are considered "reached" — otherwise an
        // enemy could overshoot a waypoint every frame and constantly
        // steer back toward it instead of advancing to the next.
        private const float WaypointReachedDistance = 16f;

        private readonly DungeonMap map;
        private readonly Dictionary<Enemy, PathState> tracked = [];

        public DungeonPathfindingController(DungeonMap map)
        {
            this.map = map;
        }

        // internal, not public — Enemy itself is an internal type (Enemy.cs
        // has no access modifier), so a public method can't take it as a
        // parameter. Every real caller (DungeonEnemySpawner) is in this same
        // assembly anyway.
        internal void Register(Enemy enemy)
        {
            tracked[enemy] = new PathState();
        }

        public void Update()
        {
            // Drop anything that died/expired since the last Update() — an
            // expired Enemy's Position/Velocity are no longer meaningful to
            // steer, and EntityManager will remove it from the world on its
            // own.
            foreach (Enemy expired in tracked.Keys.Where(e => e.IsExpired).ToList())
                tracked.Remove(expired);

            foreach (var (enemy, state) in tracked)
            {
                state.FramesUntilReplan--;
                if (state.FramesUntilReplan <= 0 || state.Path == null)
                {
                    Point start = WorldToTile(enemy.Position);
                    Point goal = WorldToTile(Player.Instance.Position);
                    state.Path = DungeonPathfinder.FindPath(map, start, goal);
                    state.NextWaypointIndex = 0;
                    state.FramesUntilReplan = ReplanIntervalFrames;
                }

                if (state.Path == null || state.NextWaypointIndex >= state.Path.Count)
                    continue; // no path found, or already at the last waypoint.

                Vector2 waypointWorld = TileCenterWorld(state.Path[state.NextWaypointIndex]);
                Vector2 toWaypoint = waypointWorld - enemy.Position;

                if (toWaypoint.LengthSquared() <= WaypointReachedDistance * WaypointReachedDistance)
                {
                    state.NextWaypointIndex++;
                }
                else if (toWaypoint != Vector2.Zero)
                {
                    // Same zero-vector guard FollowPlayer() (Enemy.cs) uses
                    // before calling ScaleTo() — a zero vector would divide
                    // by zero and poison Velocity with NaN.
                    enemy.Velocity += toWaypoint.ScaleTo(SteerAcceleration);
                }
            }
        }

        private Point WorldToTile(Vector2 worldPosition) =>
            new(
                (int)(worldPosition.X / map.TileSet.TileWidth),
                (int)(worldPosition.Y / map.TileSet.TileHeight)
            );

        private Vector2 TileCenterWorld(Point tile) =>
            new(
                tile.X * map.TileSet.TileWidth + map.TileSet.TileWidth / 2f,
                tile.Y * map.TileSet.TileHeight + map.TileSet.TileHeight / 2f
            );
    }
}
