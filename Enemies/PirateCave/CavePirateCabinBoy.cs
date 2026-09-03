using Microsoft.Xna.Framework;

namespace Realm
{
    // Pirate Cave (see Data/DungeonType_PirateCave.json and docs/DEVLOG.md)
    // — sourced directly from realmeye.com/wiki/cave-pirate-cabin-boy. Real
    // art supplied (Content/Dungeons/Pirate Cave/), no tinted reskin
    // needed. Harmless "critter" — HP5/DEF0/PointValue1, wanders only,
    // never attacks, identical to its siblings on the wiki apart from
    // sprite/drop flavor. Water-avoidance from the wiki text is skipped:
    // MoveRandomly() has no tile-awareness to build it on, and it's purely
    // cosmetic with zero mechanical effect.
    class CavePirateCabinBoy : Enemy
    {
        public CavePirateCabinBoy(Vector2 position)
            : base(Art.CavePirateCabinBoy, position)
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
