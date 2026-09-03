using Microsoft.Xna.Framework;

namespace Realm
{
    // Pirate Cave — realmeye.com/wiki/cave-pirate-hunchback. Same harmless
    // "critter" shape as CavePirateCabinBoy — see its own doc comment.
    class CavePirateHunchback : Enemy
    {
        public CavePirateHunchback(Vector2 position)
            : base(Art.CavePirateHunchback, position)
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
