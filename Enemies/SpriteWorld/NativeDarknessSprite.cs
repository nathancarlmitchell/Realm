using Microsoft.Xna.Framework;

namespace Realm
{
    // Sprite World (see Data/DungeonType_SpriteWorld.json) — sourced
    // directly from realmeye.com/wiki/native-darkness-sprite. Real art
    // supplied (Content/Dungeons/Sprite World/).
    class NativeDarknessSprite : Enemy
    {
        public NativeDarknessSprite(Vector2 position)
            : base(Art.NativeDarknessSprite, position)
        {
            health = 100;
            healthMax = 100;
            PointValue = 40;
            DropPool = SpriteWorldDropPool;
            DropChances = SpriteWorldDropChances;
            DropTierRanges = SpriteWorldDropTierRanges;
            CountsTowardNativeSpriteKills = true;

            AddBehaviour(MoveRandomly());

            // "Erratically moves around while firing slow shotguns of
            // decelerating dark bolts" — the shotgun itself is a single
            // aimed bolt per the wiki's own attack table (one row, no
            // shot-count column), so a plain aimed shot rather than FanShot.
            AddAttackBehaviour(
                ShootIfInRange(
                    range: 12.62f * 32f,
                    damage: 28,
                    projectileSpeed: 6.5f * 32f / 60f,
                    projectileImage: Art.SpriteDarknessBolt,
                    accelerationMagnitude: -11f * 32f / 3600f,
                    minSpeed: 3f * 32f / 60f
                )
            );
        }
    }
}
