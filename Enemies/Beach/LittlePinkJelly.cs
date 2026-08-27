using Microsoft.Xna.Framework;

namespace Realm
{
    // Same "own Gaussian group, not in BasicEnemyPool" shape as
    // LittleBlueJelly.cs — see EnemySpawner.SpawnGroupPack(). No Shots/Angle
    // given in the spec (single shot, no fan pattern), so this reuses
    // ShootIfInRange() directly rather than FanShot().
    class LittlePinkJelly : Enemy
    {
        private const float Range = 13f * 32f;
        private const int Damage = 12;
        private const float ProjectileSpeed = 6.5f * 32f / 60f; // 6.5 tiles/sec
        private const int AttackCooldown = 36; // 0.6s at 60fps

        // Same "Wander Speed: 4 doesn't map literally" reasoning as
        // LittleGreenJelly.cs.
        private const float WanderDistance = 120f;
        private const float WanderSpeed = 0.1f;

        public LittlePinkJelly(Vector2 position)
            : base(Art.LittlePinkJelly, position)
        {
            health = 70;
            healthMax = 70;
            Defense = 0;
            PointValue = 8;
            DropPool = BeachDropPool;
            DropChances = BeachDropChances;
            DropTierRanges = BeachDropTierRanges;

            AddBehaviour(MoveTethered(wanderDistance: WanderDistance, speed: WanderSpeed));
            AddAttackBehaviour(
                ShootIfInRange(
                    range: Range,
                    damage: Damage,
                    projectileSpeed: ProjectileSpeed,
                    projectileImage: Art.PurpleMagic,
                    cooldownFrames: AttackCooldown
                )
            );
        }
    }
}
