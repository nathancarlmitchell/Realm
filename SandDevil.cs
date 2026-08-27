using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Realm
{
    // A regular Beach wave enemy (BasicEnemyPool) with a two-phase movement
    // cycle — unusual for a basic-tier enemy (that pattern's other uses this
    // session were all mini-bosses), but nothing about its own stats/spawn
    // implies a mini-boss tier, and the spec gives it no escort.
    class SandDevil : Enemy
    {
        private enum Phase
        {
            Chase,
            Circle,
        }

        private const int ChasePhaseDuration = 180; // 3s at 60fps
        private const int CirclePhaseDuration = 180; // 3s at 60fps
        private const float CloseThreshold = 2f * 32f; // 2 tiles

        // Widened from 3 tiles after both prior spawn-distance fixes
        // (200-unit floor, then AttackRange+4-tile floor) turned out not to
        // be the real cause — confirmed directly: the actual complaint was
        // this Circle phase snapping the Sand Devil onto a tight 3-tile
        // ring around the player every ~3-second cycle once already
        // engaged, which reads as "too close" repeatedly throughout a
        // fight, independent of how far away it originally spawned. Not
        // given a specific radius in the spec at all (just "rotate
        // clockwise for 3 seconds"), so this was always a judgment call;
        // doubled to give real breathing room during the phase.
        private const float CircleRadius = 6f * 32f; // 6 tiles

        // Not given a specific rate in the spec ("rotate clockwise for 3
        // seconds") — one full lap over the 3-second Circle phase reads as
        // a clean, deliberate "circle," not a slow creep or a dizzying
        // spin. Tunable. Increasing angle = clockwise in this engine's
        // Y-down screen space (confirmed against Extensions.FromPolar's
        // plain cos/sin — matches every other "increasing angle" rotation
        // already in the codebase, e.g. LimonTheSpriteGoddess's sweeps).
        private const float CircleAngularSpeed = MathHelper.TwoPi / CirclePhaseDuration;

        private const float AttackRange = 9.75f * 32f;
        private const int AttackDamage = 10;
        private const float ProjectileSpeed = 6.5f * 32f / 60f; // 6.5 tiles/sec

        // Reported three times: Sand Devil spawns too close to the player.
        // Two prior fixes each picked a bigger arbitrary distance (a flat
        // 200-unit floor, then AttackRange + 4 tiles = 440) and each still
        // wasn't enough — the real problem was never "not a big enough
        // number," it was that any fixed distance well inside the visible
        // screen still spawns it in plain view, which reads as "too close"
        // regardless of the literal value. Tied to the actual screen size
        // this time instead: the gameplay viewport's own half-diagonal
        // (center-to-corner, the same reasoning Enemy.AggroRadius already
        // uses) is the exact distance beyond which a point can never be
        // on screen — so a Sand Devil now always spawns fully off-screen,
        // not just "far" by some arbitrary number. static readonly, not
        // const, since Vector2.Distance() isn't a compile-time constant.
        // EnemySpawner.SpawnWave()'s shared anchor+offset math only
        // guarantees ~137 units minimum in the worst case (anchor >= 250
        // from GetSpawnPosition(), offset up to ~113 toward the player) —
        // fine for most enemies, not this one. Enforced here rather than
        // in the shared spawn system, so only Sand Devil is affected.
        private static readonly float MinSpawnDistanceFromPlayer = Vector2.Distance(
            Vector2.Zero,
            new Vector2(Game1.GameplayViewportWidth / 2f, Game1.GameplayViewportHeight / 2f)
        );

        // No Cooldown given for the spinner attack at all — falls back to
        // the same 250-tick default Enemy's own shared Shoot()/Spray()/
        // ShootIfInRange() already use elsewhere. Can't just call
        // ShootIfInRange() itself here since it always constructs a plain
        // EnemyProjectile, not the WavyProjectile this attack needs.
        private const int AttackCooldown = 250;

        private Phase currentPhase = Phase.Chase;
        private int phaseTimer = ChasePhaseDuration;
        private float circleAngle;
        private int attackCooldownRemaining = 0;

        public SandDevil(Vector2 position)
            : base(Art.SandDevil, position)
        {
            health = 100;
            healthMax = 100;
            Defense = 1;
            PointValue = 8;
            DropPool = BeachDropPool;
            DropChances = BeachDropChances;
            DropTierRanges = BeachDropTierRanges;

            // Bug fix: push out to MinSpawnDistanceFromPlayer if the spawn
            // system landed this enemy closer than that — see the
            // constant's own comment above.
            Vector2 awayFromPlayer = Position - Player.Instance.Position;
            if (awayFromPlayer.LengthSquared() < MinSpawnDistanceFromPlayer * MinSpawnDistanceFromPlayer)
            {
                Vector2 direction =
                    awayFromPlayer != Vector2.Zero ? Vector2.Normalize(awayFromPlayer) : Vector2.UnitX;
                Position = Player.Instance.Position + direction * MinSpawnDistanceFromPlayer;
            }

            AddBehaviour(PhaseWatcher());
            AddAttackBehaviour(SpinnerAttack());
        }

        // Chase: closes in on the player, but swaps to erratic wandering
        // instead once within CloseThreshold ("it will wander erratically
        // if it moves within 2 tiles of the player") — a continuous check,
        // not a one-time latch, so it can dip back into a real chase if the
        // player creates distance again mid-phase. After
        // ChasePhaseDuration, switches to Circle, which repositions
        // directly onto a fixed-radius ring around the player (same
        // technique as SthenoPet.Orbit()/SandsmanArcher.Orbit()) instead of
        // accelerating there, so the transition into circling is immediate
        // rather than a slow drift into position.
        private IEnumerable<int> PhaseWatcher()
        {
            var chase = FollowPlayer(0.15f).GetEnumerator();
            var erratic = MoveRandomly().GetEnumerator();
            while (true)
            {
                if (currentPhase == Phase.Chase)
                {
                    float distSqToPlayer = Vector2.DistanceSquared(
                        Position,
                        Player.Instance.Position
                    );
                    bool tooClose = distSqToPlayer <= CloseThreshold * CloseThreshold;

                    // Bug fix: Circle snaps Position directly onto a ring
                    // CircleRadius from the player (see the else branch
                    // below) the moment this phase transitions — so
                    // whatever the actual distance is right then determines
                    // whether that snap is a no-op or a visible teleport.
                    // Two earlier attempts both still let a real teleport
                    // through: gating the transition on the timer alone let
                    // it fire from hundreds of units away (spawns are far
                    // off-screen now — see MinSpawnDistanceFromPlayer);
                    // gating the timer's own countdown on already being in
                    // range still let normal chasing keep closing in well
                    // past CircleRadius (there was nothing stopping it) all
                    // the way down toward CloseThreshold before the timer
                    // finally expired, so the eventual snap back OUT to
                    // CircleRadius was itself a ~128-unit jump — and that
                    // recurred on every Chase phase after the first, not
                    // just the initial approach. Fixed by stopping the
                    // approach itself once within CircleRadius: Chase
                    // literally cannot bring the Sand Devil any closer than
                    // the ring it's about to circle on, so by the time the
                    // timer expires and the transition fires, distance is
                    // already ≈ CircleRadius and the snap is a no-op in
                    // every practical sense — regardless of how many
                    // Chase/Circle cycles have already happened.
                    bool withinCircleRange = distSqToPlayer <= CircleRadius * CircleRadius;

                    if (tooClose)
                    {
                        erratic.MoveNext();

                        // Bug fix: MoveRandomly() is a blind random walk with
                        // no bias away from the player at all — left alone,
                        // "wander erratically" once too close could just as
                        // easily wander further in as out, including onto
                        // the player's own exact position (reported as a
                        // real bug — a Sand Devil ending up directly on the
                        // player). Clamp back out to CloseThreshold after
                        // each erratic step so the wander still looks random
                        // but can never actually converge onto the player.
                        Vector2 awayFromPlayer = Position - Player.Instance.Position;
                        if (awayFromPlayer.LengthSquared() < CloseThreshold * CloseThreshold)
                        {
                            Vector2 direction =
                                awayFromPlayer != Vector2.Zero
                                    ? Vector2.Normalize(awayFromPlayer)
                                    : Vector2.UnitX;
                            Position = Player.Instance.Position + direction * CloseThreshold;
                        }
                    }
                    else if (!withinCircleRange)
                    {
                        chase.MoveNext();
                    }
                    // else: within CircleRadius but not yet tooClose — hold
                    // here (no movement call at all, letting residual
                    // Velocity decay away naturally over the next few
                    // ticks) rather than continuing to close in, waiting
                    // out whatever's left of the Chase timer at roughly the
                    // ring's own distance.

                    if (phaseTimer <= 0)
                    {
                        if (withinCircleRange)
                        {
                            currentPhase = Phase.Circle;
                            phaseTimer = CirclePhaseDuration;
                            circleAngle = (Position - Player.Instance.Position).ToAngle();
                        }
                        // else: timer's expired but still too far away —
                        // keep chasing and re-check every subsequent tick,
                        // don't decrement further (already at/below 0).
                    }
                    else
                    {
                        phaseTimer--;
                    }
                }
                else
                {
                    // Bug fix: this branch sets Position directly onto a
                    // ring around the player every tick rather than
                    // accelerating there (see this method's own header
                    // comment) — but Enemy.Update() applies `Position +=
                    // Velocity` right after this coroutine runs, every
                    // frame, regardless of phase. Any Velocity left over
                    // from Chase (FollowPlayer/MoveRandomly both accumulate
                    // into it) was bleeding into the ring position
                    // afterward and corrupting the circle — worst right at
                    // the Chase->Circle transition, when residual Velocity
                    // is largest. Zeroing it every tick here, not just once
                    // at the transition, keeps the circle clean for its
                    // whole duration.
                    Velocity = Vector2.Zero;

                    circleAngle = MathHelper.WrapAngle(circleAngle + CircleAngularSpeed);
                    Position = Player.Instance.Position + Extensions.FromPolar(circleAngle, CircleRadius);

                    if (phaseTimer <= 0)
                    {
                        currentPhase = Phase.Chase;
                        phaseTimer = ChasePhaseDuration;
                    }
                    else
                    {
                        phaseTimer--;
                    }
                }

                yield return 0;
            }
        }

        // Only fires during Chase — the spec's Circle Phase description
        // never mentions attacking, reading as a pure repositioning/breather
        // window instead.
        private IEnumerable<int> SpinnerAttack()
        {
            while (true)
            {
                if (currentPhase == Phase.Chase)
                {
                    var aim = Player.Instance.Position - Position;
                    if (
                        aim.LengthSquared() > 0
                        && aim.LengthSquared() <= AttackRange * AttackRange
                        && attackCooldownRemaining <= 0
                    )
                    {
                        attackCooldownRemaining = AttackCooldown;
                        float aimAngle = aim.ToAngle();
                        Vector2 vel = Extensions.FromPolar(aimAngle, ProjectileSpeed);
                        EntityManager.Add(
                            new WavyProjectile(Position, vel, Art.DarkGraySpinner)
                            {
                                Damage = AttackDamage,
                                // The user's own request: "the sand devil
                                // attack should apply the unstable effect
                                // for 1 second" — UnstablesOnHit uses
                                // Player.Destabilize()'s own default
                                // duration (60 frames = 1s at 60fps), so no
                                // explicit duration needs restating here.
                                UnstablesOnHit = true,
                            }
                        );
                    }

                    if (attackCooldownRemaining > 0)
                        attackCooldownRemaining--;
                }

                yield return 0;
            }
        }
    }
}
