using Microsoft.Xna.Framework;
using Realm;

namespace Realm
{
    class Projectile : Entity
    {
        private int duration = Player.ProjectileDuration;
        public int Damage;

        public Projectile(Vector2 position, Vector2 velocity)
        {
            image = Art.Projectile;
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
            //if (!Game1.Viewport.Bounds.Contains(Position.ToPoint()))
            //IsExpired = true;
            if (durationCooldown > duration)
            {
                durationCooldown = 0;
                IsExpired = true;
            }
            durationCooldown++;
        }
    }
}
