using Microsoft.Xna.Framework;

namespace Realm
{
    // Pirate Cave — realmeye.com/wiki/pirate-captain. "Shoot one fast
    // cannonball, 2 slower cannonballs, and occasionally be Armored" — the
    // two shot types run as independent ShootIfInRange behaviours on their
    // own cooldowns rather than one combined volley, which already reads
    // as "sometimes both, mostly the fast one" given the slower shot's
    // longer cooldown.
    class PirateCaptain : Enemy
    {
        public PirateCaptain(Vector2 position)
            : base(Art.PirateCaptain, position)
        {
            health = 100;
            healthMax = 100;
            Defense = 4;
            PointValue = 10;
            DropPool = PirateCaveDropPool;
            DropChances = PirateCaveDropChances;
            DropTierRanges = PirateCaveDropTierRanges;

            AddAttackBehaviour(
                ShootIfInRange(
                    range: 11.25f * 32f,
                    damage: 14,
                    projectileSpeed: 5f * 32f / 60f,
                    projectileImage: Art.PirateCannonBullet,
                    cooldownFrames: 100
                )
            );
            AddAttackBehaviour(
                ShootIfInRange(
                    range: 10.4f * 32f,
                    damage: 18,
                    projectileSpeed: 2f * 32f / 60f,
                    projectileImage: Art.PirateShot,
                    cooldownFrames: 160
                )
            );
            AddBehaviour(PeriodicArmor(intervalFrames: 300, durationFrames: 120));
        }
    }
}
