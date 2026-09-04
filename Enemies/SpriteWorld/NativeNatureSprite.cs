using Microsoft.Xna.Framework;

namespace Realm
{
    // Sprite World (see Data/DungeonType_SpriteWorld.json) — sourced
    // directly from realmeye.com/wiki/native-nature-sprite. Real art
    // supplied (Content/Dungeons/Sprite World/).
    class NativeNatureSprite : Enemy
    {
        public NativeNatureSprite(Vector2 position)
            : base(Art.NativeNatureSprite, position)
        {
            health = 100;
            healthMax = 100;
            PointValue = 40;
            DropPool = SpriteWorldDropPool;
            DropChances = SpriteWorldDropChances;
            DropTierRanges = SpriteWorldDropTierRanges;
            CountsTowardNativeSpriteKills = true;

            // "Attempts to circle the player."
            AddBehaviour(OrbitPlayer(radius: 3f * 32f));

            // "fires 4-round shotguns of accelerating green bolts."
            AddAttackBehaviour(
                FanShot(
                    range: 13.78f * 32f,
                    damage: 30,
                    projectileSpeed: 3f * 32f / 60f,
                    shots: 4,
                    angleStep: 0.2f,
                    projectileImage: Art.SpriteNatureBolt,
                    accelerationMagnitude: 9f * 32f / 3600f,
                    maxSpeed: 10f * 32f / 60f
                )
            );
        }
    }
}
