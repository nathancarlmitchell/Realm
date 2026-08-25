using System;
using System.Collections.Generic;
using System.Linq;
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

        // Read-only outside this class — RealmState's biome ring drawing
        // centers each ring on this same point, so the ground the player
        // sees always lines up with the enemy pool GetCurrentBiome() below
        // is actually drawing from.
        public static Vector2 EntryPosition => entryPosition;

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
        //
        // Name is cross-referenced against the current biome's own
        // BiomeData.EnemyNames (see GetCurrentBiome() below) — a second,
        // independent gate on top of the level requirement, not a
        // replacement for it. A biome doesn't grant early access to a
        // still-level-locked type; it only narrows an already-unlocked
        // type down to the rings it thematically belongs in.
        private static readonly (
            string name,
            int requiredLevel,
            Func<Vector2, Enemy> factory
        )[] BasicEnemyPool =
        [
            ("Snake", 1, Enemy.CreateSnake),
            ("Slime", 2, Enemy.CreateSlime),
            ("Seeker", 3, Enemy.CreateSeeker),
            ("Wanderer", 6, Enemy.CreateWanderer),
            ("Brute", 8, Enemy.CreateBrute),
            ("Pirate", 1, Enemy.CreatePirate),
            ("Bandit", 1, position => new Bandit(position)),
        ];

        // The BiomeData ring (Data/BiomeData.json, sorted ascending by
        // MaxDistance there) whose [MinDistance, MaxDistance) contains the
        // player's current distance from entryPosition. Falls back to null
        // (SpawnWave() below then skips the biome filter entirely) if the
        // catalog doesn't cover this distance at all — a data gap
        // shouldn't be able to stop enemies from spawning outright.
        private static Data.BiomeData GetCurrentBiome()
        {
            float distanceFromEntry = Vector2.Distance(Player.Instance.Position, entryPosition);
            foreach (var biome in Game1.Instance.Biomes)
            {
                if (distanceFromEntry >= biome.MinDistance && distanceFromEntry < biome.MaxDistance)
                    return biome;
            }
            return null;
        }

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

        // Beached Buccaneer as Beach's own mini-boss: same pack-spawn shape
        // as BigSnake above (a fixed interval, always arrives with an
        // escort rather than blending into the regular wave), but — unlike
        // BigSnake, which fires regardless of location — gated to only
        // spawn while the player is actually standing in the Beach biome,
        // since a beach pirate showing up in the middle of Blighted Wastes
        // would be jarring. See GetCurrentBiome() above.
        private const int BeachedBuccaneerPackInterval = 1800; // ~30 seconds at 60fps
        private const int BeachedBuccaneerPackPirateCount = 4;
        private static int beachedBuccaneerPackCooldownRemaining = BeachedBuccaneerPackInterval;

        // Bandit Leader as Beach's second mini-boss — same shape/gating as
        // BeachedBuccaneerPack above, offset to a different interval so
        // the two don't always arrive on exactly the same tick.
        private const int BanditLeaderPackInterval = 2100; // ~35 seconds at 60fps
        private const int BanditLeaderPackBanditCount = 4;
        private static int banditLeaderPackCooldownRemaining = BanditLeaderPackInterval;

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

                if (beachedBuccaneerPackCooldownRemaining <= 0)
                {
                    if (GetCurrentBiome()?.Name == "Beach")
                        SpawnBeachedBuccaneerPack();
                    beachedBuccaneerPackCooldownRemaining = BeachedBuccaneerPackInterval;
                }
                else
                {
                    beachedBuccaneerPackCooldownRemaining--;
                }

                if (banditLeaderPackCooldownRemaining <= 0)
                {
                    if (GetCurrentBiome()?.Name == "Beach")
                        SpawnBanditLeaderPack();
                    banditLeaderPackCooldownRemaining = BanditLeaderPackInterval;
                }
                else
                {
                    banditLeaderPackCooldownRemaining--;
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
        // level-unlocked AND belong to the biome ring the player is
        // currently standing in (GetCurrentBiome() above) — reads as a
        // group arriving together rather than scattered independently
        // around the player.
        private static void SpawnWave()
        {
            Data.BiomeData biome = GetCurrentBiome();

            List<Func<Vector2, Enemy>> unlocked = [];
            foreach (var (name, requiredLevel, factory) in BasicEnemyPool)
            {
                bool levelUnlocked = Player.Instance.Level >= requiredLevel;
                bool biomeAllows = biome == null || biome.EnemyNames.Contains(name);
                if (levelUnlocked && biomeAllows)
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

        // One Beached Buccaneer plus a cluster of ordinary Pirates around
        // the same anchor point — same clustering technique as
        // SpawnBigSnakePack() above. BeachedBuccaneer is a dedicated class
        // (Bosses/BeachedBuccaneer.cs), not a bare Enemy.CreateX() factory
        // like CreateBigSnake, so this constructs it directly instead of
        // going through a Func<Vector2, Enemy> reference.
        private static void SpawnBeachedBuccaneerPack()
        {
            Vector2 anchor = GetSpawnPosition();
            EntityManager.Add(new Bosses.BeachedBuccaneer(anchor));

            for (int i = 0; i < BeachedBuccaneerPackPirateCount; i++)
            {
                Vector2 offset = new(rand.Next(-80, 81), rand.Next(-80, 81));
                EntityManager.Add(Enemy.CreatePirate(anchor + offset));
            }
        }

        // One Bandit Leader plus a cluster of ordinary Bandits — same
        // shape as SpawnBeachedBuccaneerPack() above. Both BanditLeader and
        // Bandit are dedicated classes (Bosses/BanditLeader.cs, Bandit.cs),
        // not bare Enemy.CreateX() factories, so both are constructed
        // directly.
        private static void SpawnBanditLeaderPack()
        {
            Vector2 anchor = GetSpawnPosition();
            EntityManager.Add(new Bosses.BanditLeader(anchor));

            for (int i = 0; i < BanditLeaderPackBanditCount; i++)
            {
                Vector2 offset = new(rand.Next(-80, 81), rand.Next(-80, 81));
                EntityManager.Add(new Bandit(anchor + offset));
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
