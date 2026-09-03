using System;
using System.Collections.Generic;
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

        // Whether this projectile makes the enemy it hits Vulnerable
        // (Enemy.Vulnerable(), using that method's default duration) — a
        // flat increase to all damage the enemy takes while active, not a
        // movement/attack-blocking debuff like the two above. False for
        // everything except whatever a class explicitly opts in — e.g.
        // Archer's Quiver ability.
        public bool VulnerableOnHit = false;

        // Whether this projectile's damage skips the target's Defense
        // reduction entirely (Enemy.WasShot()'s Armor Piercing path). False
        // for everything except whatever a class explicitly opts in — e.g.
        // Bow's Side shots.
        public bool IgnoresDefense = false;

        // Whether this projectile Dazes the enemy it hits (Enemy.Daze(),
        // halving its next multi-shot attacks' projectile counts — see
        // Enemy.EffectiveShotCount()). Same shape as ParalyzesOnHit/
        // StunsOnHit above. Not used by any player class yet — added
        // alongside EnemyProjectile's own DazesOnHit so the Dazed debuff is
        // genuinely bidirectional infrastructure, not just a Snake-Pit-
        // specific, player-receiving-only mechanic.
        public bool DazesOnHit = false;
        public int DazeDurationFrames = 120; // Enemy.Daze()'s own default

        // Whether this projectile Bleeds the enemy it hits — unlike every
        // debuff above, this doesn't route through Enemy.ApplyDebuff()
        // directly; see Enemy.ApplyBleedStack() for why (stacking, not
        // refreshing). Not used by any player class yet, same "genuinely
        // bidirectional" reasoning as DazesOnHit above.
        public bool BleedsOnHit = false;
        public int BleedDurationFrames = 240; // 4s
        public float BleedDamagePerSecond = 20f; // "default 20/sec if unspecified"

        // Whether this projectile ignores dungeon walls entirely — set once
        // at spawn time, checked by DungeonState.ExpireWallBlockedProjectiles
        // () instead of TileDefData.CanShootThrough for this shot. False
        // (blocked by walls, the original default behavior) for everything
        // except whatever a class explicitly opts in — currently Archer's
        // Quiver ability and Knight's Shield Slam, both large, piercing
        // "signature ability" shots meant to read as unstoppable, not a
        // general trait of piercing shots (Bow/Wand's own basic-attack
        // piercing still gets walled normally — see ExpiresOnHit below for
        // how those pierce through *destructible* tiles instead). Even a
        // PassesThroughObstacles shot still damages a destructible tile it
        // flies over — "passes through" means never blocked/stopped by one,
        // not "ignores it entirely."
        public bool PassesThroughObstacles = false;

        // Tile cells (tile-space coordinates) this projectile has already
        // damaged — mirrors Enemy.HitBy's "only damage a given target once"
        // guard, just tracked from the projectile's side instead of the
        // target's, since a destructible tile has no equivalent list of its
        // own. Without this, a slow-moving or PassesThroughObstacles shot
        // that spends several frames over the same tile cell would damage it
        // once per frame instead of once total.
        public readonly HashSet<Point> DamagedTileCells = [];

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
