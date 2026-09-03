using Microsoft.Xna.Framework;

namespace Realm
{
    // Pirate Cave — realmeye.com/wiki/cave-pirate-moll. Same harmless
    // "critter" shape as CavePirateCabinBoy — see its own doc comment.
    class CavePirateMoll : Enemy
    {
        public CavePirateMoll(Vector2 position)
            : base(Art.CavePirateMoll, position)
        {
            health = 5;
            healthMax = 5;
            PointValue = 1;
            DropPool = PirateCaveDropPool;
            DropChances = PirateCaveDropChances;
            DropTierRanges = PirateCaveDropTierRanges;

            AddBehaviour(MoveRandomly());
        }
    }
}
