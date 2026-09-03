using Microsoft.Xna.Framework;

namespace Realm
{
    // Snake Pit (see Data/DungeonType_SnakePit.json, Dungeon/
    // TreasureRoomController.cs, and docs/DEVLOG.md) — sourced directly
    // from realmeye.com/wiki/pit-snake. Real art supplied
    // (Content/Dungeons/Snake Pit/).
    class PitSnake : Enemy
    {
        public PitSnake(Vector2 position)
            : base(Art.PitSnake, position)
        {
            health = 5;
            healthMax = 5;
            PointValue = 5;
            DropPool = SnakePitDropPool;
            DropChances = SnakePitDropChances;
            DropTierRanges = SnakePitDropTierRanges;

            AddBehaviour(MoveRandomly());
            AddAttackBehaviour(
                ShootIfInRange(
                    range: 12f * 32f,
                    damage: 10,
                    projectileSpeed: 6f * 32f / 60f,
                    projectileImage: Art.SnakeBite
                )
            );
        }
    }
}
