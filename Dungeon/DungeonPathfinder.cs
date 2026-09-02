using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Realm
{
    // Static A* over a DungeonMap's tile grid. 4-directional (not 8) —
    // avoids the classic "cut across a wall corner diagonally" artifact on a
    // blocky grid, which would let an enemy visually clip through a wall's
    // corner even though no single tile step ever entered a solid tile.
    public static class DungeonPathfinder
    {
        private static readonly Point[] Directions =
        {
            new(1, 0),
            new(-1, 0),
            new(0, 1),
            new(0, -1),
        };

        // Returns the path from (but not including) startTile to goalTile,
        // in tile coordinates, or null if either endpoint is solid or no
        // path exists.
        public static List<Point> FindPath(DungeonMap map, Point startTile, Point goalTile)
        {
            if (!map.TileAt(startTile.X, startTile.Y).CanPassThrough)
                return null;
            if (!map.TileAt(goalTile.X, goalTile.Y).CanPassThrough)
                return null;

            if (startTile == goalTile)
                return [];

            var frontier = new PriorityQueue<Point, float>();
            frontier.Enqueue(startTile, 0f);

            var cameFrom = new Dictionary<Point, Point>();
            var costSoFar = new Dictionary<Point, float> { [startTile] = 0f };

            while (frontier.Count > 0)
            {
                Point current = frontier.Dequeue();

                if (current == goalTile)
                    break;

                foreach (Point dir in Directions)
                {
                    Point next = new(current.X + dir.X, current.Y + dir.Y);

                    if (!map.TileAt(next.X, next.Y).CanPassThrough)
                        continue;

                    float newCost = costSoFar[current] + 1f;
                    if (!costSoFar.TryGetValue(next, out float existingCost) || newCost < existingCost)
                    {
                        costSoFar[next] = newCost;
                        float priority = newCost + ManhattanDistance(next, goalTile);
                        frontier.Enqueue(next, priority);
                        cameFrom[next] = current;
                    }
                }
            }

            if (!cameFrom.ContainsKey(goalTile))
                return null; // unreachable.

            List<Point> path = [];
            Point step = goalTile;
            while (step != startTile)
            {
                path.Add(step);
                step = cameFrom[step];
            }
            path.Reverse();
            return path;
        }

        private static float ManhattanDistance(Point a, Point b) =>
            Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }
}
