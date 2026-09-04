using Microsoft.Xna.Framework;

namespace Realm
{
    // Sprite World (see Data/DungeonType_SpriteWorld.json) — sourced
    // directly from realmeye.com/wiki/native-ice-sprite. Real art supplied
    // (Content/Dungeons/Sprite World/).
    class NativeIceSprite : Enemy
    {
        public NativeIceSprite(Vector2 position)
            : base(Art.NativeIceSprite, position)
        {
            health = 100;
            healthMax = 100;
            PointValue = 40;
            DropPool = SpriteWorldDropPool;
            DropChances = SpriteWorldDropChances;
            DropTierRanges = SpriteWorldDropTierRanges;
            CountsTowardNativeSpriteKills = true;

            // "Slowly follows players" — a gentle acceleration, not a lunge.
            AddBehaviour(FollowPlayer(0.1f));

            // "fires slow blue crescents that inflict Slowed" — SlowsOnHit
            // reuses the existing fixed-duration Player.Slow() mechanic
            // (same as every other Slow-inflicting shot in this codebase),
            // not a bespoke 1.6s timer.
            AddAttackBehaviour(
                ShootIfInRange(
                    range: 12.5f * 32f,
                    damage: 24,
                    projectileSpeed: 2.5f * 32f / 60f,
                    projectileImage: Art.SpriteIceBolt,
                    slowsOnHit: true
                )
            );
        }
    }
}
