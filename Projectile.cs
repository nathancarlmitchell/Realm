using System;
using Microsoft.Xna.Framework;
using Realm;

namespace Realm
{
    class Projectile : Entity
    {
        public int Duration = Player.Instance.Weapon.ProjectileDuration;
        public int Damage;
        public Guid ID;

        public Projectile(Vector2 position, Vector2 velocity)
        {
            ID = Guid.NewGuid();
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
            if (durationCooldown > Duration)
            {
                durationCooldown = 0;
                IsExpired = true;
            }
            durationCooldown++;
        }
    }
}
