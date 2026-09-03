using Microsoft.Xna.Framework;

namespace Realm
{
    // Pirate Cave — realmeye.com/wiki/cave-pirate-sailor. Same melee
    // "sword" chaser shape as CavePirateBrawler, a step tougher.
    class CavePirateSailor : Enemy
    {
        public CavePirateSailor(Vector2 position)
            : base(Art.CavePirateSailor, position)
        {
            health = 30;
            healthMax = 30;
            PointValue = 3;
            DropPool = PirateCaveDropPool;
            DropChances = PirateCaveDropChances;
            DropTierRanges = PirateCaveDropTierRanges;

            AddBehaviour(FollowPlayer(0.4f));
            AddAttackBehaviour(
                ShootIfInRange(
                    range: 3.9f * 32f,
                    damage: 7,
                    projectileSpeed: 6.5f * 32f / 60f,
                    projectileImage: Art.PirateSword
                )
            );
        }
    }
}
