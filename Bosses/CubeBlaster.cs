using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm;
using Realm.Projectiles;

namespace Realm.Bosses
{
    // The other of Cube Overseer's two escort types (see
    // CubeOverseer.MaintainMinions()) — https://www.realmeye.com/wiki/cube-blaster.
    // Real stats/attacks this time, replacing entry 257's first-pass guess
    // (a single straight `ShootIfInRange` shot — this page didn't exist to
    // check yet). Two independent attacks per the wiki's own combat table:
    // a slow-inflicting "star" shot and a wavy shot, each needing its own
    // cooldown field (same reasoning as CubeGod's own two-volley
    // ShotgunVolleys() — a shared cooldown can only serve one attack).
    // Movement tethers to its own Overseer, same reasoning as
    // CubeDefender's own comment. Same continuously-replenished
    // PointValue/DropsLoot convention as CubeDefender.
    class CubeBlaster : Enemy
    {
        public CubeOverseer Owner { get; }

        private const float WanderDistance = 100f;
        private const float WanderSpeed = 0.1f;

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

            AddBehaviour(
                MoveTethered(wanderDistance: WanderDistance, speed: WanderSpeed, anchor: owner)
            );
            AddAttackBehaviour(StarAttack());
            AddAttackBehaviour(WavyAttack());
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
