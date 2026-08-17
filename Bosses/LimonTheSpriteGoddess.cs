using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm;

namespace Realm.Bosses
{
    // The first boss, spawned inside BossRealmState (entered via the portal
    // SpriteGod drops on death — see Enemy.WasShot()). Two-phase fight:
    // kites at range with Spray() above 50% health; PhaseWatcher() adds a
    // second, faster attack (BossBurst()) once health drops below that,
    // escalating the fight rather than just being a bigger version of an
    // existing enemy. SquareWall() and XCross() both run for the whole
    // fight regardless of phase — a constant, ever-present hazard around
    // (and through) the boss rather than an escalation.
    class LimonTheSpriteGoddess : Boss
    {
        // Dedicated cooldown, separate from the projectileCooldownRemaining
        // Spray()/Shoot()/Bomb() share on the base Enemy class — running
        // Spray() and BossBurst() at the same time needs its own timer, or
        // the two attacks would fight over one shared cooldown instead of
        // firing independently.
        private int bossBurstCooldownRemaining = 0;
        private readonly int bossBurstCooldown = 300;

        // SquareWall()'s own cooldown, same reasoning — it runs continuously
        // alongside Spray()/BossBurst() and can't share their timer either.
        // Matches wallTravelFrames exactly so the next sweep launches right
        // as the previous one finishes crossing its line, with no gap.
        private int wallCooldownRemaining = 0;
        private readonly int wallCooldown = 10;

        // Distance from the boss's center to each side of the wall, and how
        // long (in frames) each wall projectile takes to cross from one end
        // of its line to the other.
        private readonly float wallHalfSize = 500f;
        private const int wallTravelFrames = 100;

        // XCross()'s own cooldown/travel time — separate from the wall's so
        // the two patterns' density can be tuned independently, same
        // reasoning as bossBurstCooldown/wallCooldown above.
        private int xCooldownRemaining = 0;
        private const int xTravelFrames = 100;
        private readonly int xCooldown = 10;

        // A projectile that sweeps from one point to another, tracked
        // relative to the boss so its Position can be re-derived fresh from
        // the boss's current Position every frame instead of drifting under
        // its own Velocity — see UpdateSweepingShots(). RelativeStart/
        // RelativeEnd are offsets from the boss's center (e.g.
        // (-500, -500)), not world positions. Shared by SquareWall() and
        // XCross(), which each keep their own tracking list so the two
        // patterns' state never crosses.
        private class SweepingShot
        {
            public EnemyProjectile Projectile;
            public Vector2 RelativeStart;
            public Vector2 RelativeEnd;
            public int Elapsed;
            public int TravelFrames;
        }

        private readonly List<SweepingShot> activeWallShots = new List<SweepingShot>();
        private readonly List<SweepingShot> activeXShots = new List<SweepingShot>();

        public LimonTheSpriteGoddess(Vector2 position)
            : base(Art.Limon, position)
        {
            Name = "Limon the Sprite Goddess";

            health = 12000;
            healthMax = 12000;
            Defense = 16;
            PointValue = 2000;
            deathSound = Sound.SpriteGodDeath;
            hitSound = Sound.SpriteGodHit;
            // drawScale = 1.75f;
            // Radius = Art.Limon.Width / 2f * 1.75f;

            AddBehaviour(MoveSnake(0.2f));
            AddAttackBehaviour(
                Spray(
                    projectileSpeed: 6,
                    projectileAmount: 8,
                    damage: 50
                //projectileImage: Art.LimonProjectile,
                //collisionShape: Entity.CollisionShape.Rectangle
                )
            );
            AddAttackBehaviour(SquareWall());
            AddAttackBehaviour(XCross());
            AddBehaviour(PhaseWatcher());
        }

        // Constant square-shaped wall centered on the boss's current
        // Position — re-traced every wallCooldown frames so new sweeps keep
        // launching as the boss moves. Each of the square's 4 lines gets 2
        // projectiles, one starting at each end, each sweeping to the
        // opposite end over wallTravelFrames — so the pair crosses paths
        // partway across the line rather than sitting still. Every active
        // wall projectile's Position is re-derived from the boss's current
        // Position each frame (UpdateSweepingShots()) rather than moved via
        // its own Velocity, so it stays the same distance from the boss
        // even if the boss moves mid-sweep — the whole square translates
        // with the boss instead of drifting behind it. The player has to
        // stay inside the square to avoid it, since it's active for the
        // entire fight rather than gated to one phase.
        private IEnumerable<int> SquareWall(int damage = 25)
        {
            while (true)
            {
                if (wallCooldownRemaining <= 0)
                {
                    wallCooldownRemaining = wallCooldown;
                    SpawnSquareWall(damage);
                }

                if (wallCooldownRemaining > 0)
                    wallCooldownRemaining--;

                UpdateSweepingShots(activeWallShots);

                yield return 0;
            }
        }

        private void SpawnSquareWall(int damage)
        {
            // Corner offsets relative to the boss's center — not world
            // positions, since the wall tracks the boss rather than staying
            // fixed in world space.
            Vector2[] corners =
            {
                new Vector2(-wallHalfSize, -wallHalfSize),
                new Vector2(wallHalfSize, -wallHalfSize),
                new Vector2(wallHalfSize, wallHalfSize),
                new Vector2(-wallHalfSize, wallHalfSize),
            };

            for (int side = 0; side < corners.Length; side++)
            {
                Vector2 relStart = corners[side];
                Vector2 relEnd = corners[(side + 1) % corners.Length];

                SpawnSweepingShot(relStart, relEnd, damage, wallTravelFrames, activeWallShots);
                SpawnSweepingShot(relEnd, relStart, damage, wallTravelFrames, activeWallShots);
            }
        }

        // Same shape and box as SquareWall (reuses the same corners), but
        // its two diagonals — top-left<->bottom-right and
        // top-right<->bottom-left — cross through the boss's own center
        // instead of staying on the perimeter. Combined with SquareWall,
        // the player has to dodge something sweeping through the middle of
        // the square too, not just its edges.
        private IEnumerable<int> XCross(int damage = 25)
        {
            while (true)
            {
                if (xCooldownRemaining <= 0)
                {
                    xCooldownRemaining = xCooldown;
                    SpawnXCross(damage);
                }

                if (xCooldownRemaining > 0)
                    xCooldownRemaining--;

                UpdateSweepingShots(activeXShots);

                yield return 0;
            }
        }

        private void SpawnXCross(int damage)
        {
            Vector2[] corners =
            {
                new Vector2(-wallHalfSize, -wallHalfSize), // top-left
                new Vector2(wallHalfSize, -wallHalfSize), // top-right
                new Vector2(wallHalfSize, wallHalfSize), // bottom-right
                new Vector2(-wallHalfSize, wallHalfSize), // bottom-left
            };

            SpawnSweepingShot(corners[0], corners[2], damage, xTravelFrames, activeXShots);
            SpawnSweepingShot(corners[2], corners[0], damage, xTravelFrames, activeXShots);
            SpawnSweepingShot(corners[1], corners[3], damage, xTravelFrames, activeXShots);
            SpawnSweepingShot(corners[3], corners[1], damage, xTravelFrames, activeXShots);
        }

        private void SpawnSweepingShot(
            Vector2 relativeStart,
            Vector2 relativeEnd,
            int damage,
            int travelFrames,
            List<SweepingShot> targetList
        )
        {
            var projectile = new EnemyProjectile(Position + relativeStart, Vector2.Zero)
            {
                duration = travelFrames,
                Damage = damage,
                Orientation = (relativeEnd - relativeStart).ToAngle(),
            };
            EntityManager.Add(projectile);
            targetList.Add(
                new SweepingShot
                {
                    Projectile = projectile,
                    RelativeStart = relativeStart,
                    RelativeEnd = relativeEnd,
                    Elapsed = 0,
                    TravelFrames = travelFrames,
                }
            );
        }

        // Re-derives every tracked sweeping projectile's Position from the
        // boss's current Position each frame, instead of letting it drift
        // under its own (zero) Velocity — this is what keeps each one the
        // same distance from the boss even if the boss moves mid-sweep.
        private void UpdateSweepingShots(List<SweepingShot> shots)
        {
            for (int i = 0; i < shots.Count; i++)
            {
                SweepingShot shot = shots[i];
                if (shot.Projectile.IsExpired)
                {
                    shots.RemoveAt(i--);
                    continue;
                }

                shot.Elapsed++;
                float t = System.Math.Min(1f, (float)shot.Elapsed / shot.TravelFrames);
                shot.Projectile.Position =
                    Position + Vector2.Lerp(shot.RelativeStart, shot.RelativeEnd, t);
            }
        }

        // Phase-2 attack: a full-circle burst, evenly spaced (unlike the
        // base Enemy.Bomb(), which steps by 10 *radians* per projectile —
        // not evenly spaced; a pre-existing quirk elsewhere, not repeated
        // here).
        private IEnumerable<int> BossBurst(
            int projectileCount = 24,
            int projectileSpeed = 6,
            int damage = 70
        )
        {
            while (true)
            {
                if (bossBurstCooldownRemaining <= 0)
                {
                    bossBurstCooldownRemaining = bossBurstCooldown - (1 * 1);

                    for (int i = 0; i < projectileCount; i++)
                    {
                        Vector2 vel = Extensions.FromPolar(
                            i * (MathHelper.TwoPi / projectileCount),
                            projectileSpeed
                        );
                        EntityManager.Add(
                            new EnemyProjectile(Position, vel)
                            {
                                image = Art.LimonProjectile,
                                duration = 90,
                                Damage = damage,
                            }
                        );
                    }
                }

                if (bossBurstCooldownRemaining > 0)
                    bossBurstCooldownRemaining--;

                yield return 0;
            }
        }

        // Polls health each frame; once it crosses the halfway mark,
        // escalates the fight by adding a second attack (BossBurst, on top
        // of whatever Phase 1 attack is already running) and a second
        // movement behaviour (stacks additively with the Phase 1 one
        // already running, rather than replacing it, for a simple "faster"
        // effect). One-shot per fight via the local `enraged` flag.
        private IEnumerable<int> PhaseWatcher()
        {
            bool enraged = false;
            while (true)
            {
                if (!enraged && HealthFraction <= 0.5f)
                {
                    enraged = true;
                    AddAttackBehaviour(BossBurst());
                    AddBehaviour(FollowPlayer(0.15f));
                }
                yield return 0;
            }
        }
    }
}
