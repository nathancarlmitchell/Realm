using Microsoft.Xna.Framework;

namespace Realm
{
    // Sprite World (see Data/DungeonType_SpriteWorld.json) — sourced
    // directly from realmeye.com/wiki/native-greater-ice-sprite. Real art
    // supplied (Content/Dungeons/Sprite World/).
    class NativeGreaterIceSprite : Enemy
    {
        public NativeGreaterIceSprite(Vector2 position)
            : base(Art.NativeGreaterIceSprite, position)
        {
            health = 1000;
            healthMax = 1000;
            Defense = 6;
            PointValue = 40;
            DropPool = SpriteWorldDropPool;
            DropChances = SpriteWorldDropChances;
            DropTierRanges = SpriteWorldDropTierRanges;
            CountsTowardNativeSpriteKills = true;

            // "Remains mostly stationary, periodically charging at the
            // closest player" — the movement half, on its own timer.
            AddBehaviour(MoveTethered(wanderDistance: 32f));
            AddBehaviour(
                PeriodicCharge(intervalFrames: 150, chargeDurationFrames: 30, chargeSpeed: 0.6f)
            );

            // "shooting out a nova of 10 Slowing shots" — the attack half,
            // on a matching (not perfectly synced, an accepted
            // simplification) cadence via FanShot's own full-circle-step
            // trick (10 shots, angleStep = 360/10).
            AddAttackBehaviour(
                FanShot(
                    range: 4.8f * 32f,
                    damage: 10,
                    projectileSpeed: 8f * 32f / 60f,
                    shots: 10,
                    angleStep: MathHelper.TwoPi / 10f,
                    projectileImage: Art.SpriteIceTwirl,
                    cooldownFrames: 150,
                    slowsOnHit: true
                )
            );
        }
    }
}
