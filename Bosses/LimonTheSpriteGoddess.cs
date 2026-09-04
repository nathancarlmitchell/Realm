using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Realm;
using Realm.Projectiles;

namespace Realm.Bosses
{
    // The first boss, spawned inside BossRealmState (entered via the portal
    // SpriteGod drops on death, or Sprite World's own dungeon boss portal —
    // Data/DungeonType_SpriteWorld.json). Full rework sourced from
    // realmeye.com/wiki/sprite-world-guide's own "Boss" section — the
    // dormant/activation gate, the anti-rush horde, phase 1's 3-pattern
    // cycle, phase 2's 4 elemental transformations, and phase 3's
    // double-ring conveyor + quadrant relocation are all real wiki
    // mechanics, not invented.
    //
    // BossRealmState has no tile grid (an open Vector2-bounded arena, not a
    // DungeonMap) — the arena's own conveyor belts (both the phase 1/2
    // square border and phase 3's double ring) are therefore geometry-based
    // pushes computed directly against arenaCenter/ArenaHalfSize below,
    // architecturally separate from DungeonState's own TileDefData-based
    // conveyor mechanic (Sprite World's regular dungeon rooms/corridors).
    // Neither the square platform nor the double-ring floor pattern has any
    // dedicated art — no visual distinguishes the conveyor zones from the
    // rest of the arena, an explicit simplification, not an oversight.
    //
    // "Silence" isn't a debuff this engine has — every Silencing shot
    // elsewhere in Sprite World substitutes DazesOnHit; Limon's own kit
    // doesn't use Silence at all per the wiki, so this file has no such
    // substitution to make.
    class LimonTheSpriteGoddess : Boss
    {
        private enum Phase
        {
            Dormant,
            Phase1,
            Phase2,
            Phase3,
        }

        private enum SecondaryForm
        {
            Magic,
            Ice,
            Nature,
            Darkness,
        }

        private Phase currentPhase = Phase.Dormant;
        private SecondaryForm currentForm;
        private static readonly Random limonRand = new();

        private const float Phase2Threshold = 0.5f;
        private const float Phase3Threshold = 0.4f;
        private const int TransitionFrames = 60; // ~1s — the invulnerable window
        private const float ActivationRadius = 400f;

        // Captured once at spawn — every arena-geometry calculation below
        // (conveyor zones, phase 1's "remain in center," phase 3's quadrant
        // anchors) is relative to this rather than Limon's own (moving)
        // Position.
        private readonly Vector2 arenaCenter;
        private const float ArenaHalfSize = 500f; // matches the old SquareWall's own wallHalfSize

        public LimonTheSpriteGoddess(Vector2 position)
            : base(Art.Limon, position)
        {
            Name = "Limon the Sprite Goddess";
            arenaCenter = position;

            health = 12000;
            healthMax = 12000;
            Defense = 16;
            PointValue = 2000;
            deathSound = Sound.SpriteGodDeath;
            hitSound = Sound.SpriteGodHit;

            AddBehaviour(ActivationWatcher());
            AddBehaviour(PhaseWatcher());
            AddBehaviour(ArenaConveyorPush());

            AddBehaviour(DashMovement());
            AddBehaviour(Phase2Movement());
            AddBehaviour(Phase3Movement());

            AddAttackBehaviour(Phase1Patterns());
            AddAttackBehaviour(Phase2Attack());
            AddAttackBehaviour(Phase3Attacks());

            GuaranteedPotionChances = new()
            {
                [Potions.Dexterity] = 1.0f,
                [Potions.Defense] = 0.25f,
            };
        }

        // "When approached, Limon will flash red and attack." Merges the
        // wiki's two separate trigger paths (proximity, or taking damage
        // while a zero-Native-Sprite-kill run's own "stays vulnerable,
        // follows players" state) into one gate — either condition
        // activates her — a simplification of the wiki's own passive-
        // follow flavor state, not a mechanical difference in the fight
        // itself once activated.
        private IEnumerable<int> ActivationWatcher()
        {
            while (true)
            {
                if (currentPhase == Phase.Dormant)
                {
                    bool approached =
                        Vector2.DistanceSquared(Player.Instance.Position, Position)
                        <= ActivationRadius * ActivationRadius;
                    bool damaged = health < healthMax;

                    if (approached || damaged)
                    {
                        FlashRed();
                        currentPhase = Phase.Phase1;

                        // "if no sprites were killed before Limon was
                        // activated, she... [creates] 5-6 portals that
                        // summon a large number of assorted regular and
                        // Greater Sprites before attacking" — reuses the
                        // same 10 Native/Greater Sprite factories built for
                        // the regular dungeon roster; no new enemy types
                        // needed for the horde itself.
                        if (Enemy.NativeSpriteKillCount == 0)
                            SpawnHorde();
                    }
                }

                yield return 0;
            }
        }

        private static readonly Func<Vector2, Enemy>[] hordeFactories =
        {
            position => new NativeDarknessSprite(position),
            position => new NativeFireSprite(position),
            position => new NativeIceSprite(position),
            position => new NativeMagicSprite(position),
            position => new NativeNatureSprite(position),
            position => new NativeGreaterDarknessSprite(position),
            position => new NativeGreaterFireSprite(position),
            position => new NativeGreaterIceSprite(position),
            position => new NativeGreaterMagicSprite(position),
            position => new NativeGreaterNatureSprite(position),
        };

        private void SpawnHorde()
        {
            int waveCount = limonRand.Next(5, 7); // "5-6 portals"
            for (int i = 0; i < waveCount; i++)
            {
                float angle = (float)(limonRand.NextDouble() * MathHelper.TwoPi);
                float radius = ArenaHalfSize * 0.7f;
                Vector2 spawnPos = arenaCenter + Extensions.FromPolar(angle, radius);
                var factory = hordeFactories[limonRand.Next(hordeFactories.Length)];
                EntityManager.Add(factory(spawnPos));
            }
        }

        // Health-threshold phase transitions, re-checked every tick (same
        // "a single big hit crossing more than one threshold still visits
        // each phase in order" precedent as SnakepitGuard/
        // DreadstumpThePirateKing).
        private IEnumerable<int> PhaseWatcher()
        {
            while (true)
            {
                // pendingPhase == null guards against re-arming every tick
                // while currentPhase hasn't actually flipped yet —
                // TickPendingTransition() (ticked from
                // ArenaConveyorPush()) only flips currentPhase once
                // transitionFramesRemaining reaches 0, so without this
                // guard this loop would keep calling TransitionTo() (and
                // so keep resetting transitionFramesRemaining back to
                // TransitionFrames) on every single tick the health
                // threshold stays crossed, and the transition would never
                // actually complete.
                if (
                    pendingPhase == null
                    && currentPhase == Phase.Phase1
                    && HealthFraction <= Phase2Threshold
                )
                {
                    TransitionTo(Phase.Phase2);
                }
                else if (
                    pendingPhase == null
                    && currentPhase == Phase.Phase2
                    && HealthFraction <= Phase3Threshold
                )
                {
                    TransitionTo(Phase.Phase3);
                }

                yield return 0;
            }
        }

        private void TransitionTo(Phase next)
        {
            FlashRed();
            Invulnerable = true;
            transitionFramesRemaining = TransitionFrames;
            pendingPhase = next;
        }

        private Phase? pendingPhase = null;
        private int transitionFramesRemaining = 0;

        // Ticked from ArenaConveyorPush() below (already runs every tick
        // unconditionally) rather than its own separate coroutine, purely
        // so TransitionTo() above can stay a plain synchronous method
        // instead of also being a coroutine — the transition delay itself
        // doesn't need to block anything else.
        private void TickPendingTransition()
        {
            if (pendingPhase == null)
                return;

            if (transitionFramesRemaining > 0)
            {
                transitionFramesRemaining--;
                return;
            }

            if (pendingPhase == Phase.Phase2)
            {
                currentForm = (SecondaryForm)limonRand.Next(4);
            }

            // Phase 1's own ArmoredSpiral() pattern (and Phase 2's Magic
            // form, which reuses that same method — see Phase2Attack()'s
            // own comment) can leave Defense mid-boost via PeriodicArmor()
            // if the transition lands exactly while it's active; that
            // enumerator simply stops being MoveNext()'d the instant the
            // owning phase ends, so its own revert-to-base line never gets
            // a chance to run. Resetting explicitly here guarantees a
            // clean base Defense (16) at the start of every phase
            // regardless of what the previous phase's own pattern left
            // behind.
            Defense = 16;

            currentPhase = pendingPhase.Value;
            pendingPhase = null;
            Invulnerable = false;
        }

        // Geometry-based conveyor push — see this class's own header
        // comment for why this is separate from DungeonState's tile-based
        // mechanic. Clockwise tangent = Normalize(-rel.Y, rel.X) (screen
        // coordinates are Y-down, so increasing angle in (x, y) reads as
        // clockwise); counter-clockwise is the negation.
        private const float ConveyorSpeed = 1.5f;
        private const float BorderWidth = 64f;
        private const float InnerRingInner = ArenaHalfSize * 0.35f;
        private const float InnerRingOuter = ArenaHalfSize * 0.5f;

        private IEnumerable<int> ArenaConveyorPush()
        {
            while (true)
            {
                TickPendingTransition();

                Vector2 rel = Player.Instance.Position - arenaCenter;
                float dist = rel.Length();

                if (
                    (currentPhase == Phase.Phase1 || currentPhase == Phase.Phase2)
                    && dist >= ArenaHalfSize - BorderWidth
                    && dist <= ArenaHalfSize
                )
                {
                    Vector2 clockwise = dist > 0 ? new Vector2(-rel.Y, rel.X) / dist : Vector2.Zero;
                    Player.Instance.Position += clockwise * ConveyorSpeed;
                }
                else if (currentPhase == Phase.Phase3)
                {
                    if (dist >= ArenaHalfSize - BorderWidth && dist <= ArenaHalfSize)
                    {
                        Vector2 clockwise =
                            dist > 0 ? new Vector2(-rel.Y, rel.X) / dist : Vector2.Zero;
                        Player.Instance.Position += clockwise * ConveyorSpeed;
                    }
                    else if (dist >= InnerRingInner && dist <= InnerRingOuter)
                    {
                        Vector2 counterClockwise =
                            dist > 0 ? new Vector2(rel.Y, -rel.X) / dist : Vector2.Zero;
                        Player.Instance.Position += counterClockwise * ConveyorSpeed;
                    }
                }

                yield return 0;
            }
        }

        // Phase 1's own dash-circle pattern (pattern B below) nudges
        // Velocity directly from within DashMovement() rather than
        // Phase1Patterns() itself, so movement/attack stay split the same
        // way every other behaviour/attackBehaviour pair in this codebase
        // does (movement isn't blocked by Stunned, attacks are).
        private bool dashing = false;
        private Vector2 dashDirection;

        private IEnumerable<int> DashMovement()
        {
            while (true)
            {
                if (currentPhase == Phase.Phase1 && dashing && !Invulnerable)
                    Velocity += dashDirection * 0.6f;

                yield return 0;
            }
        }

        // Phase 1: "Limon cycles between the following patterns" — 3
        // patterns on a timer, only the active one's own MoveNext() ever
        // advances (built once here, matching NativeSpriteGod's own "don't
        // recreate a coroutine's enumerator every tick" precedent).
        private const int PatternDuration = 300; // ~5s each
        private int patternIndex = 0;
        private int patternTimer = 0;

        private IEnumerable<int> Phase1Patterns()
        {
            var burstPattern = EscalatingBurst().GetEnumerator();
            var dashPattern = DashLasers().GetEnumerator();
            var spiralPattern = ArmoredSpiral().GetEnumerator();

            while (true)
            {
                if (currentPhase == Phase.Phase1 && !Invulnerable)
                {
                    patternTimer++;
                    if (patternTimer >= PatternDuration)
                    {
                        patternTimer = 0;
                        patternIndex = (patternIndex + 1) % 3;
                        dashing = false;
                    }

                    switch (patternIndex)
                    {
                        case 0:
                            burstPattern.MoveNext();
                            break;
                        case 1:
                            dashPattern.MoveNext();
                            break;
                        default:
                            spiralPattern.MoveNext();
                            break;
                    }
                }

                yield return 0;
            }
        }

        // "Remaining in the center of the room, firing 3-way bursts of
        // orange lasers. She initially fires one shot per burst, and every
        // subsequent burst increases this number by 1, resetting after 5."
        private int burstShotCount = 1;
        private const int BurstCooldown = 90;
        private int burstCooldownRemaining = 0;

        private IEnumerable<int> EscalatingBurst()
        {
            while (true)
            {
                if (burstCooldownRemaining <= 0)
                {
                    burstCooldownRemaining = BurstCooldown;

                    Vector2 aim = Player.Instance.Position - Position;
                    float aimAngle = aim.LengthSquared() > 0 ? aim.ToAngle() : 0f;
                    for (int i = 0; i < burstShotCount; i++)
                    {
                        float shotAngle = aimAngle + (i - (burstShotCount - 1) / 2f) * 0.3f;
                        EntityManager.Add(
                            new EnemyProjectile(
                                Position,
                                Extensions.FromPolar(shotAngle, 6f * 32f / 60f),
                                Art.LimonProjectile
                            )
                            {
                                Damage = 45,
                            }
                        );
                    }

                    burstShotCount = burstShotCount >= 5 ? 1 : burstShotCount + 1;
                }
                else
                {
                    burstCooldownRemaining--;
                }

                yield return 0;
            }
        }

        // "Repeatedly dashes at the player in an attempt to circle them,
        // firing pairs of orange lasers aimed at them and additional
        // lasers perpendicular to the originals."
        private const int DashCooldown = 150;
        private const int DashDurationFrames = 30;
        private int dashCooldownRemaining = 0;
        private int dashFramesRemaining = 0;

        private IEnumerable<int> DashLasers()
        {
            while (true)
            {
                if (dashFramesRemaining > 0)
                {
                    dashFramesRemaining--;
                    if (dashFramesRemaining <= 0)
                        dashing = false;
                }
                else if (dashCooldownRemaining <= 0)
                {
                    dashCooldownRemaining = DashCooldown;

                    Vector2 aim = Player.Instance.Position - Position;
                    if (aim.LengthSquared() > 0)
                    {
                        dashDirection = aim.ScaleTo(1f);
                        dashing = true;
                        dashFramesRemaining = DashDurationFrames;

                        float aimAngle = aim.ToAngle();
                        float speed = 6f * 32f / 60f;
                        foreach (float offset in new[] { -0.15f, 0.15f })
                            EntityManager.Add(
                                new EnemyProjectile(
                                    Position,
                                    Extensions.FromPolar(aimAngle + offset, speed),
                                    Art.LimonProjectile
                                )
                                {
                                    Damage = 40,
                                }
                            );
                        foreach (
                            float perpendicular in new[]
                            {
                                aimAngle + MathHelper.PiOver2,
                                aimAngle - MathHelper.PiOver2,
                            }
                        )
                            EntityManager.Add(
                                new EnemyProjectile(
                                    Position,
                                    Extensions.FromPolar(perpendicular, speed),
                                    Art.LimonProjectile
                                )
                                {
                                    Damage = 40,
                                }
                            );
                    }
                }
                else
                {
                    dashCooldownRemaining--;
                }

                yield return 0;
            }
        }

        // "Armors herself and remains still, firing a 2-armed clockwise
        // spiral of orange lasers." Reuses PeriodicArmor's own Defense-
        // multiplier cycle for the Armored half; the spiral itself is a
        // small hand-rolled loop, one shot pair fired every few ticks at a
        // slowly-incrementing angle, since none of Spray/FanShot/Bomb build
        // up a rotating pattern over time the way a spiral needs.
        private float spiralAngle = 0f;
        private const int SpiralTickInterval = 4;
        private int spiralTicksRemaining = 0;

        private IEnumerable<int> ArmoredSpiral()
        {
            var armor = PeriodicArmor(intervalFrames: 0, durationFrames: PatternDuration).GetEnumerator();

            while (true)
            {
                armor.MoveNext();

                if (spiralTicksRemaining > 0)
                {
                    spiralTicksRemaining--;
                }
                else
                {
                    spiralTicksRemaining = SpiralTickInterval;
                    spiralAngle += 0.35f;
                    float speed = 5f * 32f / 60f;
                    EntityManager.Add(
                        new EnemyProjectile(
                            Position,
                            Extensions.FromPolar(spiralAngle, speed),
                            Art.LimonProjectile
                        )
                        {
                            Damage = 35,
                        }
                    );
                    EntityManager.Add(
                        new EnemyProjectile(
                            Position,
                            Extensions.FromPolar(spiralAngle + MathHelper.Pi, speed),
                            Art.LimonProjectile
                        )
                        {
                            Damage = 35,
                        }
                    );
                }

                yield return 0;
            }
        }

        // Phase 2 movement — one pattern per form, only ever the active
        // form's own logic runs (currentForm is fixed for the rest of the
        // fight once phase 2 begins, per TickPendingTransition() above).
        private Vector2 cornerTarget;
        private const float CornerSpeed = 2.2f;

        private IEnumerable<int> Phase2Movement()
        {
            while (true)
            {
                if (currentPhase == Phase.Phase2 && !Invulnerable)
                {
                    switch (currentForm)
                    {
                        case SecondaryForm.Magic:
                        {
                            // "Chase Limon as she travels from corner to
                            // corner" — she relocates between the arena's 4
                            // corners.
                            Vector2 toCorner = cornerTarget - Position;
                            if (toCorner.LengthSquared() < 64f * 64f)
                            {
                                cornerTarget =
                                    arenaCenter
                                    + new Vector2(
                                        limonRand.Next(2) == 0 ? -1 : 1,
                                        limonRand.Next(2) == 0 ? -1 : 1
                                    ) * (ArenaHalfSize - 80f);
                            }
                            else
                            {
                                Velocity += toCorner.ScaleTo(CornerSpeed) - Velocity * 0.5f;
                            }
                            break;
                        }
                        case SecondaryForm.Ice:
                            // "Stay in the middle... or stay close to the
                            // bottom" — stays put near the arena's own
                            // center, letting the arena's own hazards (not
                            // built) and her ring bursts do the work.
                            break;
                        case SecondaryForm.Nature:
                        {
                            // "Rotate with Limon" — she circles the player.
                            Vector2 toPlayer = Player.Instance.Position - Position;
                            float radius = 4f * 32f;
                            Vector2 desired =
                                Player.Instance.Position
                                - (
                                    toPlayer.LengthSquared() > 0
                                        ? toPlayer.ScaleTo(radius)
                                        : new Vector2(radius, 0)
                                );
                            Vector2 toDesired = desired - Position;
                            Velocity += toDesired.ScaleTo(0.5f) - Velocity * 0.3f;
                            break;
                        }
                        default: // Darkness
                            // "Hard to predict Limon's movement pattern" —
                            // an erratic wander, avoiding the corners
                            // ("stay away from the corners of the stage").
                            if (limonRand.Next(30) == 0)
                            {
                                float angle = (float)(limonRand.NextDouble() * MathHelper.TwoPi);
                                Velocity += Extensions.FromPolar(angle, 1.5f);
                            }
                            break;
                    }
                }

                yield return 0;
            }
        }

        // Phase 2 attack — one signature attack per form.
        private IEnumerable<int> Phase2Attack()
        {
            var magicSpiral = ArmoredSpiral().GetEnumerator(); // reused: "huge spiral of shots"
            var iceRing = FanShot(
                range: float.MaxValue,
                damage: 30,
                projectileSpeed: 4f * 32f / 60f,
                shots: 12,
                angleStep: MathHelper.TwoPi / 12f,
                projectileImage: Art.LimonProjectile,
                cooldownFrames: 120,
                slowsOnHit: true
            ).GetEnumerator();
            var natureBeams = ShootIfInRange(
                range: float.MaxValue,
                damage: 40,
                projectileSpeed: 5f * 32f / 60f,
                projectileImage: Art.LimonProjectile,
                cooldownFrames: 45
            ).GetEnumerator();
            var darknessBursts = FanShot(
                range: float.MaxValue,
                damage: 35,
                projectileSpeed: 6f * 32f / 60f,
                shots: 6,
                angleStep: MathHelper.TwoPi / 6f,
                projectileImage: Art.LimonProjectile,
                cooldownFrames: 100
            ).GetEnumerator();

            while (true)
            {
                if (currentPhase == Phase.Phase2 && !Invulnerable)
                {
                    switch (currentForm)
                    {
                        case SecondaryForm.Magic:
                            magicSpiral.MoveNext();
                            break;
                        case SecondaryForm.Ice:
                            iceRing.MoveNext();
                            break;
                        case SecondaryForm.Nature:
                            natureBeams.MoveNext();
                            break;
                        default:
                            darknessBursts.MoveNext();
                            break;
                    }
                }

                yield return 0;
            }
        }

        // Phase 3: reverted form, quadrant relocation.
        private int quadrantIndex = -1;
        private const float QuadrantSpeed = 2.5f;

        private Vector2 QuadrantAnchor(int index)
        {
            float offset = ArenaHalfSize * 0.5f;
            return index switch
            {
                0 => arenaCenter + new Vector2(-offset, -offset),
                1 => arenaCenter + new Vector2(offset, -offset),
                2 => arenaCenter + new Vector2(offset, offset),
                _ => arenaCenter + new Vector2(-offset, offset),
            };
        }

        private IEnumerable<int> Phase3Movement()
        {
            while (true)
            {
                if (currentPhase == Phase.Phase3 && !Invulnerable)
                {
                    if (quadrantIndex < 0)
                    {
                        quadrantIndex = limonRand.Next(4);
                    }

                    Vector2 target = QuadrantAnchor(quadrantIndex);
                    Vector2 toTarget = target - Position;
                    if (toTarget.LengthSquared() < 48f * 48f)
                    {
                        quadrantIndex = (quadrantIndex + 1) % 4;
                        OnQuadrantSwitch();
                    }
                    else
                    {
                        Velocity += toTarget.ScaleTo(QuadrantSpeed) - Velocity * 0.4f;
                    }
                }

                yield return 0;
            }
        }

        // "Every time she switches quadrants, she will fire a ring of fire
        // bolts that collapses on itself before firing outwards, as well
        // as a single aimed rainbow blast that deals heavy armor piercing
        // damage." The collapsing-then-expanding ring is approximated as a
        // plain outward ring burst (the collapse-first flourish is
        // skipped, same "visual flourish simplified away" precedent as
        // Fire Python's own wavy shots).
        private void OnQuadrantSwitch()
        {
            for (int i = 0; i < 16; i++)
            {
                EntityManager.Add(
                    new EnemyProjectile(
                        Position,
                        Extensions.FromPolar(i * (MathHelper.TwoPi / 16f), 4.5f * 32f / 60f),
                        Art.LimonProjectile
                    )
                    {
                        Damage = 30,
                        duration = 70,
                    }
                );
            }

            Vector2 aim = Player.Instance.Position - Position;
            if (aim.LengthSquared() > 0)
            {
                EntityManager.Add(
                    new EnemyProjectile(
                        Position,
                        Extensions.FromPolar(aim.ToAngle(), 8f * 32f / 60f),
                        Art.LimonProjectile
                    )
                    {
                        Damage = 120,
                        IgnoresDefense = true,
                    }
                );
            }
        }

        // "Periodically moving between these 4 quadrants, firing pairs of
        // wavy orange shots" — the continuous half of phase 3's attack;
        // OnQuadrantSwitch() above covers the discrete "every time she
        // switches" half.
        private const int WavyCooldown = 60;
        private int wavyCooldownRemaining = 0;

        private IEnumerable<int> Phase3Attacks()
        {
            while (true)
            {
                if (currentPhase == Phase.Phase3 && !Invulnerable)
                {
                    if (wavyCooldownRemaining <= 0)
                    {
                        wavyCooldownRemaining = WavyCooldown;
                        Vector2 aim = Player.Instance.Position - Position;
                        if (aim.LengthSquared() > 0)
                        {
                            float aimAngle = aim.ToAngle();
                            float speed = 5f * 32f / 60f;
                            foreach (float offset in new[] { -0.2f, 0.2f })
                                EntityManager.Add(
                                    new WavyProjectile(
                                        Position,
                                        Extensions.FromPolar(aimAngle + offset, speed),
                                        Art.LimonProjectile
                                    )
                                    {
                                        Damage = 45,
                                    }
                                );
                        }
                    }
                    else
                    {
                        wavyCooldownRemaining--;
                    }
                }

                yield return 0;
            }
        }
    }
}
