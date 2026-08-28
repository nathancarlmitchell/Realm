using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm.Bosses;

namespace Realm
{
    // Escort spawned and replenished by SandsmanKing.MaintainArchers() —
    // deliberately not part of EnemySpawner.BasicEnemyPool (same reasoning
    // as LittleScorpion), since "orbits around Sandman King" only makes
    // sense with a live King to orbit.
    class SandsmanArcher : Enemy
    {
        public SandsmanKing Owner { get; }

        private float orbitAngle;

        // Not specified in the spec — a scale that reads as "circling a
        // mini-boss" without the orbit taking so long it looks static.
        // Tunable.
        private const float OrbitRadius = 140f;
        private const float OrbitSpeed = 0.04f;

        // Range(11.9) > Trigger Range(10) here, unlike the King's own
        // Attacks (Range 8.4 < Trigger Range 10) — Sandsman Archer never
        // has a separate "notice, then react" state the way the King's
        // wander-vs-chase does (it's always orbiting, always alert), so
        // Trigger Range has no distinct mechanical role for it here; Range
        // alone drives ShootIfInRange below. Flagged as an interpretation
        // call, not silently dropped.
        private const float AttackRange = 11.9f * 32f;
        private const int AttackDamage = 8;
        private const float ProjectileSpeed = 8.5f * 32f / 60f; // 8.5 tiles/sec
        private const int AttackCooldown = 60; // 1s at 60fps

        public SandsmanArcher(SandsmanKing owner, Vector2 position)
            : base(Art.SandsmanArcher, position)
        {
            Owner = owner;
            orbitAngle = (position - owner.Position).ToAngle();

            health = 30;
            healthMax = 30;
            Defense = 0;
            PointValue = 6;
            DropPool = BeachDropPool;
            DropChances = BeachDropChances;
            DropTierRanges = BeachDropTierRanges;

            AddBehaviour(Orbit());
            AddAttackBehaviour(
                ShootIfInRange(
                    range: AttackRange,
                    damage: AttackDamage,
                    projectileSpeed: ProjectileSpeed,
                    projectileImage: Art.GreenArrow,
                    cooldownFrames: AttackCooldown,
                    collisionShape: CollisionShape.Rectangle
                )
            );
        }

        // Same "re-derive Position from the owner's current Position every
        // frame" technique as SthenoPet.Orbit() (Bosses/SthenoPet.cs) —
        // written bespoke here rather than promoted to a shared Enemy.cs
        // helper, since Stheno's own version isn't being touched this
        // session and one extra similarly-shaped coroutine in this file
        // isn't worth the indirection yet.
        private IEnumerable<int> Orbit()
        {
            while (true)
            {
                orbitAngle = MathHelper.WrapAngle(orbitAngle + OrbitSpeed);
                Position = Owner.Position + Extensions.FromPolar(orbitAngle, OrbitRadius);
                yield return 0;
            }
        }
    }
}
