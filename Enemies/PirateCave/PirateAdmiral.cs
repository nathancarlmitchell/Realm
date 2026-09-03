using Microsoft.Xna.Framework;

namespace Realm
{
    // Pirate Cave — realmeye.com/wiki/pirate-admiral. "Always shoot single,
    // fast cannonballs and sometimes shoot two slower cannonballs.
    // Occasionally... Armored" — same two-independent-attacks shape as
    // PirateCaptain, a step more aggressive on every stat.
    class PirateAdmiral : Enemy
    {
        public PirateAdmiral(Vector2 position)
            : base(Art.PirateAdmiral, position)
        {
            health = 120;
            healthMax = 120;
            Defense = 5;
            PointValue = 12;
            DropPool = PirateCaveDropPool;
            DropChances = PirateCaveDropChances;
            DropTierRanges = PirateCaveDropTierRanges;

            AddAttackBehaviour(
                ShootIfInRange(
                    range: 11.25f * 32f,
                    damage: 15,
                    projectileSpeed: 5f * 32f / 60f,
                    projectileImage: Art.PirateCannonBullet,
                    cooldownFrames: 90
                )
            );
            AddAttackBehaviour(
                ShootIfInRange(
                    range: 10.4f * 32f,
                    damage: 20,
                    projectileSpeed: 2f * 32f / 60f,
                    projectileImage: Art.PirateShot,
                    cooldownFrames: 140
                )
            );
            AddBehaviour(PeriodicArmor(intervalFrames: 260, durationFrames: 120));
        }
    }
}
