using Microsoft.Xna.Framework;

namespace Realm
{
    // Snake Pit — realmeye.com/wiki/yellow-python. "The only python that
    // does not directly move towards the player" — MoveRandomly() (wanders)
    // rather than any Follow/Orbit behaviour, plus a 3-shot "swipe" (FanShot)
    // aimed left/center/right of the player.
    class YellowPython : Enemy
    {
        public YellowPython(Vector2 position)
            : base(Art.YellowPython, position)
        {
            health = 200;
            healthMax = 200;
            Defense = 5;
            PointValue = 70;
            DropPool = SnakePitDropPool;
            DropChances = SnakePitDropChances;
            DropTierRanges = SnakePitDropTierRanges;

            AddBehaviour(MoveRandomly());
            AddAttackBehaviour(
                FanShot(
                    range: 12.75f * 32f,
                    damage: 35,
                    projectileSpeed: 8.5f * 32f / 60f,
                    shots: 3,
                    angleStep: 0.25f,
                    projectileImage: Art.SnakeBite
                )
            );
        }
    }
}
