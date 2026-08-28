using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm;
using Realm.Projectiles;

namespace Realm.Bosses
{
    // A regular Beach wave enemy (EnemySpawner.BasicEnemyPool) — no longer a
    // dedicated mini-boss spawn; Beached Buccaneer is now the only Beach
    // mini-boss. Its own dedicated class rather than a bare Enemy.CreateX()
    // factory for its bespoke phase-cycling attack state; has no escort of
    // its own (no "Spawns:" field in the spec). Introduces Aim Tracking: its
    // main attack fires at the player's *predicted* position (current
    // Position + current Velocity extrapolated forward), not their current
    // one — the player's own counter-play is deliberately changing direction
    // right as it fires so the volley lands where they used to be headed.
    class GiantCrab : Enemy
    {
        private enum Phase
        {
            Beam,
            BlueBolt,
        }

        // "When they spot you" — no explicit detection range given; reuses
        // Blue Bolt's own Range (12.6 tiles, the single largest number in
        // the spec) as the aggro trigger, so the crab is never immediately
        // out of its own longest-ranged attack the instant it aggroes.
        // Tunable.
        private const float SpotRange = 12.6f * 32f;

        // Applied once at the exact moment a beam wave fires, not
        // continuously re-aimed — that's what makes "move one way, then the
        // other right as it's about to fire" an actual dodge instead of a
        // no-op. No specific lookahead value was given; tunable.
        private const int PredictionLookaheadTicks = 30;

        // "Occasionally... at a frequent pace" gives no explicit
        // durations/cooldowns for either phase — Beam is the default,
        // sustained state ("occasionally" reads as the rarer one); Blue
        // Bolt is a shorter, faster-firing interlude. All four values below
        // are judgment calls, tunable.
        private const int BeamPhaseDuration = 480; // ~8s
        private const int BeamVolleyCooldown = 90; // ~1.5s between waves
        private const int BlueBoltPhaseDuration = 150; // ~2.5s
        private const int BlueBoltCooldown = 20; // ~0.33s between shots

        // The four "Beam" rows read as one simultaneous volley, not four
        // independent attacks — "a shockwave-like blast... if all four
        // connect" only makes sense as one wave. Range/Speed happen to give
        // a clean linear duration progression (0.2s/0.4s/0.6s/0.8s), which
        // is what actually produces the spreading "shockwave" look: all
        // four fire from the same point at the same instant toward the same
        // predicted spot, but the faster/farther-reaching ones outlast the
        // slower/shorter ones.
        private static readonly (int damage, float speed, float range)[] BeamTiers =
        [
            (1, 2f * 32f / 60f, 0.4f * 32f),
            (4, 4f * 32f / 60f, 1.6f * 32f),
            (7, 6f * 32f / 60f, 3.6f * 32f),
            (11, 8f * 32f / 60f, 6.4f * 32f),
        ];

        private const int BlueBoltDamage = 10;
        private const float BlueBoltSpeed = 7f * 32f / 60f; // 7 tiles/sec
        private const float BlueBoltRange = 12.6f * 32f;

        private bool hasAggroed = false;
        private Phase currentPhase = Phase.Beam;
        private int phaseTimer = BeamPhaseDuration;
        private int volleyCooldownRemaining = 0;
        private int blueBoltCooldownRemaining = 0;

        public GiantCrab(Vector2 position)
            : base(Art.GiantCrab, position)
        {
            health = 300;
            healthMax = 300;
            Defense = 2;
            PointValue = 86;
            DropPool = BeachDropPool;
            DropChances = BeachDropChances;
            DropTierRanges = BeachDropTierRanges;

            AddBehaviour(AggroWatcher());
            AddBehaviour(PhaseTimer());
            AddAttackBehaviour(BeamAttack());
            AddAttackBehaviour(BlueBoltAttack());
        }

        // Same one-way wander-then-chase latch as BeachedBuccaneer/
        // SandsmanKing — "when they spot you, they will chase" implies a
        // real pre-aggro state, unlike Pirate/Little Scorpion's always-on
        // range-gated fire with no separate "noticing" phase.
        private IEnumerable<int> AggroWatcher()
        {
            var wander = MoveTethered(wanderDistance: 200f, speed: 0.1f).GetEnumerator();
            var chase = FollowPlayer(0.15f).GetEnumerator();
            while (true)
            {
                if (
                    !hasAggroed
                    && Vector2.DistanceSquared(Player.Instance.Position, Position)
                        <= SpotRange * SpotRange
                )
                    hasAggroed = true;

                if (hasAggroed)
                    chase.MoveNext();
                else
                    wander.MoveNext();

                yield return 0;
            }
        }

        // Only actually cycles once aggroed — no point ticking down a
        // phase timer while still wandering, unengaged.
        private IEnumerable<int> PhaseTimer()
        {
            while (true)
            {
                if (hasAggroed)
                {
                    if (phaseTimer <= 0)
                    {
                        currentPhase = currentPhase == Phase.Beam ? Phase.BlueBolt : Phase.Beam;
                        phaseTimer =
                            currentPhase == Phase.Beam ? BeamPhaseDuration : BlueBoltPhaseDuration;
                    }
                    else
                    {
                        phaseTimer--;
                    }
                }

                yield return 0;
            }
        }

        // No distance gate here (unlike ShootIfInRange) — each beam tier's
        // own short duration already limits how far it can possibly travel,
        // so a wave fired at a far-away predicted point is naturally
        // harmless without needing a separate range check on the volley
        // itself.
        private IEnumerable<int> BeamAttack()
        {
            while (true)
            {
                if (hasAggroed && currentPhase == Phase.Beam)
                {
                    if (volleyCooldownRemaining <= 0)
                    {
                        FireBeamWave();
                        volleyCooldownRemaining = BeamVolleyCooldown;
                    }
                    else
                    {
                        volleyCooldownRemaining--;
                    }
                }

                yield return 0;
            }
        }

        private void FireBeamWave()
        {
            Vector2 predicted =
                Player.Instance.Position + Player.Instance.Velocity * PredictionLookaheadTicks;
            Vector2 aim = predicted - Position;
            if (aim.LengthSquared() <= 0)
                return;

            float aimAngle = aim.ToAngle();
            foreach (var (damage, speed, range) in BeamTiers)
            {
                Vector2 vel = Extensions.FromPolar(aimAngle, speed);
                EntityManager.Add(
                    new EnemyProjectile(Position, vel, Art.Beam, CollisionShape.Rectangle)
                    {
                        Damage = damage,
                        duration = (int)(range / speed),
                    }
                );
            }
        }

        // "Will not track your movement" — aimed at the player's real,
        // current Position each shot (no prediction), unlike BeamAttack()
        // above.
        private IEnumerable<int> BlueBoltAttack()
        {
            while (true)
            {
                if (hasAggroed && currentPhase == Phase.BlueBolt)
                {
                    var aim = Player.Instance.Position - Position;
                    if (
                        aim.LengthSquared() > 0
                        && aim.LengthSquared() <= BlueBoltRange * BlueBoltRange
                        && blueBoltCooldownRemaining <= 0
                    )
                    {
                        blueBoltCooldownRemaining = BlueBoltCooldown;
                        float aimAngle = aim.ToAngle();
                        Vector2 vel = Extensions.FromPolar(aimAngle, BlueBoltSpeed);
                        EntityManager.Add(
                            new EnemyProjectile(
                                Position,
                                vel,
                                Art.BlueBolt,
                                CollisionShape.Rectangle
                            )
                            {
                                Damage = BlueBoltDamage,
                                duration = (int)(BlueBoltRange / BlueBoltSpeed),
                            }
                        );
                    }

                    if (blueBoltCooldownRemaining > 0)
                        blueBoltCooldownRemaining--;
                }

                yield return 0;
            }
        }
    }
}
