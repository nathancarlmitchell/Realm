using Microsoft.Xna.Framework;

namespace Realm
{
    // Beach's own basic wave enemy — spawns in its own Gaussian-sized group
    // (see EnemySpawner.SpawnGroupPack()/SampleGroupSize()), not part of
    // BasicEnemyPool's regular mixed wave and not tied to any mini-boss
    // ("Does not protect" in the spec).
    class LittleBlueJelly : Enemy
    {
        private const float Range = 12f * 32f;
        private const int Damage = 9;
        private const float ProjectileSpeed = 6f * 32f / 60f; // 6 tiles/sec
        private const int Shots = 2;

        // "Angle: 10" (degrees) — the two shots split symmetrically around
        // the aim line, ±5° each, tracing a narrow "V" as they travel
        // outward. See Enemy.FanShot()'s own comment for how this same
        // formula also produces Little Green Jelly's full star.
        private static readonly float AngleStep = MathHelper.ToRadians(10f);
        private const int AttackCooldown = 60; // 1s at 60fps

        // "Lazily wanders around a small area" — no Wander Speed was given
        // for this jelly specifically (unlike Green/Pink's stated "Wander
        // Speed: 4"), so this uses the same slow-drift value this session's
        // other "lazy"/idle wanderers already settled on (BeachedBuccaneer,
        // SandsmanKing's pre-aggro wander) rather than inventing a new one.
        private const float WanderDistance = 120f;
        private const float WanderSpeed = 0.1f;

        public LittleBlueJelly(Vector2 position)
            : base(Art.LittleBlueJelly, position)
        {
            health = 70;
            healthMax = 70;
            Defense = 0;
            PointValue = 8;

            AddBehaviour(MoveTethered(wanderDistance: WanderDistance, speed: WanderSpeed));
            AddAttackBehaviour(
                FanShot(
                    range: Range,
                    damage: Damage,
                    projectileSpeed: ProjectileSpeed,
                    shots: Shots,
                    angleStep: AngleStep,
                    projectileImage: Art.BlueMissile,
                    cooldownFrames: AttackCooldown
                )
            );
        }
    }
}
