using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Realm
{
    // Beach's second basic wave enemy — Bandit Leader's escort (see
    // Bosses/BanditLeader.cs and EnemySpawner.SpawnBanditLeaderPack()),
    // same relationship Pirate has with BeachedBuccaneer. Kept as its own
    // small dedicated class rather than a bare Enemy.CreatePirate()-style
    // factory because its attack — a longer-range shot that gets replaced
    // by a shorter-range, +1-damage "dagger stab" once the player closes
    // in, sharing one cooldown rather than firing independently — doesn't
    // fit ShootIfInRange's single-range shape, and isn't generic/reusable
    // enough yet to add as a new shared Enemy.cs coroutine the way
    // ShootIfInRange itself was.
    class Bandit : Enemy
    {
        // Not Enemy's own private projectileCooldown(Remaining)/rand —
        // those fields aren't visible to a subclass (private, not
        // protected), same reason BeachedBuccaneer.cs needed its own rand.
        private static readonly Random rand = new();
        private int attackCooldownRemaining = 0;
        private const int AttackCooldown = 250; // matches Enemy's own default projectileCooldown

        // "A shorter range dagger stab that deals one extra damage" — used
        // instead of (not alongside) the main shot once the player is this
        // close; DaggerRange < MainRange so a close player always
        // qualifies for the dagger check first.
        private const float DaggerRange = 3.6f * 32f;
        private const int DaggerDamage = 9;
        private const float MainRange = 6f * 32f;
        private const int MainDamage = 8;

        // 6 tiles/sec * 32px/tile / 60 ticks/sec — shared by both the main
        // shot and the dagger stab; the spec gives them the same Speed.
        private const float ProjectileSpeed = 6f * 32f / 60f;

        public Bandit(Vector2 position)
            : base(Art.Bandit, position)
        {
            health = 50;
            healthMax = 50;
            Defense = 1;
            PointValue = 5;
            DropPool = BeachDropPool;
            DropChances = BeachDropChances;
            DropTierRanges = BeachDropTierRanges;

            AddBehaviour(FollowPlayer(0.2f));
            AddAttackBehaviour(BanditAttack());
        }

        // "They only use [the dagger] when you get close" — a single
        // shared cooldown/attack action, not two independently-firing
        // attacks: within DaggerRange, fire the dagger (higher damage,
        // shorter range); otherwise, if still within MainRange, fire the
        // regular shot. Neither fires past MainRange.
        private IEnumerable<int> BanditAttack()
        {
            while (true)
            {
                var aim = Player.Instance.Position - Position;
                if (aim.LengthSquared() > 0 && attackCooldownRemaining <= 0)
                {
                    float distSq = aim.LengthSquared();
                    int damage =
                        distSq <= DaggerRange * DaggerRange ? DaggerDamage
                        : distSq <= MainRange * MainRange ? MainDamage
                        : 0;

                    if (damage > 0)
                    {
                        attackCooldownRemaining = AttackCooldown;
                        float aimAngle = aim.ToAngle();
                        float randomSpread =
                            rand.NextFloat(-0.1f, 0.1f) + rand.NextFloat(-0.1f, 0.1f);
                        Vector2 vel = Extensions.FromPolar(
                            aimAngle + randomSpread,
                            ProjectileSpeed
                        );
                        EntityManager.Add(
                            new EnemyProjectile(Position, vel, Art.SwordSlash)
                            {
                                Damage = damage,
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
