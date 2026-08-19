using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm.States;

namespace Realm
{
    static class EnemySpawner
    {
        static Random rand = new Random();
        static float inverseSpawnChance = 60;

        // Where the player entered the current Realm instance from the Nexus
        // (RealmState's constructor captures Player.Instance.Position at
        // that moment via SetEntryPosition() below) — the local "Nexus" a
        // freshly-generated open-world dungeon actually has, since the
        // player's absolute Position otherwise carries straight over from
        // wherever they were standing in the Nexus itself.
        static Vector2 entryPosition;

        // Distance-based spawn density: the farther the player wanders from
        // where they entered, the denser spawns get, on top of the existing
        // time-based ramp below. MaxDistanceForFullDensity is roughly a
        // minute of sustained walking (Speed-derived Velocity is ~5 units/
        // frame at a mid-range Speed stat, i.e. ~300 units/sec at 60fps) —
        // tunable, not derived from anything more precise.
        const float MaxDistanceForFullDensity = 20000f;
        const float MinDistanceInverseSpawnChance = 15f;

        // Level-gated pool for the basic enemy types — each only spawns
        // once the player has reached its required level, ordered by
        // toughness (PointValue: Snake 2, Slime 4, Seeker 7, Wanderer 15,
        // Brute 120). Snake is always available from Level 1 so there's
        // never a dead stretch with nothing to fight; the rest widen the
        // mix as the player grows into the run. Levels are a starting
        // guess, easy to retune. BigSnake is deliberately NOT in this pool
        // — see BigSnakePackInterval/SpawnBigSnakePack below, its own rarer
        // mini-boss-style spawn instead of blending into the regular wave.
        private static readonly (int requiredLevel, Func<Vector2, Enemy> factory)[] BasicEnemyPool =
        [
            (1, Enemy.CreateSnake),
            (2, Enemy.CreateSlime),
            (3, Enemy.CreateSeeker),
            (6, Enemy.CreateWanderer),
            (8, Enemy.CreateBrute),
        ];

        // BigSnake as a mini-boss: much rarer than the regular wave pattern
        // (a fixed, guaranteed-when-it-fires interval instead of the wave
        // system's per-wave probability), and always arrives with its own
        // cluster of ordinary Snakes rather than blending randomly into
        // BasicEnemyPool — reads as "a BigSnake and its guard" instead of
        // just another wave roll. Still available from Level 1 like every
        // other snake, per the user's own request.
        private const int BigSnakePackInterval = 1800; // ~30 seconds at 60fps
        private const int BigSnakePackSnakeCount = 4;
        private static int bigSnakePackCooldownRemaining = BigSnakePackInterval;

        // Wave/pack spawning: instead of each basic type independently
        // rolling a 1-in-N chance every frame (a steady trickle), a wave of
        // several enemies spawns together every N frames, using the exact
        // same effectiveInverseSpawnChance value the old system rolled
        // against — reinterpreted as a hard interval instead of a
        // probability. This keeps roughly the same average spawn rate as
        // before (3 independent 1-in-N rolls over N frames average out to
        // about 3 spawns, which is what a 2-4-sized wave once every N
        // frames also averages to) while changing the pattern from a
        // trickle to bursts.
        private static int waveCooldownRemaining = 0;

        public static void SetEntryPosition(Vector2 position)
        {
            entryPosition = position;
        }

        public static void Update()
        {
            if (!Player.Instance.IsExpired && EntityManager.Count < 1500)
            {
                float distanceFromEntry = Vector2.Distance(Player.Instance.Position, entryPosition);
                float distanceFactor = MathHelper.Clamp(
                    distanceFromEntry / MaxDistanceForFullDensity,
                    0f,
                    1f
                );
                int effectiveInverseSpawnChance = Math.Max(
                    1,
                    (int)MathHelper.Lerp(inverseSpawnChance, MinDistanceInverseSpawnChance, distanceFactor)
                );

                if (waveCooldownRemaining <= 0)
                {
                    SpawnWave();
                    waveCooldownRemaining = effectiveInverseSpawnChance;
                }
                else
                {
                    waveCooldownRemaining--;
                }

                if (bigSnakePackCooldownRemaining <= 0)
                {
                    SpawnBigSnakePack();
                    bigSnakePackCooldownRemaining = BigSnakePackInterval;
                }
                else
                {
                    bigSnakePackCooldownRemaining--;
                }

                // SpriteGod stays its own independent, level-scaling roll —
                // a distinct "occasional special threat" rather than part
                // of the regular basic-enemy wave pattern above.
                if (rand.Next((int)1500 - Player.Instance.Level * 50) == 0)
                {
                    EntityManager.Add(Enemy.CreateSpriteGod(GetSpawnPosition()));
                }
            }

            // slowly increase the spawn rate as time progresses
            if (inverseSpawnChance > 20)
                inverseSpawnChance -= 0.005f;
        }

        // Spawns a small (2-4) pack of enemies clustered around one shared
        // anchor point, drawn from whichever basic types are currently
        // level-unlocked — reads as a group arriving together rather than
        // scattered independently around the player.
        private static void SpawnWave()
        {
            List<Func<Vector2, Enemy>> unlocked = [];
            foreach (var (requiredLevel, factory) in BasicEnemyPool)
            {
                if (Player.Instance.Level >= requiredLevel)
                    unlocked.Add(factory);
            }

            if (unlocked.Count == 0)
                return;

            Vector2 anchor = GetSpawnPosition();
            int waveSize = rand.Next(2, 5);
            for (int i = 0; i < waveSize; i++)
            {
                Func<Vector2, Enemy> factory = unlocked[rand.Next(unlocked.Count)];
                Vector2 offset = new(rand.Next(-80, 81), rand.Next(-80, 81));
                EntityManager.Add(factory(anchor + offset));
            }
        }

        // One BigSnake plus a cluster of ordinary Snakes around the same
        // anchor point — same clustering technique as SpawnWave(), so it
        // reads as a mini-boss encounter (the BigSnake and its guard)
        // rather than a randomly-assembled wave.
        private static void SpawnBigSnakePack()
        {
            Vector2 anchor = GetSpawnPosition();
            EntityManager.Add(Enemy.CreateBigSnake(anchor));

            for (int i = 0; i < BigSnakePackSnakeCount; i++)
            {
                Vector2 offset = new(rand.Next(-80, 81), rand.Next(-80, 81));
                EntityManager.Add(Enemy.CreateSnake(anchor + offset));
            }
        }

        private static Vector2 GetSpawnPosition()
        {
            Vector2 pos;

            int minSpawnDistance = 250;
            int maxSpawnDistance = 1000;

            float minX = Player.Instance.Position.X - maxSpawnDistance;
            float minY = Player.Instance.Position.Y - maxSpawnDistance;
            float maxX = Player.Instance.Position.X + maxSpawnDistance;
            float maxY = Player.Instance.Position.Y + maxSpawnDistance;
            do
            {
                pos = new Vector2(rand.Next((int)minX, (int)maxX), rand.Next((int)minY, (int)maxY));
            } while (
                Vector2.DistanceSquared(pos, Player.Instance.Position)
                < minSpawnDistance * minSpawnDistance
            );
            return pos;
        }

        public static void Reset()
        {
            inverseSpawnChance = 60;
            waveCooldownRemaining = 0;
            bigSnakePackCooldownRemaining = BigSnakePackInterval;
        }
    }
}
