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

        // Forwarded straight to Player.Hit()'s own ignoresDefense param —
        // first real use: Limon the Sprite Goddess's phase 3 "rainbow
        // blast" (realmeye.com/wiki/sprite-world-guide's own "heavy armor
        // piercing damage"). False (the default) is a byte-for-byte no-op
        // for every existing projectile.
        public bool IgnoresDefense = false;

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

        // Added to Velocity every tick, then clamped to [MinSpeed, MaxSpeed]
        // (either bound left null skips that side of the clamp) — a
        // "decelerating shot" is just Acceleration pointed opposite the
        // firing direction with a MinSpeed floor above 0 (below that it'd
        // reverse rather than stop); an "accelerating shot" is the same
        // vector pointed along it with a MaxSpeed ceiling. First real use:
        // Sprite World's Native Sprites (realmeye.com/wiki/sprite-world),
        // several of which the wiki describes with an explicit
        // "Acceleration: N tiles/sec² ... Min./Max. Speed" pair. Zero (the
        // default) is a pure no-op, unchanged for every other projectile.
        public Vector2 Acceleration = Vector2.Zero;
        public float? MinSpeed = null;
        public float? MaxSpeed = null;

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

        // The direction speed is measured/clamped along — captured once,
        // lazily, on this projectile's first Update() tick (not in the
        // constructor: Acceleration/MinSpeed/MaxSpeed are all set via an
        // object initializer, which runs *after* the constructor body, so
        // they aren't reliably populated yet there). Falls back to
        // Acceleration's own direction when Velocity starts at zero (the
        // "delayed accelerate" pattern — Native Greater Nature Sprite spawns
        // its shot stationary, then accelerates toward a point).
        private bool accelerationDirectionResolved = false;
        private Vector2 accelerationDirection = Vector2.Zero;

        public override void Update()
        {
            if (Acceleration != Vector2.Zero)
            {
                if (!accelerationDirectionResolved)
                {
                    accelerationDirectionResolved = true;
                    accelerationDirection = (
                        Velocity != Vector2.Zero ? Velocity : Acceleration
                    ).ScaleTo(1f);
                }

                Velocity += Acceleration;

                // Clamped along accelerationDirection specifically, not by
                // raw magnitude — a magnitude-only clamp can't tell a
                // decelerating shot's speed dropping toward 0 from it
                // continuing past 0 and reversing direction entirely (its
                // magnitude keeps looking like a normal positive speed
                // either way). Measuring the signed speed *along* the
                // original direction catches that: once continued
                // deceleration would push it below MinSpeed, this pins
                // Velocity at exactly MinSpeed along that direction instead
                // (never negative — a decelerating shot that reaches
                // MinSpeed 0 genuinely stops, matching the wiki's own "Min.
                // Speed: 0" projectiles, rather than flying backward).
                float speedAlongDirection = Vector2.Dot(Velocity, accelerationDirection);
                if (MinSpeed.HasValue && speedAlongDirection < MinSpeed.Value)
                    Velocity = accelerationDirection * MinSpeed.Value;
                else if (MaxSpeed.HasValue && speedAlongDirection > MaxSpeed.Value)
                    Velocity = accelerationDirection * MaxSpeed.Value;
            }

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
