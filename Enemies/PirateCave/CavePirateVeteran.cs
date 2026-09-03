using Microsoft.Xna.Framework;

namespace Realm
{
    // Pirate Cave — realmeye.com/wiki/cave-pirate-veteran. "Chases the
    // nearest player, circling them when close enough" — FollowPlayer's
    // constant pull toward the player and OrbitPlayer's pull toward the
    // circle just add together, so it closes in, then settles into
    // circling once near the target radius, with no separate
    // distance-gated state machine needed (see Enemy.OrbitPoint's own doc
    // comment).
    class CavePirateVeteran : Enemy
    {
        public CavePirateVeteran(Vector2 position)
            : base(Art.CavePirateVeteran, position)
        {
            health = 35;
            healthMax = 35;
            Defense = 2;
            PointValue = 4;
            DropPool = PirateCaveDropPool;
            DropChances = PirateCaveDropChances;
            DropTierRanges = PirateCaveDropTierRanges;

            AddBehaviour(FollowPlayer(0.2f));
            AddBehaviour(OrbitPlayer(radius: 5.2f * 32f));
            AddAttackBehaviour(
                ShootIfInRange(
                    range: 5.2f * 32f,
                    damage: 8,
                    projectileSpeed: 6.5f * 32f / 60f,
                    projectileImage: Art.PirateSword
                )
            );
        }
    }
}
