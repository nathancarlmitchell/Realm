using Microsoft.Xna.Framework;

namespace Realm
{
    // Sprite World (see Data/DungeonType_SpriteWorld.json) — sourced
    // directly from realmeye.com/wiki/native-greater-nature-sprite. Real
    // art supplied (Content/Dungeons/Sprite World/).
    class NativeGreaterNatureSprite : Enemy
    {
        public NativeGreaterNatureSprite(Vector2 position)
            : base(Art.NativeGreaterNatureSprite, position)
        {
            health = 1000;
            healthMax = 1000;
            Defense = 6;
            PointValue = 40;
            DropPool = SpriteWorldDropPool;
            DropChances = SpriteWorldDropChances;
            DropTierRanges = SpriteWorldDropTierRanges;
            CountsTowardNativeSpriteKills = true;

            // "Attempts to circle the player."
            AddBehaviour(OrbitPlayer(radius: 4f * 32f));

            // "firing green bolts that start off stationary, but rapidly
            // accelerate towards the position of the target at the time" —
            // projectileSpeed: 0 gives the "starts stationary" half
            // directly; accelerationMagnitude (aimed once, at fire time,
            // same as every other shot here — not continuously re-aimed)
            // gives the accelerate-toward-target half. The wiki's own
            // "after 0.6s" acceleration delay is simplified away — it
            // accelerates from the moment it's fired instead.
            AddAttackBehaviour(
                ShootIfInRange(
                    range: 14.36f * 32f,
                    damage: 37,
                    projectileSpeed: 0f,
                    projectileImage: Art.SpriteNatureGreaterShape,
                    accelerationMagnitude: 10f * 32f / 3600f,
                    maxSpeed: 7f * 32f / 60f
                )
            );
        }
    }
}
