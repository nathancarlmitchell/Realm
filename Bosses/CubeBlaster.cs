using Microsoft.Xna.Framework;
using Realm;

namespace Realm.Bosses
{
    // The other of Cube Overseer's two escort types (see
    // CubeOverseer.MaintainMinions()) — stands off at range rather than
    // closing in, matching the wiki's own "Blaster" framing. Same
    // continuously-replenished PointValue/DropsLoot convention as
    // CubeDefender.
    class CubeBlaster : Enemy
    {
        public CubeOverseer Owner { get; }

        private const float WanderDistance = 60f;
        private const float WanderSpeed = 0.05f;

        private const float AttackRange = 10f * 32f; // 10 tiles
        private const int AttackDamage = 22;
        private const float ProjectileSpeed = 4f * 32f / 60f; // 4 tiles/sec
        private const int AttackCooldownFrames = 70;

        public CubeBlaster(CubeOverseer owner, Vector2 position)
            : base(Art.CubeBlaster, position)
        {
            Owner = owner;

            health = 100;
            healthMax = 100;
            Defense = 1;
            PointValue = 0;
            DropsLoot = false;

            AddBehaviour(MoveTethered(wanderDistance: WanderDistance, speed: WanderSpeed));
            AddAttackBehaviour(
                ShootIfInRange(
                    range: AttackRange,
                    damage: AttackDamage,
                    projectileSpeed: ProjectileSpeed,
                    cooldownFrames: AttackCooldownFrames
                )
            );
        }
    }
}
