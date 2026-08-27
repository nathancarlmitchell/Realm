using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm;

namespace Realm.Bosses
{
    // A regular Beach wave enemy (EnemySpawner.BasicEnemyPool) — no longer a
    // dedicated mini-boss spawn; Beached Buccaneer is now the only Beach
    // mini-boss. Its own dedicated class since, like ScorpionQueen, it
    // manages its own escorts whenever it spawns — but unlike ScorpionQueen
    // (one escort type, spawned as an instant burst), the King has two
    // independent escort types, each with its own Max/Cooldown and no
    // initial burst — both start at 0 and populate gradually over their own
    // stated cooldown, since the spec never says "spawns with N" the way
    // ScorpionQueen's did.
    class SandsmanKing : Enemy
    {
        // "Wanders aimlessly until it spots a player" — same one-way
        // wander-then-chase latch as BeachedBuccaneer.WanderThenChase(),
        // just renamed. TriggerRange (10 tiles) gates the aggro switch;
        // AttackRange (8.4 tiles) gates the attack itself, via
        // ShootIfInRange below. AttackRange < TriggerRange means a player
        // within attack range has necessarily already crossed TriggerRange
        // first, so the attack doesn't need its own separate hasAggroed
        // check — it's already consistent by construction.
        private const float TriggerRange = 10f * 32f;
        private const float AttackRange = 8.4f * 32f;
        private const int AttackDamage = 15;
        private const float ProjectileSpeed = 7f * 32f / 60f; // 7 tiles/sec
        private const int AttackCooldown = 600; // 10s at 60fps

        private const int MaxArchers = 2;
        private const int ArcherSpawnCooldownFrames = 600; // 10s

        private const int MaxSorcerers = 3;
        private const int SorcererSpawnCooldownFrames = 480; // 8s

        private static readonly Random rand = new();
        private bool hasAggroed = false;

        public SandsmanKing(Vector2 position)
            : base(Art.SandsmanKing, position)
        {
            health = 270;
            healthMax = 270;
            Defense = 2;
            PointValue = 86;
            DropPool = BeachDropPool;
            DropChances = BeachDropChances;
            DropTierRanges = BeachDropTierRanges;

            AddBehaviour(AggroWatcher());
            AddAttackBehaviour(
                ShootIfInRange(
                    range: AttackRange,
                    damage: AttackDamage,
                    projectileSpeed: ProjectileSpeed,
                    projectileImage: Art.SwordSlash,
                    cooldownFrames: AttackCooldown
                )
            );
            AddBehaviour(MaintainArchers());
            AddBehaviour(MaintainSorcerers());
        }

        private IEnumerable<int> AggroWatcher()
        {
            var wander = MoveTethered(wanderDistance: 250f, speed: 0.1f).GetEnumerator();
            var chase = FollowPlayer(0.15f).GetEnumerator();
            while (true)
            {
                if (
                    !hasAggroed
                    && Vector2.DistanceSquared(Player.Instance.Position, Position)
                        <= TriggerRange * TriggerRange
                )
                    hasAggroed = true;

                if (hasAggroed)
                    chase.MoveNext();
                else
                    wander.MoveNext();

                yield return 0;
            }
        }

        // Scoped to Owner == this so two live Kings never count (or cap)
        // each other's escorts — same reasoning as
        // ScorpionQueen.MaintainScorpions(). No initial burst here (unlike
        // ScorpionQueen's 10-at-once) — both escort types start at 0 and
        // fill in gradually, one per their own stated Cooldown, since only
        // Max/Cooldown were given, not an explicit starting count.
        private IEnumerable<int> MaintainArchers()
        {
            int cooldownRemaining = ArcherSpawnCooldownFrames;
            while (true)
            {
                int missing =
                    MaxArchers
                    - EntityManager.CountWhere<SandsmanArcher>(a => a.Owner == this && !a.IsExpired);

                if (missing > 0)
                {
                    if (cooldownRemaining <= 0)
                    {
                        EntityManager.Add(new SandsmanArcher(this, Position + rand.NextVector2(0f, 40f)));
                        cooldownRemaining = ArcherSpawnCooldownFrames;
                    }
                    else
                    {
                        cooldownRemaining--;
                    }
                }

                yield return 0;
            }
        }

        private IEnumerable<int> MaintainSorcerers()
        {
            int cooldownRemaining = SorcererSpawnCooldownFrames;
            while (true)
            {
                int missing =
                    MaxSorcerers
                    - EntityManager.CountWhere<SandsmanSorcerer>(s =>
                        s.Owner == this && !s.IsExpired
                    );

                if (missing > 0)
                {
                    if (cooldownRemaining <= 0)
                    {
                        EntityManager.Add(
                            new SandsmanSorcerer(this, Position + rand.NextVector2(0f, 40f))
                        );
                        cooldownRemaining = SorcererSpawnCooldownFrames;
                    }
                    else
                    {
                        cooldownRemaining--;
                    }
                }

                yield return 0;
            }
        }
    }
}
