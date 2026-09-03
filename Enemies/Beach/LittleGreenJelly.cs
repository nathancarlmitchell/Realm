using Microsoft.Xna.Framework;

namespace Realm
{
    // Same "own Gaussian group, not in BasicEnemyPool" shape as
    // LittleBlueJelly.cs — see EnemySpawner.SpawnGroupPack().
    class LittleGreenJelly : Enemy
    {
        private const float Range = 12f * 32f;
        private const int Damage = 10;
        private const float ProjectileSpeed = 6f * 32f / 60f; // 6 tiles/sec
        private const int Shots = 5;

        // "Angle: 72" (degrees) — 5 shots * 72° = exactly 360°, so
        // Enemy.FanShot()'s symmetric-around-aim spacing produces a full,
        // aim-independent 5-point star ("firing five green bullets in a
        // star shape"), not a narrow fan.
        private static readonly float AngleStep = MathHelper.ToRadians(72f);
        private const int AttackCooldown = 108; // 1.8s at 60fps

        // "Wander Speed: 4" given in the spec doesn't map cleanly onto
        // MoveTethered's own speed scale (an accel-per-tick value normally
        // 0.05-0.2 for every other "lazy"/idle wanderer this session) — a
        // literal tiles/sec-style conversion of 4 would move far too fast
        // for "lazily wanders." Uses the same slow-drift value as those
        // other wanderers instead; flagged as an interpretation call.
        private const float WanderDistance = 120f;
        private const float WanderSpeed = 0.1f;

        public LittleGreenJelly(Vector2 position)
            : base(Art.LittleGreenJelly, position)
        {
            health = 70;
            healthMax = 70;
            Defense = 0;
            PointValue = 8;
            DropPool = BeachDropPool;
            DropChances = BeachDropChances;
            DropTierRanges = BeachDropTierRanges;
            PortalDropChances = BeachPortalDropChances;

            AddBehaviour(MoveTethered(wanderDistance: WanderDistance, speed: WanderSpeed));
            AddAttackBehaviour(
                FanShot(
                    range: Range,
                    damage: Damage,
                    projectileSpeed: ProjectileSpeed,
                    shots: Shots,
                    angleStep: AngleStep,
                    projectileImage: Art.GreenMagic,
                    cooldownFrames: AttackCooldown
                )
            );
        }
    }
}
