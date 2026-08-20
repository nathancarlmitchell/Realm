using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm;

namespace Realm.Bosses
{
    // Stheno's orbiting add — circles her while trailing slowing green
    // orbs. Killed pets are replenished by SthenoTheSnakeQueen.
    // MaintainPets(), not by this class itself.
    class SthenoPet : Enemy
    {
        private readonly SthenoTheSnakeQueen owner;
        private float orbitAngle;

        private const float OrbitRadius = 220f;
        private const float OrbitSpeed = 0.03f; // radians/frame — "rapidly circle"

        private int trailCooldownRemaining = 0;
        private const int TrailCooldown = 45;

        // Must be finite, or an uncapped, constantly-respawning ring of
        // orbiting pets would carpet the arena in permanent slow zones.
        private const int TrailDuration = 240; // ~4s at 60fps

        public SthenoPet(SthenoTheSnakeQueen owner, float spawnAngle)
            : base(Art.SthenoPet, owner.Position)
        {
            this.owner = owner;
            orbitAngle = spawnAngle;

            health = 1000;
            healthMax = 1000;
            Defense = 7;
            PointValue = 0;
            DropsLoot = false;

            AddBehaviour(Orbit());
            AddAttackBehaviour(TrailOrbs());
        }

        // Re-derives Position from the boss's current Position every frame
        // — same technique LimonTheSpriteGoddess.UpdateSweepingShots() uses
        // to keep its wall projectiles tracking the boss.
        private IEnumerable<int> Orbit()
        {
            while (true)
            {
                orbitAngle = MathHelper.WrapAngle(orbitAngle + OrbitSpeed);
                Position = owner.Position + Extensions.FromPolar(orbitAngle, OrbitRadius);
                yield return 0;
            }
        }

        // Drops a stationary green orb at the pet's current position — pure
        // debuff, no direct damage. The pet's real threat is zoning
        // pressure from accumulating orbs, not damage; leaving the arena's
        // center to dodge them is the same thing that also stops Stheno's
        // own attacks (see SthenoTheSnakeQueen.PlayerInCenter).
        private IEnumerable<int> TrailOrbs()
        {
            while (true)
            {
                if (trailCooldownRemaining <= 0)
                {
                    trailCooldownRemaining = TrailCooldown;
                    EntityManager.Add(
                        new EnemyProjectile(Position, Vector2.Zero, Art.SthenoPetProjectile)
                        {
                            Damage = 65,
                            SlowsOnHit = true,
                            duration = TrailDuration,
                        }
                    );
                }

                if (trailCooldownRemaining > 0)
                    trailCooldownRemaining--;

                yield return 0;
            }
        }
    }
}
