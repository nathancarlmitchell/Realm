using System;
using Microsoft.Xna.Framework;
using Realm;

namespace Realm.Projectiles
{
    class Projectile : Entity
    {
        public int Duration = Player.Instance.Weapon.ProjectileDuration;
        public int Damage;
        public Guid ID;

        // Whether this projectile expires the moment it hits an enemy, or
        // keeps flying through (still only damages a given enemy once each,
        // via EntityManager's HitBy tracking — this only controls whether it
        // then continues on toward anything else in its path). Set once at
        // spawn time by whoever creates the projectile (Weapon.Shoot() for a
        // basic attack, each class's UseAbility() for its ability shot) —
        // not derived from live weapon state later, since that can change
        // mid-flight (e.g. switching weapons) and can't distinguish a basic
        // attack from an ability fired with the same weapon equipped.
        // Defaults true (expire on hit) so a spawn site that forgets to set
        // it explicitly fails safe rather than silently passing through
        // everything.
        public bool ExpiresOnHit = true;

        // Whether this projectile paralyzes the enemy it hits
        // (Enemy.Paralyze(), using that method's default duration). False for
        // everything except whatever a class explicitly opts in at spawn
        // time — e.g. Archer's Quiver ability.
        public bool ParalyzesOnHit = false;

        // Whether this projectile stuns the enemy it hits (Enemy.Stun(),
        // using that method's default duration) — the stronger debuff,
        // blocking attacks as well as movement, unlike ParalyzesOnHit above.
        // False for everything except whatever a class explicitly opts in —
        // e.g. Knight's Shield Slam.
        public bool StunsOnHit = false;

        // Whether this projectile's damage skips the target's Defense
        // reduction entirely (Enemy.WasShot()'s Armor Piercing path). False
        // for everything except whatever a class explicitly opts in — e.g.
        // Bow's Side shots.
        public bool IgnoresDefense = false;

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
            if (durationCooldown > Duration)
            {
                durationCooldown = 0;
                IsExpired = true;
            }
            durationCooldown++;
        }
    }
}
