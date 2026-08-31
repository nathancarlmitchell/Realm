using Microsoft.Xna.Framework;
using Realm;

namespace Realm.Bosses
{
    // One of Cube Overseer's two escort types (see
    // CubeOverseer.MaintainMinions()) — a frontline melee-range attacker,
    // matching the wiki's own "Defender" framing. Continuously replenished
    // like SthenoPet/SthenoSwarm, so PointValue/DropsLoot follow that same
    // "don't let it be farmed" convention rather than LittleScorpion's
    // (which does drop normal loot).
    class CubeDefender : Enemy
    {
        public CubeOverseer Owner { get; }

        private const float AttackRange = 5f * 32f; // 5 tiles
        private const int AttackDamage = 18;
        private const float ProjectileSpeed = 5f * 32f / 60f; // 5 tiles/sec
        private const int AttackCooldownFrames = 40;

        public CubeDefender(CubeOverseer owner, Vector2 position)
            : base(Art.HealthBar, position)
        {
            Owner = owner;

            health = 150;
            healthMax = 150;
            Defense = 3;
            PointValue = 0;
            DropsLoot = false;

            drawScale = 32f;
            Radius = 16f;
            tint = Color.IndianRed;

            AddBehaviour(FollowPlayer());
            AddAttackBehaviour(
                ShootIfInRange(
                    range: AttackRange,
                    damage: AttackDamage,
                    projectileSpeed: ProjectileSpeed,
                    cooldownFrames: AttackCooldownFrames
                )
            );
        }
    }
}
