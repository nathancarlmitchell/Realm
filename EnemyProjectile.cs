using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    class EnemyProjectile : Entity
    {
        public int duration = 250;

        // Defaults to the flat damage every enemy projectile has always dealt
        // to the player — set explicitly by whoever wants a harder-hitting
        // shot (e.g. a boss) without changing any other enemy's behavior.
        public int Damage = 10;

        // Whether this projectile slows the player on hit (Player.Slow(),
        // using that method's default duration) — the enemy-thrown mirror of
        // Projectile's ParalyzesOnHit/StunsOnHit. False for everything except
        // whatever explicitly opts in — e.g. a Stheno Pet's trailing orb.
        public bool SlowsOnHit = false;

        // Whether a hit against the player consumes this projectile — true
        // (the original, still-default behavior) for everything except
        // whatever explicitly opts out, e.g. GrenadeProjectile, which stays
        // alive (and its hitbox live) for its whole duration regardless of
        // how many times the player is inside it. Mirrors the player's own
        // Projectile.ExpiresOnHit.
        public bool ExpiresOnHit = true;

        // Set true the first time this projectile actually damages the
        // player (see EntityManager.HandleCollisions()) — prevents a
        // non-expiring projectile (ExpiresOnHit = false) from dealing
        // damage again on every subsequent frame the player still overlaps
        // it. Irrelevant for anything that still expires on hit, since
        // IsExpired already stops it from colliding again by then.
        public bool HasHitPlayer = false;

        // image defaults to the shared enemy projectile sprite; passing one
        // explicitly (e.g. a boss-specific projectile) has to happen here
        // rather than via an object initializer, since Radius is derived
        // from the image's own size and needs to be computed against
        // whichever image actually ends up used.
        public EnemyProjectile(Vector2 position, Vector2 velocity, Texture2D image = null)
        {
            this.image = image ?? Art.EnemyProjectile;
            Position = position;
            Velocity = velocity;
            Orientation = Velocity.ToAngle();
            Radius = this.image.Width / 2f;
        }

        private int durationCooldown = 0;

        public override void Update()
        {
            if (Velocity.LengthSquared() > 0)
                Orientation = Velocity.ToAngle();
            Position += Velocity * 1f;
            // delete bullets that go off-screen
            if (!Game1.GetWorldBounds(1.25f).Contains(Position.ToPoint()))
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
