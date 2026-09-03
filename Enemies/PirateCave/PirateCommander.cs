using Microsoft.Xna.Framework;

namespace Realm
{
    // Pirate Cave — realmeye.com/wiki/pirate-commander. Same stationary
    // "cannon" sentry shape as PirateLieutenant, a step tougher with a
    // faster fire rate.
    class PirateCommander : Enemy
    {
        public PirateCommander(Vector2 position)
            : base(Art.PirateCommander, position)
        {
            health = 80;
            healthMax = 80;
            Defense = 3;
            PointValue = 8;
            DropPool = PirateCaveDropPool;
            DropChances = PirateCaveDropChances;
            DropTierRanges = PirateCaveDropTierRanges;

            AddAttackBehaviour(
                ShootIfInRange(
                    range: 10f * 32f,
                    damage: 12,
                    projectileSpeed: 5f * 32f / 60f,
                    projectileImage: Art.PirateCannonBullet,
                    cooldownFrames: 110
                )
            );
        }
    }
}
