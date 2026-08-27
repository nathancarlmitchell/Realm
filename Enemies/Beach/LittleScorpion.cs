using Microsoft.Xna.Framework;
using Realm.Bosses;

namespace Realm
{
    // Escort spawned and replenished by ScorpionQueen.MaintainScorpions() —
    // deliberately not part of EnemySpawner.BasicEnemyPool, unlike
    // Pirate/Bandit, since "wanders close to the Scorpion Queen" only makes
    // sense with a live Queen to tether to; it never appears standalone.
    // Lives at the project root (matching Bandit.cs) even though
    // ScorpionQueen itself lives under Bosses/ — that's a file-organization
    // holdover from when ScorpionQueen was Beach's own mini-boss; she's now
    // a regular Beach wave enemy like everything else (Beached Buccaneer is
    // the only Beach mini-boss), but her escort mechanic is unchanged.
    class LittleScorpion : Enemy
    {
        public ScorpionQueen Owner { get; }

        // "Wanders around close to the Scorpion Queen" — tethered to the
        // Queen's live Position (MoveTethered's anchor parameter), not just
        // its own spawn point, so it keeps following her even as she drifts.
        private const float WanderDistance = 100f;
        private const float WanderSpeed = 0.15f;

        private const float AttackRange = 8f * 32f; // 8 tiles
        private const int AttackDamage = 7;
        private const float ProjectileSpeed = 4f * 32f / 60f; // 4 tiles/sec

        public LittleScorpion(ScorpionQueen owner, Vector2 position)
            : base(Art.LittleScorpion, position)
        {
            Owner = owner;

            health = 10;
            healthMax = 10;
            Defense = 0;
            PointValue = 2;
            DropPool = BeachDropPool;
            DropChances = BeachDropChances;
            DropTierRanges = BeachDropTierRanges;

            AddBehaviour(
                MoveTethered(wanderDistance: WanderDistance, speed: WanderSpeed, anchor: owner)
            );
            // "Shooting a single shot toward the closest player" — this
            // game has no multiplayer/multiple simultaneous players, so
            // "closest player" is just Player.Instance, same as every other
            // enemy's aim logic. A single shot per cooldown, fired only
            // once the player is in range, is exactly what ShootIfInRange()
            // already does.
            AddAttackBehaviour(
                ShootIfInRange(
                    range: AttackRange,
                    damage: AttackDamage,
                    projectileSpeed: ProjectileSpeed,
                    projectileImage: Art.SwordSlash
                )
            );
        }
    }
}
