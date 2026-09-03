using Microsoft.Xna.Framework;

namespace Realm
{
    // Pirate Cave — realmeye.com/wiki/cave-pirate-monkey. Same harmless
    // "critter" shape as CavePirateCabinBoy — see its own doc comment.
    class CavePirateMonkey : Enemy
    {
        public CavePirateMonkey(Vector2 position)
            : base(Art.CavePirateMonkey, position)
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
