using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm;
using Realm.Projectiles;

namespace Realm.Bosses
{
    // The third boss, spawned inside BossRealmState (entered via the portal
    // the new "Cube" trigger enemy drops on death — see Enemy.CreateCube()).
    // Adapted from RotMG's own Cube God (https://www.realmeye.com/wiki/cube-god),
    // a multiplayer-scaled fight (45,000 HP, a 13-bullet shotgun meant to be
    // split across a whole party) — numbers below are a single-player-scaled
    // first pass, not a 1:1 port, same adaptation Limon/Stheno's own source
    // material already got.
    class CubeGod : Boss
    {
        private static readonly Random rand = new();

        // "Slowly wanders the area."
        private const float WanderDistance = 200f;
        private const float WanderSpeed = 0.05f;

        // Two shotgun-style volleys fired from the same cooldown, matching
        // the wiki's "fires shotguns of Blue Magic which are sometimes
        // followed by a shotgun of Blue Bolts" — one combined coroutine
        // rather than two independently-cadenced attacks, so the "sometimes
        // followed by" reads as one attack chaining into another rather
        // than two unrelated attacks that happen to overlap.
        // Speed/range converted from the wiki's own tiles/sec and tiles
        // values (32px/tile, 60 ticks/sec) — real numbers, not guesses.
        private int volleyCooldownRemaining = 0;
        private const int VolleyCooldown = 100;
        private const int BlueMagicPelletCount = 9;
        private const int BlueMagicDamage = 60;
        private const float BlueMagicSpread = 0.5f; // total fan width, radians
        private const float BlueMagicSpeed = 10f * 32f / 60f; // 10 tiles/sec
        private const int BlueMagicDuration = 144; // 24-tile range / speed
        private const int BlueBoltsPelletCount = 7;
        private const int BlueBoltsDamage = 90;
        private const float BlueBoltsSpread = 0.3f;
        private const float BlueBoltsSpeed = 8f * 32f / 60f; // 8 tiles/sec
        private const int BlueBoltsDuration = 150; // 20-tile range / speed
        private const float ChainedBoltsChance = 0.4f;

        // "About every time it loses 1/3 HP, it will flash red and become
        // invulnerable for a short time" — two thresholds (2/3 and 1/3 HP),
        // each a one-shot flash/invuln window. Crossing 1/3 additionally
        // unlocks a permanent extra attack (EnrageBurst below) as the
        // closest single-player equivalent to the real fight's permanent
        // post-2/3-HP escalation — the real "gains Stun Immunity" has no
        // engine counterpart to guard against, since no player ability
        // stuns enemies today, so it isn't modeled as a flag nothing reads.
        private const float SecondFlashThreshold = 2f / 3f;
        private const float ThirdFlashThreshold = 1f / 3f;
        private const int PhaseInvulnFrames = 60;
        private bool secondFlashTriggered = false;
        private bool thirdFlashTriggered = false;

        private int enrageBurstCooldownRemaining = 0;
        private const int EnrageBurstCooldown = 130;
        private const int EnrageBurstPelletCount = 16;
        private const int EnrageBurstDamage = 50;
        private const float EnrageBurstSpeed = 4f * 32f / 60f;

        private const int TargetOverseerCount = 3;
        private const int OverseerRespawnIntervalFrames = 600;

        public CubeGod(Vector2 position)
            : base(Art.CubeGod, position)
        {
            Name = "Cube God";
            Description =
                "Cube + Godlike powers = Cube God. Happens to be three dimensional. Not a square.";

            health = 45000;
            healthMax = 45000;
            Defense = 40;
            PointValue = 25000;

            // No dedicated Cube God audio exists yet — placeholder, same
            // status as Limon/Stheno's own reused-family audio (see
            // docs/BACKLOG.md's boss-follow-ups item).
            deathSound = Sound.DefaultHit;
            hitSound = Sound.DefaultHit;

            AddBehaviour(MoveTethered(wanderDistance: WanderDistance, speed: WanderSpeed));
            AddBehaviour(PhaseWatcher());
            AddBehaviour(MaintainOverseers());
            AddAttackBehaviour(ShotgunVolleys());

            // Spawns the full Overseer complement immediately, per direct
            // request ("several of the minions spawn instantly in the
            // fight") — same "instant burst, then MaintainX() only handles
            // replacements" shape as ScorpionQueen.MaintainScorpions(). Each
            // Overseer's own constructor does the same for its Defender/
            // Blaster minions, so this alone seeds the whole "cube system"
            // at once rather than it slowly filling in over the opening
            // ~30 seconds.
            for (int i = 0; i < TargetOverseerCount; i++)
                EntityManager.Add(new CubeOverseer(this, Position + rand.NextVector2(0f, 80f)));

            GuaranteedPotionChances = new() { [Potions.Life] = 1.0f, [Potions.Defense] = 0.5f };
        }

        private IEnumerable<int> ShotgunVolleys()
        {
            while (true)
            {
                if (!Invulnerable && volleyCooldownRemaining <= 0)
                {
                    Vector2 aim = Player.Instance.Position - Position;
                    if (aim.LengthSquared() > 0)
                    {
                        volleyCooldownRemaining = VolleyCooldown;
                        float aimAngle = aim.ToAngle();

                        FireFan(
                            aimAngle,
                            BlueMagicPelletCount,
                            BlueMagicSpread,
                            BlueMagicSpeed,
                            BlueMagicDamage,
                            BlueMagicDuration,
                            Art.BlueMagic
                        );

                        if (rand.NextDouble() < ChainedBoltsChance)
                            FireFan(
                                aimAngle,
                                BlueBoltsPelletCount,
                                BlueBoltsSpread,
                                BlueBoltsSpeed,
                                BlueBoltsDamage,
                                BlueBoltsDuration,
                                Art.BlueBolt
                            );
                    }
                }

                if (volleyCooldownRemaining > 0)
                    volleyCooldownRemaining--;

                yield return 0;
            }
        }

        // A fan of `count` pellets spread evenly across `totalSpread`
        // radians, centered on `centerAngle` — same FromPolar fan technique
        // Enemy.Spray()/LimonTheSpriteGoddess.BossBurst() already use
        // internally, written bespoke here (rather than calling Spray())
        // since Spray() owns a single shared cooldown field and this boss
        // needs its own independent one (see Enemy.Spray()'s own comment on
        // why Limon's attacks are all bespoke coroutines for the same
        // reason).
        private void FireFan(
            float centerAngle,
            int count,
            float totalSpread,
            float speed,
            int damage,
            int duration,
            Texture2D image
        )
        {
            float start = centerAngle - totalSpread / 2f;
            float step = count > 1 ? totalSpread / (count - 1) : 0f;
            for (int i = 0; i < count; i++)
            {
                float angle = start + i * step;
                EntityManager.Add(
                    new EnemyProjectile(Position, Extensions.FromPolar(angle, speed), image)
                    {
                        Damage = damage,
                        duration = duration,
                        Shape = CollisionShape.Rectangle,
                        image = Art.BlueMagic,
                    }
                );
            }
        }

        // Permanent full-circle burst unlocked once HealthFraction crosses
        // 1/3 — same "evenly-spaced full circle via FromPolar" technique as
        // LimonTheSpriteGoddess.BossBurst().
        private IEnumerable<int> EnrageBurst()
        {
            while (true)
            {
                if (!Invulnerable && enrageBurstCooldownRemaining <= 0)
                {
                    enrageBurstCooldownRemaining = EnrageBurstCooldown;
                    for (int i = 0; i < EnrageBurstPelletCount; i++)
                    {
                        float angle = i * (MathHelper.TwoPi / EnrageBurstPelletCount);
                        EntityManager.Add(
                            new EnemyProjectile(
                                Position,
                                Extensions.FromPolar(angle, EnrageBurstSpeed)
                            )
                            {
                                Damage = EnrageBurstDamage,
                                Shape = CollisionShape.Rectangle,
                            }
                        );
                    }
                }

                if (enrageBurstCooldownRemaining > 0)
                    enrageBurstCooldownRemaining--;

                yield return 0;
            }
        }

        private IEnumerable<int> PhaseWatcher()
        {
            while (true)
            {
                if (!secondFlashTriggered && HealthFraction <= SecondFlashThreshold)
                {
                    secondFlashTriggered = true;
                    FlashRed();
                    Invulnerable = true;
                    for (int i = 0; i < PhaseInvulnFrames; i++)
                        yield return 0;
                    Invulnerable = false;
                }
                else if (!thirdFlashTriggered && HealthFraction <= ThirdFlashThreshold)
                {
                    thirdFlashTriggered = true;
                    FlashRed();
                    Invulnerable = true;
                    AddAttackBehaviour(EnrageBurst());
                    for (int i = 0; i < PhaseInvulnFrames; i++)
                        yield return 0;
                    Invulnerable = false;
                }

                yield return 0;
            }
        }

        // Tops the live Overseer count back up to TargetOverseerCount,
        // throttled to one spawn every OverseerRespawnIntervalFrames — same
        // shape as ScorpionQueen.MaintainScorpions(). Unscoped (not
        // Owner == this) since only one CubeGod ever exists per arena, same
        // reasoning as SthenoTheSnakeQueen.MaintainPets().
        private IEnumerable<int> MaintainOverseers()
        {
            int cooldownRemaining = OverseerRespawnIntervalFrames;
            while (true)
            {
                int missing =
                    TargetOverseerCount - EntityManager.CountWhere<CubeOverseer>(o => !o.IsExpired);

                if (missing > 0)
                {
                    if (cooldownRemaining <= 0)
                    {
                        EntityManager.Add(
                            new CubeOverseer(this, Position + rand.NextVector2(0f, 80f))
                        );
                        cooldownRemaining = OverseerRespawnIntervalFrames;
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
