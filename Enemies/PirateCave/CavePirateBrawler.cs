using Microsoft.Xna.Framework;

namespace Realm
{
    // Pirate Cave — realmeye.com/wiki/cave-pirate-brawler. Melee "sword"
    // chaser — FollowPlayer + a short-range sword swing (an EnemyProjectile
    // via ShootIfInRange, same as every other "melee" enemy this engine
    // already has).
    class CavePirateBrawler : Enemy
    {
        public CavePirateBrawler(Vector2 position)
            : base(Art.CavePirateBrawler, position)
        {
            health = 20;
            healthMax = 20;
            PointValue = 2;
            DropPool = PirateCaveDropPool;
            DropChances = PirateCaveDropChances;
            DropTierRanges = PirateCaveDropTierRanges;

            AddBehaviour(FollowPlayer(0.4f));
            AddAttackBehaviour(
                ShootIfInRange(
                    range: 3.9f * 32f,
                    damage: 4,
                    projectileSpeed: 6.5f * 32f / 60f,
                    projectileImage: Art.PirateSword
                )
            );
        }
    }
}
