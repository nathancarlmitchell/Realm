using Microsoft.Xna.Framework;

namespace Realm
{
    // Pirate Cave — realmeye.com/wiki/cave-pirate-macaw. Same harmless
    // "critter" shape as CavePirateCabinBoy — see its own doc comment.
    class CavePirateMacaw : Enemy
    {
        public CavePirateMacaw(Vector2 position)
            : base(Art.CavePirateMacaw, position)
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
