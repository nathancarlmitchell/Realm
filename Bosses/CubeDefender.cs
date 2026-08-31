using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm;
using Realm.Projectiles;

namespace Realm.Bosses
{
    // One of Cube Overseer's two escort types (see
    // CubeOverseer.MaintainMinions()) — https://www.realmeye.com/wiki/cube-defender.
    // Real attack from the wiki's own combat table (a wavy shot). Movement
    // is a deliberate departure from both the wiki's own blank "Behavior:
    // TBA" and this class's own entry-259 guess (tethering near its
    // Overseer) — per direct request, it now darts erratically toward and
    // away from the player in short bursts while it keeps shooting, rather
    // than orbiting anything. Continuously replenished like SthenoPet/
    // SthenoSwarm, so PointValue/DropsLoot follow that same "don't let it
    // be farmed" convention rather than LittleScorpion's (which does drop
    // normal loot) — the wiki's own EXP value is real-game flavor, not
    // something this engine's anti-farming convention should import.
    class CubeDefender : Enemy
    {
        private static readonly Random rand = new();

        public CubeOverseer Owner { get; private set; }

        // "Quickly jumping forward and backward from the player" — a
        // short, strong Velocity impulse toward or away from the player
        // (randomly chosen each cycle), then a brief pause while the
        // engine's own Velocity *= 0.8 decay (Enemy.Update()) carries the
        // jump out and slows it back down — a one-shot impulse reads as a
        // "jump," unlike a sustained multi-frame acceleration, which would
        // just look like a smooth drift.
        private const int MinDashPauseFrames = 20;
        private const int MaxDashPauseFrames = 50;
        private const float DashImpulse = 8f;

        // Speed/range converted from the wiki's own tiles/sec and tiles
        // values (32px/tile, 60 ticks/sec) — real numbers, not guesses. No
        // cadence is given for the shot itself — AttackCooldown is a
        // first-pass tunable.
        private int attackCooldownRemaining = 0;
        private const int AttackCooldown = 45;
        private const int AttackDamage = 50;
        private const float AttackSpeed = 10f * 32f / 60f; // 10 tiles/sec
        private const int AttackDuration = 144; // 24-tile range / speed

        public CubeDefender(CubeOverseer owner, Vector2 position)
            : base(Art.CubeDefender, position)
        {
            Owner = owner;

            health = 1000;
            healthMax = 1000;
            Defense = 0;
            PointValue = 10;
            DropsLoot = false;

            drawScale = 0.75f;
            Radius = image.Width * drawScale / 2;

            AddBehaviour(MaintainOwner());
            AddBehaviour(ErraticDash());
            AddAttackBehaviour(WavyAttack());
        }

        // Re-homes to the nearest still-alive Overseer the instant its own
        // dies — per direct request, matching the boss page's own tips
        // ("when their Overseer dies, the protecting Cubes will search for
        // a new Overseer... or stand almost still" if none exist, which
        // CubeOverseer.FindNearest() returning null naturally leaves as a
        // no-op here — Owner just stays null/expired until one exists).
        private IEnumerable<int> MaintainOwner()
        {
            while (true)
            {
                if (Owner == null || Owner.IsExpired)
                {
                    var nearest = CubeOverseer.FindNearest(Position);
                    if (nearest != null)
                        Owner = nearest;
                }

                yield return 0;
            }
        }

        private IEnumerable<int> ErraticDash()
        {
            while (true)
            {
                int pauseFrames = rand.Next(MinDashPauseFrames, MaxDashPauseFrames);
                for (int i = 0; i < pauseFrames; i++)
                    yield return 0;

                Vector2 toPlayer = Player.Instance.Position - Position;
                if (toPlayer.LengthSquared() > 0)
                {
                    Vector2 direction =
                        Vector2.Normalize(toPlayer) * (rand.Next(2) == 0 ? 1f : -1f);
                    Velocity += direction * DashImpulse;
                }
            }
        }

        // "Wavy shots" — reuses WavyProjectile, the same technique
        // SandDevil.cs's SpinnerAttack() already established for the exact
        // same wiki wording.
        private IEnumerable<int> WavyAttack()
        {
            while (true)
            {
                if (!Invulnerable && attackCooldownRemaining <= 0)
                {
                    Vector2 aim = Player.Instance.Position - Position;
                    if (aim.LengthSquared() > 0)
                    {
                        attackCooldownRemaining = AttackCooldown;
                        Vector2 vel = Extensions.FromPolar(aim.ToAngle(), AttackSpeed);
                        EntityManager.Add(
                            new WavyProjectile(Position, vel, Art.CyanMagic)
                            {
                                Damage = AttackDamage,
                                duration = AttackDuration,
                                Shape = CollisionShape.Rectangle,
                            }
                        );
                    }
                }

                if (attackCooldownRemaining > 0)
                    attackCooldownRemaining--;

                yield return 0;
            }
        }
    }
}
