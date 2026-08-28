using Microsoft.Xna.Framework;

namespace Realm
{
    // A regular Beach wave enemy (BasicEnemyPool), not tied to any
    // mini-boss or group-spawn mechanic — near-identical stats to
    // Enemy.CreatePirate() (HP 5/PointValue 2/Damage 4/Range 2.4 tiles),
    // but given its own dedicated file rather than a bare factory method,
    // matching this session's more recent convention (Bandit.cs,
    // LittleScorpion.cs) rather than Pirate's older one.
    class Piratess : Enemy
    {
        private const float Range = 2.4f * 32f;
        private const int Damage = 4;
        private const float ProjectileSpeed = 4f * 32f / 60f; // 4 tiles/sec

        public Piratess(Vector2 position)
            : base(Art.Piratess, position)
        {
            health = 6;
            healthMax = 6;
            Defense = 0;
            PointValue = 2;
            DropPool = BeachDropPool;
            DropChances = BeachDropChances;
            DropTierRanges = BeachDropTierRanges;

            AddBehaviour(FollowPlayer(0.2f));
            AddAttackBehaviour(
                ShootIfInRange(
                    range: Range,
                    damage: Damage,
                    projectileSpeed: ProjectileSpeed,
                    projectileImage: Art.SwordSlash,
                    collisionShape: CollisionShape.Rectangle
                )
            );
        }
    }
}
