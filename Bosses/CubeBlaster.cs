using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm;
using Realm.Projectiles;

namespace Realm.Bosses
{
    // The other of Cube Overseer's two escort types (see
    // CubeOverseer.MaintainMinions()) — https://www.realmeye.com/wiki/cube-blaster.
    // Real attacks from the wiki's own combat table. Movement is a
    // deliberate departure from this class's own entry-259 guess
    // (tethering near its Overseer via MoveTethered) — per direct request,
    // it now orbits its Overseer at ~2-3 tiles, with a smoothly-wobbling
    // radius/speed and an occasional direction flip. Re-derives Position
    // from the Overseer's own live position every frame, same technique
    // SthenoPet.Orbit() already established; the wobble is the same
    // sine-based "twinkle" modulation SwirlParticle already uses for its
    // own sparkle look, applied here to radius and angular speed instead
    // of scale/alpha. Same continuously-replenished PointValue/DropsLoot
    // convention as CubeDefender.
    class CubeBlaster : Enemy
    {
        private static readonly Random rand = new();

        public CubeOverseer Owner { get; private set; }

        // "Rotate around its parent at about 2-3 tiles distance, with
        // slight variations in speed and jitter, occasionally switching
        // rotation directions" — baseOrbitRadius randomized once per
        // instance within that 2-3 tile range (64-96px); a slow sine
        // wobble layered on top of both radius and angular speed for
        // continuous "variation" (re-randomizing either one independently
        // every tick would read as noise, not a smooth variation); a
        // separate small random per-tick position offset for "jitter"; and
        // a periodic, randomly-timed sign flip on the orbit direction.
        private readonly float baseOrbitRadius;
        private float orbitAngle;
        private float orbitDirection;
        private float radiusWobblePhase;
        private float speedWobblePhase;
        private int directionSwitchCooldownRemaining;
        private const float OrbitRadiusJitterAmplitude = 8f;
        private const float RadiusWobbleSpeed = 0.05f; // radians/tick
        private const float BaseAngularSpeed = 0.03f; // radians/tick
        private const float SpeedWobbleAmplitude = 0.4f; // +/-40% of base
        private const float SpeedWobbleSpeed = 0.02f; // radians/tick
        private const float PositionJitterMagnitude = 2f; // px, per tick
        private const int MinDirectionSwitchFrames = 180; // 3s
        private const int MaxDirectionSwitchFrames = 420; // 7s

        // Re-homing to a new (possibly distant) Overseer would otherwise
        // make the next orbit tick snap Position straight to the new
        // parent's own orbit point — a visible teleport. Instead, the
        // moment Owner changes, OrbitOwner() below eases from wherever this
        // Blaster actually is toward the freshly-computed orbit target over
        // RehomeTransitionFrames, rather than jumping there instantly.
        private const int RehomeTransitionFrames = 30; // 0.5s
        private int rehomeTransitionRemaining = 0;
        private Vector2 rehomeTransitionStart;

        // Speed/range converted from the wiki's own tiles/sec and tiles
        // values (32px/tile, 60 ticks/sec) — real numbers, not guesses. No
        // cadence is given for either shot — the two Cooldown consts below
        // are first-pass tunables.
        private int starCooldownRemaining = 0;
        private const int StarCooldown = 70;
        private const int StarDamage = 10;
        private const float StarSpeed = 6f * 32f / 60f; // 6 tiles/sec
        private const int StarDuration = 162; // 16.2-tile range / speed

        private int wavyCooldownRemaining = 0;
        private const int WavyCooldown = 60;
        private const int WavyDamage = 40;
        private const float WavySpeed = 10f * 32f / 60f; // 10 tiles/sec
        private const int WavyDuration = 144; // 24-tile range / speed

        public CubeBlaster(CubeOverseer owner, Vector2 position)
            : base(Art.CubeBlaster, position)
        {
            Owner = owner;

            health = 500;
            healthMax = 500;
            Defense = 0;
            PointValue = 5;
            DropsLoot = false;

            baseOrbitRadius = rand.NextFloat(64f, 96f); // 2-3 tiles
            orbitAngle = rand.NextFloat(0f, MathHelper.TwoPi);
            orbitDirection = rand.Next(2) == 0 ? 1f : -1f;
            radiusWobblePhase = rand.NextFloat(0f, MathHelper.TwoPi);
            speedWobblePhase = rand.NextFloat(0f, MathHelper.TwoPi);
            directionSwitchCooldownRemaining = rand.Next(
                MinDirectionSwitchFrames,
                MaxDirectionSwitchFrames
            );

            AddBehaviour(MaintainOwner());
            AddBehaviour(OrbitOwner());
            AddAttackBehaviour(StarAttack());
            AddAttackBehaviour(WavyAttack());
        }

        // Re-homes to the nearest still-alive Overseer the instant its own
        // dies — per direct request, matching the boss page's own tips
        // ("when their Overseer dies, the protecting Cubes will search for
        // a new Overseer... or stand almost still" if none exist, which
        // OrbitOwner() below leaves as a no-op — nothing to orbit).
        private IEnumerable<int> MaintainOwner()
        {
            while (true)
            {
                if (Owner == null || Owner.IsExpired)
                {
                    var nearest = CubeOverseer.FindNearest(Position);
                    if (nearest != null)
                    {
                        rehomeTransitionStart = Position;
                        rehomeTransitionRemaining = RehomeTransitionFrames;
                        Owner = nearest;
                    }
                }

                yield return 0;
            }
        }

        private IEnumerable<int> OrbitOwner()
        {
            while (true)
            {
                if (Owner != null && !Owner.IsExpired)
                {
                    if (directionSwitchCooldownRemaining <= 0)
                    {
                        orbitDirection = -orbitDirection;
                        directionSwitchCooldownRemaining = rand.Next(
                            MinDirectionSwitchFrames,
                            MaxDirectionSwitchFrames
                        );
                    }
                    else
                    {
                        directionSwitchCooldownRemaining--;
                    }

                    speedWobblePhase += SpeedWobbleSpeed;
                    float speedMultiplier = 1f + SpeedWobbleAmplitude * (float)Math.Sin(speedWobblePhase);
                    orbitAngle = MathHelper.WrapAngle(
                        orbitAngle + orbitDirection * BaseAngularSpeed * speedMultiplier
                    );

                    radiusWobblePhase += RadiusWobbleSpeed;
                    float radius =
                        baseOrbitRadius + OrbitRadiusJitterAmplitude * (float)Math.Sin(radiusWobblePhase);

                    Vector2 jitter = rand.NextVector2(0f, PositionJitterMagnitude);
                    Vector2 targetPosition = Owner.Position + Extensions.FromPolar(orbitAngle, radius) + jitter;

                    if (rehomeTransitionRemaining > 0)
                    {
                        float t = 1f - (rehomeTransitionRemaining / (float)RehomeTransitionFrames);
                        Position = Vector2.Lerp(rehomeTransitionStart, targetPosition, t);
                        rehomeTransitionRemaining--;
                    }
                    else
                    {
                        Position = targetPosition;
                    }
                }

                yield return 0;
            }
        }

        // "A star projectile that inflicts Slow" — Player.Slow() via
        // SlowsOnHit, same shape as SthenoPet.TrailOrbs()'s own slow orb.
        private IEnumerable<int> StarAttack()
        {
            while (true)
            {
                if (!Invulnerable && starCooldownRemaining <= 0)
                {
                    Vector2 aim = Player.Instance.Position - Position;
                    if (aim.LengthSquared() > 0)
                    {
                        starCooldownRemaining = StarCooldown;
                        Vector2 vel = Extensions.FromPolar(aim.ToAngle(), StarSpeed);
                        EntityManager.Add(
                            new EnemyProjectile(Position, vel, Art.GreenStar)
                            {
                                Damage = StarDamage,
                                duration = StarDuration,
                                SlowsOnHit = true,
                            }
                        );
                    }
                }

                if (starCooldownRemaining > 0)
                    starCooldownRemaining--;

                yield return 0;
            }
        }

        // "Wavy shots" — reuses WavyProjectile, same technique
        // CubeDefender's own WavyAttack()/SandDevil.SpinnerAttack() use for
        // the same wiki wording.
        private IEnumerable<int> WavyAttack()
        {
            while (true)
            {
                if (!Invulnerable && wavyCooldownRemaining <= 0)
                {
                    Vector2 aim = Player.Instance.Position - Position;
                    if (aim.LengthSquared() > 0)
                    {
                        wavyCooldownRemaining = WavyCooldown;
                        Vector2 vel = Extensions.FromPolar(aim.ToAngle(), WavySpeed);
                        EntityManager.Add(
                            new WavyProjectile(Position, vel, Art.YellowMagic)
                            {
                                Damage = WavyDamage,
                                duration = WavyDuration,
                                Shape = CollisionShape.Rectangle,
                            }
                        );
                    }
                }

                if (wavyCooldownRemaining > 0)
                    wavyCooldownRemaining--;

                yield return 0;
            }
        }
    }
}
