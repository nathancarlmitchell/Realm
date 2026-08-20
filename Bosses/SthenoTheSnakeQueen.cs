using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm;

namespace Realm.Bosses
{
    // The second boss, spawned inside BossRealmState (entered via the
    // portal BigSnake drops on death — see Enemy.CreateBigSnake()).
    // Stationary — nothing in the spec has her moving. Cycles through 3
    // mutually-exclusive, time-based phases (unlike LimonTheSpriteGoddess's
    // health-threshold, additive phase), each of Stheno's own attack
    // coroutines gated on the current phase, not being Invulnerable
    // (briefly true during every phase transition), and the player standing
    // in PlayerInCenter. Two kinds of adds: SthenoPet (orbiting, replenished
    // by MaintainPets() below) and SthenoSwarm (summoned on every phase
    // change).
    class SthenoTheSnakeQueen : Boss
    {
        private static readonly Random rand = new();

        private enum Phase
        {
            Blades,
            Bursts,
            Spiral,
        }

        private Phase currentPhase = Phase.Blades;

        // The arena's true center, recovered from BossRealmState's fixed
        // spawn offset (position = center + (0,-600)) rather than
        // hardcoding or coupling to its constants directly.
        private readonly Vector2 roomCenter;

        private const float CenterRadius = 500f;

        // "If her target backs out of the center of the room, she will
        // stop firing" — checked by each of Stheno's own attack coroutines
        // below (not by her adds, which always act regardless).
        private bool PlayerInCenter =>
            Vector2.DistanceSquared(Player.Instance.Position, roomCenter)
            <= CenterRadius * CenterRadius;

        private const int PhaseDurationFrames = 900; // ~15s at 60fps
        private const int TransitionFrames = 90; // ~1.5s — the invulnerable window
        private const int SwarmsPerPhaseChange = 3;

        private const int TargetPetCount = 6;

        public SthenoTheSnakeQueen(Vector2 position)
            : base(Art.Stheno, position)
        {
            Name = "Stheno the Snake Queen";
            Description =
                "Unlike her sister, Stheno took to a more reclusive life in the depths of a "
                + "ruined temple. She lacks the raw power of Medusa, but her skilled handling "
                + "of dual blades led Oryx to make her a general.";

            health = 9000;
            healthMax = 9000;
            Defense = 19;
            PointValue = 3000;

            // No dedicated Stheno audio yet — reuses the same placeholder
            // the snake family already does, swap once real audio exists.
            deathSound = Sound.SnakesDeath;
            hitSound = Sound.SnakesHit;

            roomCenter = position + new Vector2(0, 600);

            AddBehaviour(PhaseTimer());
            AddBehaviour(MaintainPets());
            AddAttackBehaviour(FireBladePairs());
            AddAttackBehaviour(RapidGrenades());
            AddAttackBehaviour(GrenadeBursts());
            AddAttackBehaviour(AimedBombs());
            AddAttackBehaviour(Spiral());
        }

        // Runs the current phase for PhaseDurationFrames, then becomes
        // briefly Invulnerable, summons 3 Swarms, holds for
        // TransitionFrames, and advances to the next phase — repeating
        // forever. Swarms spawn only on the transition itself ("every time
        // she changes phases"), not on the initial entry into Blades.
        private IEnumerable<int> PhaseTimer()
        {
            while (true)
            {
                for (int i = 0; i < PhaseDurationFrames; i++)
                    yield return 0;

                Invulnerable = true;
                SpawnSwarms(SwarmsPerPhaseChange);

                for (int i = 0; i < TransitionFrames; i++)
                    yield return 0;

                currentPhase = currentPhase switch
                {
                    Phase.Blades => Phase.Bursts,
                    Phase.Bursts => Phase.Spiral,
                    _ => Phase.Blades,
                };
                Invulnerable = false;
            }
        }

        private void SpawnSwarms(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Extensions.FromPolar(i * (MathHelper.TwoPi / count), 60f);
                EntityManager.Add(new SthenoSwarm(Position + offset));
            }
        }

        // Tops the live SthenoPet count back up to TargetPetCount every
        // frame — covers both "spawn several on room entry" (starts at 0)
        // and "respawn immediately on death" (tops up every subsequent
        // tick) with one mechanism. Not gated on PlayerInCenter — pet
        // respawning isn't one of "her own attacks."
        private IEnumerable<int> MaintainPets()
        {
            while (true)
            {
                int missing = TargetPetCount - EntityManager.CountWhere<SthenoPet>(_ => true);
                for (int i = 0; i < missing; i++)
                    EntityManager.Add(new SthenoPet(this, rand.NextFloat(0, MathHelper.TwoPi)));

                yield return 0;
            }
        }

        // Shared by every phase's grenade attack — a stationary, larger-
        // than-normal-radius hazard alive for its whole duration. No fuse/
        // telegraph exists in this engine, so "AoE damage" comes from the
        // bigger radius, not a delayed detonation.
        private void SpawnGrenade(Vector2 position, int damage, float radius, int duration = 90)
        {
            EntityManager.Add(
                new EnemyProjectile(position, Vector2.Zero, Art.RedFire)
                {
                    Damage = damage,
                    Radius = radius,
                    duration = duration,
                }
            );
        }

        // Phase 1a: 4 directions (rotated so one always faces the nearest
        // player), each firing a tight pair of blades.
        private int bladeCooldownRemaining = 0;
        private const int BladeCooldown = 20;
        private const float BladePairSpread = 0.06f; // rad, half-separation within a pair
        private const float BladeSpeed = 5f;
        private const int BladeDamage = 20;
        private const int BladeDuration = 90;

        private IEnumerable<int> FireBladePairs()
        {
            while (true)
            {
                if (currentPhase == Phase.Blades && !Invulnerable && PlayerInCenter)
                {
                    if (bladeCooldownRemaining <= 0)
                    {
                        Vector2 aim = Player.Instance.Position - Position;
                        if (aim.LengthSquared() > 0)
                        {
                            bladeCooldownRemaining = BladeCooldown;
                            float aimAngle = aim.ToAngle();
                            for (int i = 0; i < 4; i++)
                            {
                                float dirAngle = aimAngle + i * MathHelper.PiOver2;
                                FireBlade(dirAngle - BladePairSpread);
                                FireBlade(dirAngle + BladePairSpread);
                            }
                        }
                    }

                    if (bladeCooldownRemaining > 0)
                        bladeCooldownRemaining--;
                }

                yield return 0;
            }
        }

        private void FireBlade(float angle)
        {
            EntityManager.Add(
                new EnemyProjectile(Position, Extensions.FromPolar(angle, BladeSpeed), Art.SwordSlash)
                {
                    Damage = BladeDamage,
                    duration = BladeDuration,
                }
            );
        }

        // Phase 1b: rapid grenades scattered near the player (not exactly
        // on them — the engine has no telegraph, so a dead-on instant
        // grenade would be unavoidable).
        private int rapidGrenadeCooldownRemaining = 0;
        private const int RapidGrenadeCooldown = 40;
        private const int RapidGrenadeDamage = 90;
        private const float RapidGrenadeRadius = 60f;

        private IEnumerable<int> RapidGrenades()
        {
            while (true)
            {
                if (currentPhase == Phase.Blades && !Invulnerable && PlayerInCenter)
                {
                    if (rapidGrenadeCooldownRemaining <= 0)
                    {
                        rapidGrenadeCooldownRemaining = RapidGrenadeCooldown;
                        Vector2 offset = rand.NextVector2(80f, 220f);
                        SpawnGrenade(
                            Player.Instance.Position + offset,
                            RapidGrenadeDamage,
                            RapidGrenadeRadius
                        );
                    }

                    if (rapidGrenadeCooldownRemaining > 0)
                        rapidGrenadeCooldownRemaining--;
                }

                yield return 0;
            }
        }

        // Phase 2: bursts of grenades placed around 2 points along each of
        // 4 sides of a diamond or square (alternating), skipping corners
        // and midpoints so every side has a real gap to duck through — same
        // corner-offset technique as LimonTheSpriteGoddess.SpawnSquareWall(),
        // applied to static placement instead of a sweeping motion.
        private bool nextBurstIsSquare = false;
        private int burstCooldownRemaining = 0;
        private const int BurstCooldown = 150;
        private const float BurstRadius = 400f;
        private const int BurstDamage = 100;
        private const float BurstGrenadeRadius = 70f;

        private IEnumerable<int> GrenadeBursts()
        {
            while (true)
            {
                if (currentPhase == Phase.Bursts && !Invulnerable && PlayerInCenter)
                {
                    if (burstCooldownRemaining <= 0)
                    {
                        burstCooldownRemaining = BurstCooldown;
                        SpawnBurst(nextBurstIsSquare);
                        nextBurstIsSquare = !nextBurstIsSquare;
                    }

                    if (burstCooldownRemaining > 0)
                        burstCooldownRemaining--;
                }

                yield return 0;
            }
        }

        private void SpawnBurst(bool square)
        {
            Vector2[] corners = new Vector2[4];
            if (square)
            {
                float r = BurstRadius * (float)Math.Sqrt(2);
                for (int i = 0; i < 4; i++)
                    corners[i] =
                        Position + Extensions.FromPolar(MathHelper.PiOver4 + i * MathHelper.PiOver2, r);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                    corners[i] = Position + Extensions.FromPolar(i * MathHelper.PiOver2, BurstRadius);
            }

            for (int side = 0; side < 4; side++)
            {
                Vector2 start = corners[side];
                Vector2 end = corners[(side + 1) % 4];
                SpawnGrenade(Vector2.Lerp(start, end, 0.3f), BurstDamage, BurstGrenadeRadius);
                SpawnGrenade(Vector2.Lerp(start, end, 0.7f), BurstDamage, BurstGrenadeRadius);
            }
        }

        // Phase 3a: grenades thrown exactly at the player — unlike phase
        // 1's scattered throw, this one is aimed dead-on.
        private int aimedBombCooldownRemaining = 0;
        private const int AimedBombCooldown = 50;
        private const int AimedBombDamage = 80;
        private const float AimedBombRadius = 55f;

        private IEnumerable<int> AimedBombs()
        {
            while (true)
            {
                if (currentPhase == Phase.Spiral && !Invulnerable && PlayerInCenter)
                {
                    if (aimedBombCooldownRemaining <= 0)
                    {
                        aimedBombCooldownRemaining = AimedBombCooldown;
                        SpawnGrenade(Player.Instance.Position, AimedBombDamage, AimedBombRadius);
                    }

                    if (aimedBombCooldownRemaining > 0)
                        aimedBombCooldownRemaining--;
                }

                yield return 0;
            }
        }

        // Phase 3b: 6 evenly-spaced purple orbs; spiralAngleOffset persists
        // and advances every volley (never resets), which is what makes it
        // read as a rotating spiral instead of a static pulsing ring.
        private int spiralCooldownRemaining = 0;
        private const int SpiralCooldown = 12;
        private float spiralAngleOffset = 0f;
        private const float SpiralRotationStep = 0.1f;
        private const int SpiralDirections = 6;
        private const float SpiralSpeed = 4f;
        private const int SpiralDamage = 25;
        private const int SpiralDuration = 150;

        private IEnumerable<int> Spiral()
        {
            while (true)
            {
                if (currentPhase == Phase.Spiral && !Invulnerable && PlayerInCenter)
                {
                    if (spiralCooldownRemaining <= 0)
                    {
                        spiralCooldownRemaining = SpiralCooldown;

                        for (int i = 0; i < SpiralDirections; i++)
                        {
                            float angle =
                                i * (MathHelper.TwoPi / SpiralDirections) + spiralAngleOffset;
                            EntityManager.Add(
                                new EnemyProjectile(
                                    Position,
                                    Extensions.FromPolar(angle, SpiralSpeed),
                                    Art.PurpleMagic
                                )
                                {
                                    Damage = SpiralDamage,
                                    duration = SpiralDuration,
                                }
                            );
                        }

                        spiralAngleOffset = MathHelper.WrapAngle(
                            spiralAngleOffset + SpiralRotationStep
                        );
                    }

                    if (spiralCooldownRemaining > 0)
                        spiralCooldownRemaining--;
                }

                yield return 0;
            }
        }
    }
}
