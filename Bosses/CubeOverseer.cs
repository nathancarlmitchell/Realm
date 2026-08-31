using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm;
using Realm.Projectiles;

namespace Realm.Bosses
{
    // Cube God's own escort (https://www.realmeye.com/wiki/cube-overseer).
    // Real stats/attack this time, replacing entry 257's first-pass guess
    // (which — before this page was checked — assumed Overseer didn't
    // fight at all, going only off the boss page's own tips text). It does
    // fight: "wanders around on one spot... shooting a shotgun of 5 Orange
    // Magic projectiles towards the nearest player, every second shotgun is
    // closely followed by a single Fire Bolt." Replenished by
    // CubeGod.MaintainOverseers(), and in turn maintains its own small
    // cluster of minions via MaintainMinions() below — the "cube system"
    // the real fight scatters across the arena.
    class CubeOverseer : Enemy
    {
        private static readonly Random rand = new();

        // "Wanders around on one spot" — self-tethered to its own spawn
        // point (MoveTethered's default anchor), not tracking Cube God as
        // he drifts — same "only wanders in place" shape as
        // ScorpionQueen's own wander.
        private const float WanderDistance = 60f;
        private const float WanderSpeed = 0.05f;

        // Speed/range converted from the wiki's own tiles/sec and tiles
        // values (32px/tile, 60 ticks/sec) — real numbers, not guesses.
        // No explicit cadence given for "every second shotgun" beyond the
        // ordering itself — ShotgunCooldown is a first-pass tunable.
        private int shotgunCooldownRemaining = 0;
        private const int ShotgunCooldown = 80;
        private int shotgunCount = 0;
        private const int OrangeMagicPelletCount = 5;
        private const int OrangeMagicDamage = 60;
        private const float OrangeMagicSpread = 0.3f; // total fan width, radians
        private const float OrangeMagicSpeed = 10f * 32f / 60f; // 10 tiles/sec
        private const int OrangeMagicDuration = 144; // 24-tile range / speed
        private const int FireBoltDamage = 100;
        private const float FireBoltSpeed = 8f * 32f / 60f; // 8 tiles/sec
        private const int FireBoltDuration = 150; // 20-tile range / speed

        private const int TargetDefenderCount = 2;
        private const int TargetBlasterCount = 2;
        private const int MinionRespawnIntervalFrames = 450;

        // `owner` isn't stored/used for movement (see WanderDistance's own
        // comment — Overseer wanders in place, not tracking Cube God) but
        // stays a required parameter so CubeGod's own spawn calls stay
        // typed to a real CubeGod, matching every other escort constructor
        // in this codebase (e.g. LittleScorpion(ScorpionQueen owner, ...)),
        // and leaves room for a future "search for a new Overseer when
        // mine dies" behavior (see the boss page's own tips) without
        // another signature change.
        public CubeOverseer(CubeGod owner, Vector2 position)
            : base(Art.CubeOverseer, position)
        {
            health = 1500;
            healthMax = 1500;
            Defense = 0;
            PointValue = 0;
            DropsLoot = false;

            AddBehaviour(MoveTethered(wanderDistance: WanderDistance, speed: WanderSpeed));
            AddBehaviour(MaintainMinions());
            AddAttackBehaviour(ShotgunAttack());

            // Spawns its full Defender/Blaster complement immediately, per
            // direct request ("several of the minions spawn instantly in
            // the fight") — same "instant burst, then MaintainX() only
            // handles replacements" shape as
            // ScorpionQueen.MaintainScorpions(). Combined with CubeGod's own
            // constructor spawning every Overseer instantly too, the whole
            // "cube system" is present from the moment the fight starts.
            for (int i = 0; i < TargetDefenderCount; i++)
                EntityManager.Add(new CubeDefender(this, Position + rand.NextVector2(0f, 40f)));
            for (int i = 0; i < TargetBlasterCount; i++)
                EntityManager.Add(new CubeBlaster(this, Position + rand.NextVector2(0f, 40f)));
        }

        // "Shooting a shotgun of 5 Orange Magic projectiles towards the
        // nearest player, every second shotgun is closely followed by a
        // single Fire Bolt" — a running shotgunCount (not a coin flip)
        // makes that "every second" literal rather than probabilistic.
        private IEnumerable<int> ShotgunAttack()
        {
            while (true)
            {
                if (!Invulnerable && shotgunCooldownRemaining <= 0)
                {
                    Vector2 aim = Player.Instance.Position - Position;
                    if (aim.LengthSquared() > 0)
                    {
                        shotgunCooldownRemaining = ShotgunCooldown;
                        float aimAngle = aim.ToAngle();

                        float start = aimAngle - OrangeMagicSpread / 2f;
                        float step =
                            OrangeMagicPelletCount > 1
                                ? OrangeMagicSpread / (OrangeMagicPelletCount - 1)
                                : 0f;
                        for (int i = 0; i < OrangeMagicPelletCount; i++)
                        {
                            float angle = start + i * step;
                            EntityManager.Add(
                                new EnemyProjectile(
                                    Position,
                                    Extensions.FromPolar(angle, OrangeMagicSpeed),
                                    Art.OrangeMagic
                                )
                                {
                                    Damage = OrangeMagicDamage,
                                    duration = OrangeMagicDuration,
                                }
                            );
                        }

                        shotgunCount++;
                        if (shotgunCount % 2 == 0)
                            EntityManager.Add(
                                new EnemyProjectile(
                                    Position,
                                    Extensions.FromPolar(aimAngle, FireBoltSpeed),
                                    Art.FireBolt
                                )
                                {
                                    Damage = FireBoltDamage,
                                    duration = FireBoltDuration,
                                }
                            );
                    }
                }

                if (shotgunCooldownRemaining > 0)
                    shotgunCooldownRemaining--;

                yield return 0;
            }
        }

        // Tops this Overseer's own Defender/Blaster counts back up,
        // throttled to one spawn per type every MinionRespawnIntervalFrames
        // — same shape as ScorpionQueen.MaintainScorpions(), scoped to
        // Owner == this so multiple simultaneous Overseers never count each
        // other's minions (SandsmanKing's two independent MaintainX()
        // coroutines are the same idea for two escort types on one owner).
        private IEnumerable<int> MaintainMinions()
        {
            int defenderCooldownRemaining = MinionRespawnIntervalFrames;
            int blasterCooldownRemaining = MinionRespawnIntervalFrames;

            while (true)
            {
                int missingDefenders =
                    TargetDefenderCount
                    - EntityManager.CountWhere<CubeDefender>(d => d.Owner == this && !d.IsExpired);

                if (missingDefenders > 0)
                {
                    if (defenderCooldownRemaining <= 0)
                    {
                        EntityManager.Add(new CubeDefender(this, Position + rand.NextVector2(0f, 40f)));
                        defenderCooldownRemaining = MinionRespawnIntervalFrames;
                    }
                    else
                    {
                        defenderCooldownRemaining--;
                    }
                }

                int missingBlasters =
                    TargetBlasterCount
                    - EntityManager.CountWhere<CubeBlaster>(b => b.Owner == this && !b.IsExpired);

                if (missingBlasters > 0)
                {
                    if (blasterCooldownRemaining <= 0)
                    {
                        EntityManager.Add(new CubeBlaster(this, Position + rand.NextVector2(0f, 40f)));
                        blasterCooldownRemaining = MinionRespawnIntervalFrames;
                    }
                    else
                    {
                        blasterCooldownRemaining--;
                    }
                }

                yield return 0;
            }
        }
    }
}
