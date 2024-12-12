using Microsoft.Xna.Framework;

namespace Realm
{
    class EnemyProjectile : Entity
    {
        public int duration = 250;

        public EnemyProjectile(Vector2 position, Vector2 velocity)
        {
            image = Art.EnemyProjectile;
            Position = position;
            Velocity = velocity;
            Orientation = Velocity.ToAngle();
            Radius = image.Width / 2f;
        }

        private int durationCooldown = 0;

        public override void Update()
        {
            if (Velocity.LengthSquared() > 0)
                Orientation = Velocity.ToAngle();
            Position += Velocity * 1f;
            // delete bullets that go off-screen
            if (!Game1.WorldBounds.Contains(Position.ToPoint()))
                IsExpired = true;
            if (durationCooldown > duration)
            {
                durationCooldown = 0;
                IsExpired = true;
            }
            durationCooldown++;
        }
    }
}
