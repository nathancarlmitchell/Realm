using Microsoft.Xna.Framework;

namespace Realm
{
    // Sprite World (see Data/DungeonType_SpriteWorld.json) — sourced
    // directly from realmeye.com/wiki/native-greater-darkness-sprite. Real
    // art supplied (Content/Dungeons/Sprite World/).
    class NativeGreaterDarknessSprite : Enemy
    {
        public NativeGreaterDarknessSprite(Vector2 position)
            : base(Art.NativeGreaterDarknessSprite, position)
        {
            health = 1000;
            healthMax = 1000;
            Defense = 6;
            PointValue = 40;
            DropPool = SpriteWorldDropPool;
            DropChances = SpriteWorldDropChances;
            DropTierRanges = SpriteWorldDropTierRanges;
            CountsTowardNativeSpriteKills = true;

            // "Periodically 'warps' towards players" — a short, fast burst
            // rather than a normal sustained chase, approximating a warp.
            AddBehaviour(PeriodicCharge(intervalFrames: 180, chargeDurationFrames: 6, chargeSpeed: 3f));

            // "firing shotguns of stagnating black bullets" — Min. Speed: 0
            // means it can decelerate all the way to a stop, not just slow.
            AddAttackBehaviour(
                ShootIfInRange(
                    range: 5.33f * 32f,
                    damage: 42,
                    projectileSpeed: 5f * 32f / 60f,
                    projectileImage: Art.SpriteDarknessGreaterShape,
                    accelerationMagnitude: -15f * 32f / 3600f,
                    minSpeed: 0f
                )
            );
        }
    }
}
