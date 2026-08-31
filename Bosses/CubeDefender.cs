using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm;
using Realm.Projectiles;

namespace Realm.Bosses
{
    // One of Cube Overseer's two escort types (see
    // CubeOverseer.MaintainMinions()) — https://www.realmeye.com/wiki/cube-defender.
    // Real stats/attack this time, replacing entry 257's first-pass guess
    // (a plain straight `ShootIfInRange` shot with `FollowPlayer` chase
    // movement — this page didn't exist to check yet). The wiki's own
    // "Behavior" section is blank ("TBA"), but its one combat row is
    // explicit: a single wavy shot. Movement changed to tether to its own
    // Overseer instead of chasing the player — matches the boss page's own
    // tips ("when their Overseer dies, the protecting Cubes will search
    // for a new Overseer... or stand almost still"), which only makes
    // sense if they're normally clustered around one, not off chasing
    // across the map. Continuously replenished like SthenoPet/SthenoSwarm,
    // so PointValue/DropsLoot follow that same "don't let it be farmed"
    // convention rather than LittleScorpion's (which does drop normal
    // loot) — the wiki's own EXP value is real-game flavor, not something
    // this engine's anti-farming convention should import.
    class CubeDefender : Enemy
    {
        public CubeOverseer Owner { get; }

        // No movement description exists ("Behavior: TBA") — tethering to
        // the Overseer that spawned it is a first-pass reading of the boss
        // page's own "protecting Cubes" framing, not a documented number.
        private const float WanderDistance = 100f;
        private const float WanderSpeed = 0.1f;

        // Speed/range converted from the wiki's own tiles/sec and tiles
        // values (32px/tile, 60 ticks/sec) — real numbers, not guesses. No
        // cadence is given for the shot itself — AttackCooldown is a
        // first-pass tunable.
        private int attackCooldownRemaining = 0;
        private const int AttackCooldown = 60;
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

            AddBehaviour(
                MoveTethered(wanderDistance: WanderDistance, speed: WanderSpeed, anchor: owner)
            );
            AddAttackBehaviour(WavyAttack());
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
