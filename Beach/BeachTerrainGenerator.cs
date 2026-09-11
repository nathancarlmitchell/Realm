using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Realm.Data;

namespace Realm
{
    // Scatters BeachTerrainFeature circles (water pools, obstacle clusters)
    // across a disc in continuous world space — the open Realm's Beach ring
    // has no tile grid, so this is deliberately NOT DungeonGenerator.cs's
    // room-and-corridor approach (which always fills a full rectangular
    // grid — an 8000px-radius ring would be a ~500x500-tile grid, a cost
    // category nothing else in this engine pays). Placement mirrors
    // DungeonGenerator.PlaceRooms()'s own circular-room overlap rejection
    // (center-distance-vs-summed-radii+padding), just against a disc
    // instead of a bounded tile rectangle.
    static class BeachTerrainGenerator
    {
        // Mirrors DungeonGenerator.MaxPlacementAttempts — a placement
        // budget per feature slot, not a per-ring "personality" knob, so
        // it's a fixed engine constant rather than a BiomeData field.
        private const int MaxPlacementAttempts = 500;

        // Radius (world px) kept clear around the ring's own center
        // (wherever the player entered this Realm instance —
        // EnemySpawner.EntryPosition) so a fresh arrival never spawns
        // pinned inside a feature.
        private const float EntryKeepClearRadius = 400f;

        public static List<BeachTerrainFeature> Generate(
            TileSetData tileSet,
            BiomeData biome,
            Vector2 center,
            Random rand
        )
        {
            List<BeachTerrainFeature> features = [];

            TileDefData waterTile = ResolveTile(tileSet, biome.WaterTileName);
            TileDefData obstacleTile = ResolveTile(tileSet, biome.ObstacleTileName);

            if (waterTile != null)
            {
                int waterCount = rand.Next(biome.WaterPoolCountMin, biome.WaterPoolCountMax + 1);
                for (int i = 0; i < waterCount; i++)
                    PlaceOne(
                        features,
                        waterTile,
                        biome.WaterPoolMinRadius,
                        biome.WaterPoolMaxRadius,
                        biome,
                        center,
                        rand
                    );
            }

            if (obstacleTile != null)
            {
                int obstacleCount = rand.Next(
                    biome.ObstacleClusterCountMin,
                    biome.ObstacleClusterCountMax + 1
                );
                for (int i = 0; i < obstacleCount; i++)
                    PlaceOne(
                        features,
                        obstacleTile,
                        biome.ObstacleMinRadius,
                        biome.ObstacleMaxRadius,
                        biome,
                        center,
                        rand
                    );
            }

            return features;
        }

        // Null (not thrown) for an unset/blank name — same "0/null = off"
        // contract every BiomeData terrain field already has; Generate()
        // above simply skips that feature type entirely rather than
        // failing the whole Realm instance over one missing tile name.
        private static TileDefData ResolveTile(TileSetData tileSet, string name) =>
            string.IsNullOrEmpty(name) ? null : tileSet.Tiles.FirstOrDefault(t => t.Name == name);

        private static void PlaceOne(
            List<BeachTerrainFeature> features,
            TileDefData tile,
            float minRadius,
            float maxRadius,
            BiomeData biome,
            Vector2 center,
            Random rand
        )
        {
            for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
            {
                // Uniform over the ring's AREA, not just its radius — same
                // sqrt(rand) technique RealmState's own Beach Beacon
                // placement already uses, for the same reason (sampling
                // radius directly bunches points near the center).
                float angle = (float)(rand.NextDouble() * MathHelper.TwoPi);
                float distance = biome.MaxDistance * MathF.Sqrt((float)rand.NextDouble());
                Vector2 candidateCenter = center + Extensions.FromPolar(angle, distance);
                float candidateRadius = MathHelper.Lerp(
                    minRadius,
                    maxRadius,
                    (float)rand.NextDouble()
                );

                if (Vector2.DistanceSquared(candidateCenter, center) < EntryKeepClearRadius * EntryKeepClearRadius)
                    continue;

                bool overlaps = features.Any(existing =>
                {
                    float minDist = candidateRadius + existing.Radius + biome.FeaturePadding;
                    return Vector2.DistanceSquared(candidateCenter, existing.Center) < minDist * minDist;
                });
                if (overlaps)
                    continue;

                features.Add(new BeachTerrainFeature(candidateCenter, candidateRadius, tile));
                return;
            }

            // Ran out of attempts (a crowded ring near its feature-count
            // ceiling) — just place fewer of this type rather than
            // throwing; same "best effort, no hard failure" precedent
            // DungeonGenerator.PlaceRooms() sets for its own placement
            // budget.
        }
    }
}
