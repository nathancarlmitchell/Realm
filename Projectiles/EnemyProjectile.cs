using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm.Projectiles
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

        // Whether this projectile applies Unstable to the player on hit
        // (Player.Destabilize(), using that method's default 1-second
        // duration) — same shape as SlowsOnHit above. False for everything
        // except whatever explicitly opts in — currently just Sand Devil's
        // spinner attack (see SandDevil.cs's SpinnerAttack()).
        public bool UnstablesOnHit = false;

        // Duration (in frames) passed to Player.Destabilize() when
        // UnstablesOnHit fires — defaults to Destabilize()'s own default
        // (180 frames = 3s) so existing callers that only set
        // UnstablesOnHit = true keep behaving exactly as before. Whoever
        // wants a different duration overrides this too, same shape as
        // Damage above.
        public int UnstableDurationFrames = 180;

        // Whether this projectile Dazes the player on hit (Player.Daze(),
        // see Entity.DebuffType.Dazed's own doc comment) — same
        // SlowsOnHit/UnstablesOnHit shape. First real use: Snakepit Guard's
        // Snake Spinners/Snake Balls (Snake Pit).
        public bool DazesOnHit = false;
        public int DazeDurationFrames = 120; // Player.Daze()'s own default

        // Whether this projectile Bleeds the player on hit (Player.Bleed())
        // — same shape again. First real use: Snakepit Dart Thrower's dart.
        public bool BleedsOnHit = false;
        public int BleedDurationFrames = 240; // Player.Bleed()'s own default

        // Whether a hit against the player consumes this projectile — true
        // (the original, still-default behavior) for everything except
        // whatever explicitly opts out, e.g. GrenadeProjectile, which stays
        // alive (and its hitbox live) for its whole duration regardless of
        // how many times the player is inside it. Mirrors the player's own
        // Projectile.ExpiresOnHit.
        public bool ExpiresOnHit = true;

        // Mirrors Projectile.PassesThroughObstacles — whether this projectile
        // ignores dungeon walls (TileDefData.CanShootThrough) entirely. False
        // for every enemy projectile today; the mechanism exists here too so
        // a future enemy/boss shot can opt in the same way the player's own
        // Quiver/Shield Slam do.
        public bool PassesThroughObstacles = false;

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
        //
        // shape: overrides the inherited Entity.Shape default (Circle) —
        // e.g. a wide slash/beam sprite that reads better hit-testing as a
        // rectangle (Bandit.cs's own sword slash). Left null, this is a
        // byte-for-byte no-op for every existing caller; same shape as
        // Enemy.ShootIfInRange()'s own collisionShape parameter, which now
        // just forwards here instead of setting Shape after construction.
        public EnemyProjectile(
            Vector2 position,
            Vector2 velocity,
            Texture2D image = null,
            CollisionShape? shape = null
        )
        {
            this.image = image ?? Art.EnemyProjectile;
            Position = position;
            Velocity = velocity;
            Orientation = Velocity.ToAngle();
            Radius = this.image.Width / 2f;
            if (shape.HasValue)
                Shape = shape.Value;
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
