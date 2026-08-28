using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm;

namespace Realm.Bosses
{
    // A regular Beach wave enemy (EnemySpawner.BasicEnemyPool) — no longer
    // a dedicated mini-boss/escort-pack spawn; Beached Buccaneer is now the
    // only Beach mini-boss. Its own dedicated class (rather than a bare
    // Enemy.CreateX() factory) since it needs bespoke instance state (a
    // one-time flee trigger, its own AoE cooldown) that doesn't fit the
    // generic reusable coroutines alone. The main projectile attack DOES
    // fit one of those generics (ShootIfInRange), reused directly rather
    // than hand-duplicated.
    class BanditLeader : Enemy
    {
        private static readonly Random rand = new();

        private const float MainRange = 4.8f * 32f;
        private const int MainDamage = 9;
        private const float ProjectileSpeed = 6f * 32f / 60f; // 6 tiles/sec

        private const float AoeRange = 6f * 32f;
        private const float AoeRadius = 2f * 32f;
        private const int AoeDamage = 12;
        private const int AoeCooldown = 120; // 2 seconds at 60fps

        // No explicit percentage given in the spec ("runs away... when low
        // on health") — 25% chosen as a clearly-critical threshold, lower
        // than BeachedBuccaneer's spec'd 50% enrage point since fleeing is
        // a much more drastic response than a temporary buff. Tunable.
        private const float FleeHealthThreshold = 0.25f;

        // "Catch!" only fires on a fraction of grenade throws (not every
        // one) — the AoE's own 2-second cooldown would otherwise repeat it
        // so often it reads as spam rather than a bark. Tunable.
        private const float CatchTauntChance = 0.35f;

        private bool hasFled = false;
        private int aoeCooldownRemaining = 0;

        public BanditLeader(Vector2 position)
            : base(Art.BanditLeader, position)
        {
            health = 280;
            healthMax = 280;
            Defense = 2;
            PointValue = 88;
            DropPool = BeachDropPool;
            DropChances = BeachDropChances;
            DropTierRanges = BeachDropTierRanges;

            AddBehaviour(FleeWatcher());
            AddAttackBehaviour(
                ShootIfInRange(
                    range: MainRange,
                    damage: MainDamage,
                    projectileSpeed: ProjectileSpeed,
                    projectileImage: Art.SwordSlash,
                    collisionShape: CollisionShape.Rectangle
                )
            );
            AddAttackBehaviour(ThrowGrenades());
        }

        // "Chases nearby players... runs away from players when low on
        // health" — no explicit re-engage condition once fled (this enemy
        // has no health regen to ever raise HealthFraction back up), so a
        // one-way latch and a live per-tick check are behaviorally
        // identical here; the latch is what also gates the one-time flee
        // taunt below from repeating every tick while low.
        private IEnumerable<int> FleeWatcher()
        {
            var chase = FollowPlayer(0.15f).GetEnumerator();
            var flee = FleePlayer(0.25f).GetEnumerator();
            while (true)
            {
                if (!hasFled && HealthFraction <= FleeHealthThreshold)
                {
                    hasFled = true;
                    // "…" (Unicode ellipsis) in the user's original text
                    // replaced with 3 ASCII periods — Art.HudFont's
                    // SpriteFont has no glyph for it, and
                    // SpriteFont.MeasureString()/DrawString() both throw
                    // ArgumentException on an unresolvable character, which
                    // would have crashed the whole game the instant a real
                    // player triggered this taunt in actual play. Found via
                    // this feature's own scripted test.
                    EntityManager.Add(new TauntBubble(this, "Forget this...run for it!"));
                }

                if (hasFled)
                    flee.MoveNext();
                else
                    chase.MoveNext();

                yield return 0;
            }
        }

        // "Throwing small grenades every few seconds" — thrown directly at
        // the player's current position; no Range-clamp needed the way
        // BeachedBuccaneer's AoE has one, since this is already gated to
        // only fire when the player is within AoeRange in the first place.
        private IEnumerable<int> ThrowGrenades()
        {
            while (true)
            {
                if (aoeCooldownRemaining <= 0)
                {
                    Vector2 toPlayer = Player.Instance.Position - Position;
                    if (toPlayer.LengthSquared() <= AoeRange * AoeRange)
                    {
                        aoeCooldownRemaining = AoeCooldown;
                        EntityManager.Add(
                            new GrenadeProjectile(Position + toPlayer, AoeRadius, AoeDamage)
                        );
                        if (rand.NextDouble() < CatchTauntChance)
                            EntityManager.Add(new TauntBubble(this, "Catch!"));
                    }
                }
                else
                {
                    aoeCooldownRemaining--;
                }

                yield return 0;
            }
        }
    }
}
