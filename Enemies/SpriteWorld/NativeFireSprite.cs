using Microsoft.Xna.Framework;

namespace Realm
{
    // Sprite World (see Data/DungeonType_SpriteWorld.json) — sourced
    // directly from realmeye.com/wiki/native-fire-sprite. Real art supplied
    // (Content/Dungeons/Sprite World/).
    class NativeFireSprite : Enemy
    {
        public NativeFireSprite(Vector2 position)
            : base(Art.NativeFireSprite, position)
        {
            health = 100;
            healthMax = 100;
            PointValue = 40;
            DropPool = SpriteWorldDropPool;
            DropChances = SpriteWorldDropChances;
            DropTierRanges = SpriteWorldDropTierRanges;
            CountsTowardNativeSpriteKills = true;

            AddBehaviour(MoveRandomly());

            // "Erratically moves around while firing pairs of fire shots at
            // a nonstop rate" — a narrow 2-shot fan, short cooldown for the
            // "nonstop" cadence.
            AddAttackBehaviour(
                FanShot(
                    range: 7f * 32f,
                    damage: 27,
                    projectileSpeed: 3.5f * 32f / 60f,
                    shots: 2,
                    angleStep: 0.15f,
                    projectileImage: Art.SpriteFireBolt,
                    cooldownFrames: 60
                )
            );
        }
    }
}
