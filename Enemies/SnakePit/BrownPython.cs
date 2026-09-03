using Microsoft.Xna.Framework;

namespace Realm
{
    // Snake Pit — realmeye.com/wiki/brown-python. "Circles approximately 2
    // tiles from the nearest player, continually firing... Occasionally
    // charges towards the nearest player" — OrbitPlayer's constant pull
    // toward the orbit target and PeriodicCharge's occasional velocity
    // burst just add together, same "two behaviours, no bespoke state
    // machine" shape Cave Pirate Veteran's own FollowPlayer+OrbitPlayer
    // combo already established.
    class BrownPython : Enemy
    {
        public BrownPython(Vector2 position)
            : base(Art.BrownPython, position)
        {
            health = 200;
            healthMax = 200;
            Defense = 20;
            PointValue = 70;
            DropPool = SnakePitDropPool;
            DropChances = SnakePitDropChances;
            DropTierRanges = SnakePitDropTierRanges;

            AddBehaviour(OrbitPlayer(radius: 2f * 32f));
            AddBehaviour(PeriodicCharge(intervalFrames: 200, chargeDurationFrames: 25, chargeSpeed: 6f));
            AddAttackBehaviour(
                ShootIfInRange(
                    range: 6.3f * 32f,
                    damage: 30,
                    projectileSpeed: 7f * 32f / 60f,
                    projectileImage: Art.SnakeBite
                )
            );
        }
    }
}
