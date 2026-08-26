using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm.Bosses;

namespace Realm
{
    // Escort spawned and replenished by SandsmanKing.MaintainSorcerers() —
    // deliberately not part of EnemySpawner.BasicEnemyPool (same reasoning
    // as LittleScorpion/SandsmanArcher), kept consistent with both even
    // though this one's own behavior never actually references Owner (its
    // wander is untethered) — Owner exists purely so the King can scope its
    // Max count to its own escorts, same as every other escort this session.
    class SandsmanSorcerer : Enemy
    {
        public SandsmanKing Owner { get; }

        // "Wanders aimlessly" — no anchor given (unlike Little Scorpion's
        // explicit tie to the Queen), so this uses a self-tethered wander
        // around its own spawn point, same as every other untethered
        // wanderer in this engine (there's no truly unbounded wander
        // primitive to reach for instead).
        private const float WanderDistance = 300f;
        private const float WanderSpeed = 0.15f;

        // "Once approached fires a fast and strong short ranged dark blue
        // projectile" reads as the same distance-based
        // closer-range-replaces-farther-range mechanic as Bandit.cs's
        // dagger/ranged split — one shared cooldown, not two independently
        // firing attacks (matches "fires purple... once approached fires
        // dark blue", not "fires purple AND dark blue").
        private const float DarkBlueRange = 2.4f * 32f;
        private const int DarkBlueDamage = 17;
        private const float DarkBlueSpeed = 8f * 32f / 60f; // 8 tiles/sec

        private const float PurpleRange = 8f * 32f;
        private const int PurpleDamage = 13;
        private const float PurpleSpeed = 0.8f * 32f / 60f; // 0.8 tiles/sec

        // No Cooldown given in the spec (unlike Sandsman King/Archer, which
        // both state one explicitly) — falls back to the same 250-tick
        // default Enemy's own shared Shoot()/Spray()/ShootIfInRange()
        // already use elsewhere. Tunable.
        private const int AttackCooldown = 250;

        private int attackCooldownRemaining = 0;

        public SandsmanSorcerer(SandsmanKing owner, Vector2 position)
            : base(Art.SandsmanSorcerer, position)
        {
            Owner = owner;

            health = 88;
            healthMax = 88;
            Defense = 0;
            PointValue = 18;
            DropPool = BeachDropPool;
            DropChances = BeachDropChances;
            DropTierRanges = BeachDropTierRanges;

            AddBehaviour(MoveTethered(wanderDistance: WanderDistance, speed: WanderSpeed));
            AddAttackBehaviour(SorcererAttack());
        }

        private IEnumerable<int> SorcererAttack()
        {
            while (true)
            {
                var aim = Player.Instance.Position - Position;
                if (aim.LengthSquared() > 0 && attackCooldownRemaining <= 0)
                {
                    float distSq = aim.LengthSquared();
                    float aimAngle = aim.ToAngle();

                    if (distSq <= DarkBlueRange * DarkBlueRange)
                    {
                        attackCooldownRemaining = AttackCooldown;
                        Fire(aimAngle, DarkBlueSpeed, DarkBlueDamage, Art.DarkBlueMagic);
                    }
                    else if (distSq <= PurpleRange * PurpleRange)
                    {
                        attackCooldownRemaining = AttackCooldown;
                        Fire(aimAngle, PurpleSpeed, PurpleDamage, Art.PurpleMysticShot);
                    }
                }

                if (attackCooldownRemaining > 0)
                    attackCooldownRemaining--;

                yield return 0;
            }
        }

        private void Fire(float aimAngle, float speed, int damage, Texture2D image)
        {
            Vector2 vel = Extensions.FromPolar(aimAngle, speed);
            EntityManager.Add(new EnemyProjectile(Position, vel, image) { Damage = damage });
        }
    }
}
