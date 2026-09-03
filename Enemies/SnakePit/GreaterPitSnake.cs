using Microsoft.Xna.Framework;

namespace Realm
{
    // Snake Pit — realmeye.com/wiki/greater-pit-snake. "Wanders in place,
    // snaps back if it strays too far from the center of the room" —
    // MoveTethered() already defaults to tethering at its own spawn point,
    // so no center-point plumbing is needed here at all. "Shoots single
    // shots in a random cardinal or ordinal direction" (ShootRandomDirection),
    // "throwing bombs at the nearest player" and "continually detonating a
    // red AoE explosion on itself" both map onto the ThrowGrenades
    // primitive, just with a different targetPosition delegate.
    class GreaterPitSnake : Enemy
    {
        public GreaterPitSnake(Vector2 position)
            : base(Art.GreaterPitSnake, position)
        {
            health = 500;
            healthMax = 500;
            Defense = 10;
            PointValue = 250;
            DropPool = SnakePitDropPool;
            DropChances = SnakePitDropChances;
            DropTierRanges = SnakePitDropTierRanges;

            AddBehaviour(MoveTethered());
            AddAttackBehaviour(
                ShootRandomDirection(
                    damage: 45,
                    projectileSpeed: 8f * 32f / 60f,
                    cooldownFrames: 70,
                    projectileImage: Art.SnakeBite
                )
            );
            AddAttackBehaviour(
                ThrowGrenades(
                    damage: 65,
                    radius: 2f * 32f,
                    cooldownFrames: 110,
                    targetPosition: () => Player.Instance.Position
                )
            );
            AddAttackBehaviour(
                ThrowGrenades(
                    damage: 65,
                    radius: 2f * 32f,
                    cooldownFrames: 130,
                    targetPosition: () => Position
                )
            );
        }
    }
}
