using Microsoft.Xna.Framework;

namespace Realm
{
    // Pirate Cave — realmeye.com/wiki/cave-pirate-parrot. Same harmless
    // "critter" shape as CavePirateCabinBoy — see its own doc comment.
    class CavePirateParrot : Enemy
    {
        public CavePirateParrot(Vector2 position)
            : base(Art.CavePirateParrot, position)
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
