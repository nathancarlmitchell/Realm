using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    // A scheduled, invisible AoE damage tick — Priest's Tome Nova fires two
    // pulses (its first applied immediately in Priest.UseAbility(), its
    // second scheduled via one of these) rather than one instant hit. Not a
    // Projectile (no travel, no direction, no HitBy pass-through logic to
    // reuse) and not drawn — overrides Draw() to a no-op since the base
    // Entity.Draw() would otherwise try to draw a null image and crash.
    class NovaPulse : Entity
    {
        private readonly Vector2 center;
        private readonly float radius;
        private readonly int damage;
        private int ticksUntilPulse;

        public NovaPulse(Vector2 center, float radius, int damage, int delayTicks)
        {
            this.center = center;
            this.radius = radius;
            this.damage = damage;
            ticksUntilPulse = delayTicks;
            Position = center;
        }

        public override void Update()
        {
            ticksUntilPulse--;
            if (ticksUntilPulse <= 0)
            {
                EntityManager.DamageEnemiesInRadius(center, radius, damage);
                Particle.SpawnBurst(
                    center,
                    Color.White,
                    count: 10,
                    minSpeed: 1.5f,
                    maxSpeed: 4f,
                    lifespanTicks: 20
                );
                // Matches the orange scatter Priest.UseAbility() spawns on
                // the Nova's first, immediate pulse -- see Particle.
                // SpawnAreaBurst's own comment.
                Particle.SpawnAreaBurst(
                    center,
                    radius,
                    Color.Orange,
                    count: 16,
                    minSpeed: 1f,
                    maxSpeed: 5f,
                    lifespanTicks: 18
                );
                IsExpired = true;
            }
        }

        public override void Draw(SpriteBatch spriteBatch) { }
    }
}
