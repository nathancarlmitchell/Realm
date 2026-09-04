using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm.Projectiles;

namespace Realm
{
    // Sprite World (see Data/DungeonType_SpriteWorld.json) — sourced
    // directly from realmeye.com/wiki/native-greater-magic-sprite. Real art
    // supplied (Content/Dungeons/Sprite World/). No dedicated FanShot/Spray/
    // ShootIfInRange support for a boomerang shot exists, so this fires its
    // own hand-rolled cooldown loop (same shape as Enemy.Bomb()'s own) that
    // spawns BoomerangProjectile directly instead.
    class NativeGreaterMagicSprite : Enemy
    {
        private int boomerangCooldownRemaining = 0;
        private const int BoomerangCooldown = 90;

        public NativeGreaterMagicSprite(Vector2 position)
            : base(Art.NativeGreaterMagicSprite, position)
        {
            health = 1000;
            healthMax = 1000;
            Defense = 6;
            PointValue = 40;
            DropPool = SpriteWorldDropPool;
            DropChances = SpriteWorldDropChances;
            DropTierRanges = SpriteWorldDropTierRanges;
            CountsTowardNativeSpriteKills = true;

            // "Maintains its distance" — barely wanders.
            AddBehaviour(MoveTethered(wanderDistance: 64f));

            AddAttackBehaviour(BoomerangBurst());
        }

        // "firing bursts of 1-3 long-ranged teal boomerangs" — 2 (the
        // middle of that range) fired in a narrow spread each burst.
        private IEnumerable<int> BoomerangBurst()
        {
            const float range = 24f * 32f;
            float rangeSquared = range * range;
            const float projectileSpeed = 6f * 32f / 60f;
            const int returnAfterFrames = 40;

            while (true)
            {
                Vector2 aim = Player.Instance.Position - Position;
                if (
                    aim.LengthSquared() > 0
                    && aim.LengthSquared() <= rangeSquared
                    && boomerangCooldownRemaining <= 0
                )
                {
                    boomerangCooldownRemaining = BoomerangCooldown;
                    float aimAngle = aim.ToAngle();

                    for (int i = 0; i < 2; i++)
                    {
                        float shotAngle = aimAngle + (i - 0.5f) * 0.15f;
                        Vector2 vel = Extensions.FromPolar(shotAngle, projectileSpeed);
                        EntityManager.Add(
                            new BoomerangProjectile(
                                Position,
                                vel,
                                returnAfterFrames,
                                Art.SpriteMagicTwirl
                            )
                            {
                                Damage = 39,
                            }
                        );
                    }
                }

                if (boomerangCooldownRemaining > 0)
                    boomerangCooldownRemaining--;

                yield return 0;
            }
        }
    }
}
