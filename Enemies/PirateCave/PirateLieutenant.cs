using Microsoft.Xna.Framework;

namespace Realm
{
    // Pirate Cave — realmeye.com/wiki/pirate-lieutenant. Ranged "cannon"
    // sentry — no movement stat on the wiki at all, and the wiki never
    // says it chases (only "fires at the nearest player," "always have
    // other pirates with them... protecting Dreadstump") — stationary, no
    // AddBehaviour movement at all.
    class PirateLieutenant : Enemy
    {
        public PirateLieutenant(Vector2 position)
            : base(Art.PirateLieutenant, position)
        {
            health = 70;
            healthMax = 70;
            Defense = 2;
            PointValue = 7;
            DropPool = PirateCaveDropPool;
            DropChances = PirateCaveDropChances;
            DropTierRanges = PirateCaveDropTierRanges;

            AddAttackBehaviour(
                ShootIfInRange(
                    range: 10f * 32f,
                    damage: 10,
                    projectileSpeed: 5f * 32f / 60f,
                    projectileImage: Art.PirateCannonBullet,
                    cooldownFrames: 150
                )
            );
        }
    }
}
