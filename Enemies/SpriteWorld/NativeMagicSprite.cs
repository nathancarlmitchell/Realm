using Microsoft.Xna.Framework;

namespace Realm
{
    // Sprite World (see Data/DungeonType_SpriteWorld.json) — sourced
    // directly from realmeye.com/wiki/native-magic-sprite. Real art
    // supplied (Content/Dungeons/Sprite World/). No dedicated "Blue Sprite
    // Magic" bolt art was supplied — reuses Art.SpriteMagicTwirl (see its
    // own Art.cs doc comment) rather than sitting without any real art.
    class NativeMagicSprite : Enemy
    {
        public NativeMagicSprite(Vector2 position)
            : base(Art.NativeMagicSprite, position)
        {
            health = 100;
            healthMax = 100;
            PointValue = 40;
            DropPool = SpriteWorldDropPool;
            DropChances = SpriteWorldDropChances;
            DropTierRanges = SpriteWorldDropTierRanges;
            CountsTowardNativeSpriteKills = true;

            AddBehaviour(MoveRandomly());

            // "Erratically moves around while firing 3-round bursts of blue
            // bolts."
            AddAttackBehaviour(
                FanShot(
                    range: 14.4f * 32f,
                    damage: 18,
                    projectileSpeed: 8f * 32f / 60f,
                    shots: 3,
                    angleStep: 0.15f,
                    projectileImage: Art.SpriteMagicTwirl
                )
            );
        }
    }
}
