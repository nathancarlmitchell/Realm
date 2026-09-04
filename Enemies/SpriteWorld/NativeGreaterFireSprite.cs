using Microsoft.Xna.Framework;

namespace Realm
{
    // Sprite World (see Data/DungeonType_SpriteWorld.json) — sourced
    // directly from realmeye.com/wiki/native-greater-fire-sprite. Real art
    // supplied (Content/Dungeons/Sprite World/).
    class NativeGreaterFireSprite : Enemy
    {
        public NativeGreaterFireSprite(Vector2 position)
            : base(Art.NativeGreaterFireSprite, position)
        {
            health = 1000;
            healthMax = 1000;
            Defense = 6;
            PointValue = 40;
            DropPool = SpriteWorldDropPool;
            DropChances = SpriteWorldDropChances;
            DropTierRanges = SpriteWorldDropTierRanges;
            CountsTowardNativeSpriteKills = true;

            // "Maintains its distance while rapidly firing" — barely
            // wanders, matching MoveTethered's own small default range.
            AddBehaviour(MoveTethered(wanderDistance: 64f));

            // "rapidly firing slow, dense shotguns of orange shots" — no
            // exact shot count on the wiki table; 6 reads as "dense"
            // without becoming unreadable at this speed/range.
            AddAttackBehaviour(
                FanShot(
                    range: 5f * 32f,
                    damage: 33,
                    projectileSpeed: 2.5f * 32f / 60f,
                    shots: 6,
                    angleStep: 0.15f,
                    projectileImage: Art.SpriteFireGreaterShape,
                    cooldownFrames: 45
                )
            );
        }
    }
}
