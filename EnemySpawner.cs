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
        // where they entered, the denser spawns get. MaxDistanceForFullDensity
        // is roughly a minute of sustained walking (Speed-derived Velocity is
        // ~5 units/frame at a mid-range Speed stat, i.e. ~300 units/sec at
        // 60fps) — tunable, not derived from anything more precise.
        // BaseInverseSpawnChance is what the density blends FROM (near
        // entryPosition) down to MinDistanceInverseSpawnChance (far from
        // entryPosition) — previously this itself decayed over play time
        // (an elapsed-time-based ramp, independent of distance), but that
        // time-based ramp was removed; it's now a fixed constant.
        const float MaxDistanceForFullDensity = 20000f;
        const float BaseInverseSpawnChance = 60f;
        const float MinDistanceInverseSpawnChance = 15f;

        // Beach felt more hectic than every other biome — reported directly
        // after entry 213 folded four extra enemy types (Bandit Leader,
        // Scorpion Queen, Sandsman King, Giant Crab) into its own regular
        // wave pool on top of its existing Beached Buccaneer mini-boss pack
        // and three Little Jelly group-packs, none of which any other
        // biome has. Rather than touch the shared distance-based formula
        // every biome relies on, this stretches Beach's own wave cooldown
        // (see Update() below) by 25% — a "slightly" reduced spawn rate,
        // not a rework — and the same factor is baked directly into
        // BeachedBuccaneerPackInterval/the three Little Jelly pack
        // intervals below (all Beach-exclusive already).
        const float BeachSpawnRateMultiplier = 1.25f;

        // "Far too many enemies on screen at once" — reported directly
        // after a live Beach playtest. None of the spawn gates above (or
        // below) ever look at how many enemies are already alive near the
        // player, only at distance-scaled frequency — so enemies could
        // keep piling up indefinitely as long as the player didn't clear
        // them faster than new ones arrived. This caps it: no new spawn
        // (wave, pack, or SpriteGod) fires while at least MaxNearbyEnemies
        // are already within NearbyEnemyRadius of the player, regardless of
        // what any individual cooldown says — see TooManyEnemiesNearby()
        // below. Radius mirrors the "on-screen half-diagonal" idiom used
        // elsewhere (e.g. SandDevil.cs's MinSpawnDistanceFromPlayer) rather
        // than an arbitrary flat number, so "nearby" tracks the actual
        // visible play area regardless of window size. Both numbers are a
        // first-pass guess — expect retuning after the next playtest pass.
        private static readonly float NearbyEnemyRadius = Vector2.Distance(
            Vector2.Zero,
            new Vector2(Game1.GameplayViewportWidth / 2f, Game1.GameplayViewportHeight / 2f)
        );
        private const int MaxNearbyEnemies = 12;

        // Pool for the basic enemy types, ordered by toughness (PointValue:
        // Snake 2, Slime 4, Seeker 7, Wanderer 15, Brute 120). Every type
        // here is available from the start — no player-level requirement.
        // BigSnake is deliberately NOT in this pool — see
        // BigSnakePackInterval/SpawnBigSnakePack below, its own rarer
        // mini-boss-style spawn instead of blending into the regular wave.
        // Bandit Leader/Scorpion Queen/Sandsman King/Giant Crab WERE their
        // own dedicated mini-boss pack-spawns the same way, but are now
        // folded in here as regular wave members instead — Beached
        // Buccaneer (still its own pack-spawn below) is the only Beach
        // mini-boss left. Each still runs whatever bespoke behavior its own
        // class defines when it spawns (e.g. Scorpion Queen still builds her
        // own escort of Little Scorpions) — only how often/how it gets
        // spawned changed, not what it does once it exists.
        //
        // Name is cross-referenced against the current biome's own
        // BiomeData.EnemyNames (see GetCurrentBiome() below), which narrows
        // this pool down to the types that belong in the ring the player is
        // currently standing in.
        private static readonly (string name, Func<Vector2, Enemy> factory)[] BasicEnemyPool =
        [
            ("Snake", Enemy.CreateSnake),
            ("Slime", Enemy.CreateSlime),
            ("Seeker", Enemy.CreateSeeker),
            ("Wanderer", Enemy.CreateWanderer),
            ("Brute", Enemy.CreateBrute),
            ("Pirate", Enemy.CreatePirate),
            ("Bandit", position => new Bandit(position)),
            ("Piratess", position => new Piratess(position)),
            ("Sand Devil", position => new SandDevil(position)),
            ("Bandit Leader", position => new Bosses.BanditLeader(position)),
            ("Scorpion Queen", position => new Bosses.ScorpionQueen(position)),
            ("Sandsman King", position => new Bosses.SandsmanKing(position)),
            ("Giant Crab", position => new Bosses.GiantCrab(position)),

            // Pirate Cave (Data/DungeonType_PirateCave.json) — never added to
            // any BiomeData.json's own EnemyNames, so these never spawn in
            // the open Realm; only DungeonEnemySpawner.ResolveFactories()
            // ever selects them.
            ("Cave Pirate Cabin Boy", Enemy.CreateCavePirateCabinBoy),
            ("Cave Pirate Hunchback", Enemy.CreateCavePirateHunchback),
            ("Cave Pirate Macaw", Enemy.CreateCavePirateMacaw),
            ("Cave Pirate Moll", Enemy.CreateCavePirateMoll),
            ("Cave Pirate Monkey", Enemy.CreateCavePirateMonkey),
            ("Cave Pirate Parrot", Enemy.CreateCavePirateParrot),
            ("Cave Pirate Brawler", Enemy.CreateCavePirateBrawler),
            ("Cave Pirate Sailor", Enemy.CreateCavePirateSailor),
            ("Cave Pirate Veteran", Enemy.CreateCavePirateVeteran),
            ("Pirate Lieutenant", Enemy.CreatePirateLieutenant),
            ("Pirate Commander", Enemy.CreatePirateCommander),
            ("Pirate Captain", Enemy.CreatePirateCaptain),
            ("Pirate Admiral", Enemy.CreatePirateAdmiral),

            // Snake Pit (Data/DungeonType_SnakePit.json) — same "never in
            // any BiomeData.json" isolation as Pirate Cave's own roster
            // above. Snakepit Guard/Dart Thrower aren't here — the
            // Treasure Room controller spawns them directly, never picked
            // randomly by DungeonEnemySpawner.
            ("Pit Snake", position => new PitSnake(position)),
            ("Pit Viper", position => new PitViper(position)),
            ("Greater Pit Snake", position => new GreaterPitSnake(position)),
            ("Greater Pit Viper", position => new GreaterPitViper(position)),
            ("Brown Python", position => new BrownPython(position)),
            ("Yellow Python", position => new YellowPython(position)),
            ("Fire Python", position => new FirePython(position)),
        ];

        // Cross-references a name list against BasicEnemyPool above — the
        // same lookup SpawnWave() below already does inline for the current
        // biome's own EnemyNames, pulled out so DungeonEnemySpawner (Dungeon/
        // DungeonEnemySpawner.cs) can use it too, for Data/DungeonType_
        // {Name}.json's own EnemyNames. internal, not public — the return
        // type involves Enemy, itself an internal type.
        internal static Func<Vector2, Enemy>[] ResolveFactories(string[] enemyNames) =>
            BasicEnemyPool.Where(e => enemyNames.Contains(e.name)).Select(e => e.factory).ToArray();

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
        // Interval bumped from 1800 by BeachSpawnRateMultiplier (see above)
        // as part of Beach's overall spawn-rate reduction.
        private const int BeachedBuccaneerPackInterval = (int)(1800 * BeachSpawnRateMultiplier); // ~37.5 seconds at 60fps
        private const int BeachedBuccaneerPackPirateCount = 4;
        private static int beachedBuccaneerPackCooldownRemaining = BeachedBuccaneerPackInterval;

        // The three Little Jellies each spawn as their own same-type
        // cluster ("spawns in groups of 2-7... Mean 5, Std. Deviation 1") —
        // a genuinely different shape from every earlier pack above (no
        // mini-boss, no escort, just a group of one type sampled from a
        // normal distribution) and from SpawnWave() below (a small,
        // randomly-mixed handful of different basic types). Gated to Beach
        // like every other Beach-specific pack; not part of BasicEnemyPool
        // since "spawns in groups" already fully describes how each one
        // ever appears. Intervals bumped by BeachSpawnRateMultiplier (see
        // above), same as BeachedBuccaneerPackInterval.
        private const int LittleBlueJellyPackInterval = (int)(1500 * BeachSpawnRateMultiplier); // ~31 seconds at 60fps
        private static int littleBlueJellyPackCooldownRemaining = LittleBlueJellyPackInterval;
        private const int LittleGreenJellyPackInterval = (int)(1650 * BeachSpawnRateMultiplier); // ~34 seconds at 60fps
        private static int littleGreenJellyPackCooldownRemaining = LittleGreenJellyPackInterval;
        private const int LittlePinkJellyPackInterval = (int)(1800 * BeachSpawnRateMultiplier); // ~37.5 seconds at 60fps
        private static int littlePinkJellyPackCooldownRemaining = LittlePinkJellyPackInterval;

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

        // See MaxNearbyEnemies/NearbyEnemyRadius's own comment above.
        // Computed once per Update() call (below) rather than once per
        // spawn-type check, since every check in the same frame asks the
        // exact same question against the exact same player position.
        private static bool TooManyEnemiesNearby()
        {
            int nearbyCount = 0;
            float radiusSquared = NearbyEnemyRadius * NearbyEnemyRadius;
            foreach (var position in EntityManager.EnemyPositions)
            {
                if (Vector2.DistanceSquared(position, Player.Instance.Position) <= radiusSquared)
                {
                    nearbyCount++;
                    if (nearbyCount >= MaxNearbyEnemies)
                        return true;
                }
            }
            return false;
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
                    (int)
                        MathHelper.Lerp(
                            BaseInverseSpawnChance,
                            MinDistanceInverseSpawnChance,
                            distanceFactor
                        )
                );

                // See BeachSpawnRateMultiplier's own comment above — Beach's
                // regular wave (below) gets the same 25% cooldown stretch as
                // its dedicated pack intervals, on top of the shared
                // distance-based formula every other biome still uses
                // unscaled.
                if (GetCurrentBiome()?.Name == "Beach")
                    effectiveInverseSpawnChance = (int)(
                        effectiveInverseSpawnChance * BeachSpawnRateMultiplier
                    );

                // Computed once per Update() call — see TooManyEnemiesNearby()
                // above. Every cooldown below still ticks down normally even
                // while this is true; only the actual spawn is suppressed,
                // so a wave that would have fired isn't queued up on top of
                // the next one — it's just skipped, and the next expired
                // cooldown tries again fresh.
                bool tooManyEnemiesNearby = TooManyEnemiesNearby();

                if (waveCooldownRemaining <= 0)
                {
                    if (!tooManyEnemiesNearby)
                        SpawnWave();
                    waveCooldownRemaining = effectiveInverseSpawnChance;
                }
                else
                {
                    waveCooldownRemaining--;
                }

                if (bigSnakePackCooldownRemaining <= 0)
                {
                    if (!tooManyEnemiesNearby)
                        SpawnBigSnakePack();
                    bigSnakePackCooldownRemaining = BigSnakePackInterval;
                }
                else
                {
                    bigSnakePackCooldownRemaining--;
                }

                if (beachedBuccaneerPackCooldownRemaining <= 0)
                {
                    if (GetCurrentBiome()?.Name == "Beach" && !tooManyEnemiesNearby)
                        SpawnBeachedBuccaneerPack();
                    beachedBuccaneerPackCooldownRemaining = BeachedBuccaneerPackInterval;
                }
                else
                {
                    beachedBuccaneerPackCooldownRemaining--;
                }

                if (littleBlueJellyPackCooldownRemaining <= 0)
                {
                    if (GetCurrentBiome()?.Name == "Beach" && !tooManyEnemiesNearby)
                        SpawnGroupPack(position => new LittleBlueJelly(position));
                    littleBlueJellyPackCooldownRemaining = LittleBlueJellyPackInterval;
                }
                else
                {
                    littleBlueJellyPackCooldownRemaining--;
                }

                if (littleGreenJellyPackCooldownRemaining <= 0)
                {
                    if (GetCurrentBiome()?.Name == "Beach" && !tooManyEnemiesNearby)
                        SpawnGroupPack(position => new LittleGreenJelly(position));
                    littleGreenJellyPackCooldownRemaining = LittleGreenJellyPackInterval;
                }
                else
                {
                    littleGreenJellyPackCooldownRemaining--;
                }

                if (littlePinkJellyPackCooldownRemaining <= 0)
                {
                    if (GetCurrentBiome()?.Name == "Beach" && !tooManyEnemiesNearby)
                        SpawnGroupPack(position => new LittlePinkJelly(position));
                    littlePinkJellyPackCooldownRemaining = LittlePinkJellyPackInterval;
                }
                else
                {
                    littlePinkJellyPackCooldownRemaining--;
                }

                // SpriteGod stays its own independent roll — a distinct
                // "occasional special threat" rather than part of the
                // regular basic-enemy wave pattern above. No longer scales
                // with player level. Excluded from Beach specifically —
                // Beach already has its own mini-boss (Beached Buccaneer)
                // and reclassified regular-wave heavyweights (Bandit
                // Leader/Scorpion Queen/Sandsman King/Giant Crab); a
                // SpriteGod on top of those read as out of place there.
                if (
                    GetCurrentBiome()?.Name != "Beach"
                    && !tooManyEnemiesNearby
                    && rand.Next(1500) == 0
                )
                {
                    EntityManager.Add(Enemy.CreateSpriteGod(GetSpawnPosition()));
                }

                // Cube stays its own independent roll too, same shape as
                // SpriteGod above — not biome-restricted (the real fight's
                // own lore is "hordes of sentient squares" left behind
                // everywhere, unlike SpriteGod's Sprite-forest theming), so
                // no Beach exclusion either.
                if (!tooManyEnemiesNearby && rand.Next(1500) == 0)
                {
                    EntityManager.Add(Enemy.CreateCube(GetSpawnPosition()));
                }
            }
        }

        // Spawns a small (2-4) pack of enemies clustered around one shared
        // anchor point, drawn from whichever basic types belong to the
        // biome ring the player is currently standing in (GetCurrentBiome()
        // above) — reads as a group arriving together rather than scattered
        // independently around the player.
        private static void SpawnWave()
        {
            Data.BiomeData biome = GetCurrentBiome();

            List<Func<Vector2, Enemy>> unlocked = [];
            foreach (var (name, factory) in BasicEnemyPool)
            {
                bool biomeAllows = biome == null || biome.EnemyNames.Contains(name);
                if (biomeAllows)
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

        // A cluster of same-type enemies around one shared anchor point —
        // same clustering technique as every SpawnXPack() above, but sized
        // from a normal distribution instead of a fixed escort count. First
        // real use: the three Little Jellies, all "spawns in groups of 2-7
        // ... Mean 5, Std. Deviation 1."
        private static void SpawnGroupPack(Func<Vector2, Enemy> factory)
        {
            Vector2 anchor = GetSpawnPosition();
            int count = SampleGroupSize(mean: 5, stdDev: 1, min: 2, max: 7);
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = new(rand.Next(-80, 81), rand.Next(-80, 81));
                EntityManager.Add(factory(anchor + offset));
            }
        }

        // Box-Muller transform — System.Random has no built-in Gaussian
        // sampler. Rounded to the nearest int and clamped to [min, max]
        // (the spec's own stated 2-7 range), so the Mean/Std. Deviation
        // numbers shape the distribution without ever producing an
        // out-of-range or degenerate (0-enemy) group.
        private static int SampleGroupSize(double mean, double stdDev, int min, int max)
        {
            double u1 = 1.0 - rand.NextDouble();
            double u2 = rand.NextDouble();
            double gaussian = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            int size = (int)Math.Round(mean + stdDev * gaussian);
            return Math.Clamp(size, min, max);
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
            waveCooldownRemaining = 0;
            bigSnakePackCooldownRemaining = BigSnakePackInterval;
        }
    }
}
