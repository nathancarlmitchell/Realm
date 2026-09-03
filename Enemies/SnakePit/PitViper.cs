using Microsoft.Xna.Framework;

namespace Realm
{
    // Snake Pit — realmeye.com/wiki/pit-viper. "A counterpart to the Pit
    // Snake" — same shape, twice the damage split across a 2-shot "V
    // formation" (FanShot with shots: 2 gives exactly a symmetric pair)
    // instead of one shot.
    class PitViper : Enemy
    {
        public PitViper(Vector2 position)
            : base(Art.PitViper, position)
        {
            health = 5;
            healthMax = 5;
            PointValue = 5;
            DropPool = SnakePitDropPool;
            DropChances = SnakePitDropChances;
            DropTierRanges = SnakePitDropTierRanges;

            AddBehaviour(MoveRandomly());
            AddAttackBehaviour(
                FanShot(
                    range: 10f * 32f,
                    damage: 20,
                    projectileSpeed: 5f * 32f / 60f,
                    shots: 2,
                    angleStep: 0.3f,
                    projectileImage: Art.SnakeBite
                )
            );
        }
    }
}
