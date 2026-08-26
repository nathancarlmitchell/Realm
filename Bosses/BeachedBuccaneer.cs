using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm;

namespace Realm.Bosses
{
    // Beach biome's mini-boss — not a full Boss subclass (no portal, no
    // separate arena), just a much tougher Enemy that spawns with a Pirate
    // escort, the same relationship Enemy.CreateBigSnake() already has with
    // CreateSnake() (see EnemySpawner.SpawnBeachedBuccaneerPack()). Kept as
    // its own dedicated class rather than a bare Enemy.CreateX() factory
    // method (unlike CreateBigSnake) because its behavior — a health-phase
    // transition, a randomized dual attack, taunt dialogue — needs bespoke
    // instance state (an enraged flag, its own attack cooldown) that
    // doesn't fit the generic reusable coroutines every other basic/
    // mini-boss enemy composes straight from Enemy.cs.
    class BeachedBuccaneer : Enemy
    {
        // Not Enemy's own private `rand` — that field isn't visible to a
        // subclass (private, not protected), so this needs its own.
        private static readonly Random rand = new();

        private static readonly string[] Taunts =
        [
            "My Finely crafted blade is the only thing I've got now! Ye debt collectors won't snatch it from my dead body!",
            "I'll cut yer sailling budget till yer nothin' but a penny pinching landlubber!",
            "This blade once cut down the finest of sea dogs, y'hear? I was a legend!",
            "I'll have ye blown back down the history of miserable landlubbers!",
            "My lady couldn't survive the wrath of the seas, and neither will you!",
        ];

        // Shared by the aggro trigger and the attack range gate below —
        // "approached"/"in range" in the spec is the same 7-tile
        // engagement distance as the projectile attack's own Range.
        private const float EngageRange = 7f * 32f;
        private const float AoeRange = 4f * 32f;
        private const float AoeRadius = 6f * 32f;
        private const int AttackCooldown = 120; // 2 seconds at 60fps
        private const float FanSpread = 0.15f; // radians between shots once multi-shot

        // 3.5 tiles/sec * 32px/tile / 60 ticks/sec (this project's usual
        // tiles-per-second -> px-per-tick conversion, e.g. Priest.cs's own
        // comments on the same math).
        private const float ProjectileSpeed = 3.5f * 32f / 60f;

        private bool hasAggroed = false;
        private bool enraged = false;
        private int attackCooldownRemaining = 0;

        public BeachedBuccaneer(Vector2 position)
            : base(Art.BeachedBuccaneer, position)
        {
            health = 500;
            healthMax = 500;
            Defense = 2;
            PointValue = 60;
            DropPool = BeachDropPool;
            DropChances = BeachDropChances;
            DropTierRanges = BeachDropTierRanges;

            AddBehaviour(WanderThenChase());
            AddAttackBehaviour(BuccaneerAttack());
            AddBehaviour(PhaseWatcher());
            AddBehaviour(TauntWhenPlayerNear(EngageRange, Taunts));
        }

        // "Walks aimlessly on his spot until approached. He will then
        // slowly chase..." — a one-way latch (aggroed sticks forever, no
        // "give up and wander again"), driving MoveTethered()'s and
        // FollowPlayer()'s own enumerators directly rather than running
        // both simultaneously (which would just add their two Velocity
        // contributions together every tick instead of switching between
        // them).
        private IEnumerable<int> WanderThenChase()
        {
            var wander = MoveTethered(wanderDistance: 150f, speed: 0.1f).GetEnumerator();
            var chase = FollowPlayer(0.15f).GetEnumerator();
            while (true)
            {
                if (
                    !hasAggroed
                    && Vector2.DistanceSquared(Player.Instance.Position, Position)
                        <= EngageRange * EngageRange
                )
                    hasAggroed = true;

                if (hasAggroed)
                    chase.MoveNext();
                else
                    wander.MoveNext();

                yield return 0;
            }
        }

        // At 50% health: a FlashRed() cue, a one-shot taunt, "Wooden Shield
        // Armored" (+2 Defense, matching the Tier 0 Wooden Shield's own
        // DefenseBonus in Data/ShieldData.json), and BuccaneerAttack()
        // below starts firing multi-shot instead of single-shot (reads
        // `enraged`, set here).
        private IEnumerable<int> PhaseWatcher()
        {
            while (true)
            {
                if (!enraged && HealthFraction <= 0.5f)
                {
                    enraged = true;
                    FlashRed();
                    Defense += 2;
                    EntityManager.Add(
                        new TauntBubble(
                            this,
                            "Now you've done it! A proper challenge for a pirate like me!"
                        )
                    );
                }
                yield return 0;
            }
        }

        // "He will then slowly chase and attack the nearest player with
        // either white bolts or red AoE grenades" — one shared cooldown
        // (the AoE's own stated 2-second Cooldown; the projectile attack
        // has no separately-stated one), randomly choosing between the two
        // each time it's ready. Gated on hasAggroed, not just range — no
        // attacks during the pre-aggro wander, matching the spec's own
        // "he will then... attack" phrasing.
        private IEnumerable<int> BuccaneerAttack()
        {
            while (true)
            {
                if (hasAggroed && attackCooldownRemaining <= 0)
                {
                    Vector2 toPlayer = Player.Instance.Position - Position;
                    if (toPlayer.LengthSquared() <= EngageRange * EngageRange)
                    {
                        attackCooldownRemaining = AttackCooldown;

                        if (rand.Next(2) == 0)
                            FireBolts(toPlayer);
                        else
                            ThrowGrenade();
                    }
                }
                else if (attackCooldownRemaining > 0)
                {
                    attackCooldownRemaining--;
                }

                yield return 0;
            }
        }

        // 1 bolt normally, 2-3 in a fan once enraged ("starts firing 2 to 3
        // bullets at once").
        private void FireBolts(Vector2 toPlayer)
        {
            float aimAngle = toPlayer.ToAngle();
            int shots = enraged ? rand.Next(2, 4) : 1;
            float startAngle = aimAngle - FanSpread * (shots - 1) / 2f;

            for (int i = 0; i < shots; i++)
            {
                Vector2 vel = Extensions.FromPolar(startAngle + i * FanSpread, ProjectileSpeed);
                EntityManager.Add(new EnemyProjectile(Position, vel, Art.WhiteBolt) { Damage = 8 });
            }
        }

        // Thrown at the player's current position, clamped to AoeRange from
        // the boss — same Range-clamp pattern as Priest's Tome nova
        // (CharacterClasses/Priest.cs).
        private void ThrowGrenade()
        {
            Vector2 toTarget = Player.Instance.Position - Position;
            if (toTarget.LengthSquared() > AoeRange * AoeRange)
                toTarget = Vector2.Normalize(toTarget) * AoeRange;

            EntityManager.Add(new GrenadeProjectile(Position + toTarget, AoeRadius, 12));
        }
    }
}
