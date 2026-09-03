using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm;
using Realm.Projectiles;

namespace Realm.Bosses
{
    // The boss of Pirate Cave (Data/DungeonType_PirateCave.json), spawned
    // inside BossRealmState via the portal Pirate Cave drops in its farthest
    // room (Portal.Destination.DreadstumpBossRealm). Health-threshold phases
    // (Enemy.HealthFraction — same "PhaseWatcher() polls health each frame"
    // shape as LimonTheSpriteGoddess), unlike Stheno's fixed-timer phases —
    // the wiki's own phase descriptions read as progression-driven, not
    // time-boxed. All numbers below are read directly off the wiki's own
    // attack table (realmeye.com/wiki/dreadstump-the-pirate-king); a
    // first-pass build like Stheno/Cube God, not yet playtested.
    class DreadstumpThePirateKing : Boss
    {
        private static readonly Random rand = new();

        private enum Phase
        {
            Kiting,
            Circling,
            ShipCannons,
            Armored,
        }

        private Phase currentPhase = Phase.Kiting;

        private const float Phase2Threshold = 0.75f;
        private const float Phase3Threshold = 0.50f;
        private const float Phase4Threshold = 0.25f;
        private const int TransitionFrames = 60; // ~1s — the invulnerable window

        // The arena's true center, recovered from BossRealmState's fixed
        // spawn offset (position = center + (0,-600)), same trick Stheno's
        // own roomCenter already uses.
        private readonly Vector2 roomCenter;

        private readonly int baseDefense;

        public DreadstumpThePirateKing(Vector2 position)
            : base(Art.DreadstumpThePirateKing, position)
        {
            Name = "Dreadstump the Pirate King";
            Description =
                "Once the most dreaded pirate that has ever put a sail to the wind, Dreadstump "
                + "has become quite complacent wasting away in his hideout. He was responsible "
                + "for extorting liquor as Oryx's tax, but was found drinking it.";

            health = 1000;
            healthMax = 1000;
            Defense = 6;
            baseDefense = Defense;
            PointValue = 200;

            // Same pool/tier-range table as every regular Pirate Cave enemy
            // (Enemy.PirateCaveDropPool/PirateCaveDropTierRanges) — but see
            // SpawnLoot() below, which guarantees exactly one item from it
            // rather than every category at once like the other three
            // bosses' SpawnGuaranteedLoot(). Per direct request.
            DropPool = PirateCaveDropPool;
            DropTierRanges = PirateCaveDropTierRanges;

            // No dedicated Dreadstump audio yet — reuses the shared default,
            // same placeholder-audio status as Cube God.
            deathSound = Sound.DefaultHit;
            hitSound = Sound.DefaultHit;

            roomCenter = position + new Vector2(0, 600);

            AddBehaviour(PhaseWatcher());
            AddBehaviour(KitingMovement());
            AddBehaviour(CirclingMovement());
            AddAttackBehaviour(KitingShots());
            AddAttackBehaviour(CirclingAttack());
            AddAttackBehaviour(ArmorBurst());
            AddAttackBehaviour(ShipCannons());
            AddAttackBehaviour(BigBursts());
        }

        // Overrides Boss's own default (SpawnGuaranteedLoot — one item
        // guaranteed per DropPool category, every time) with a single
        // guaranteed item picked among the same categories instead — a
        // deliberately more modest guaranteed reward than Limon/Stheno/
        // CubeGod's own multi-item hauls, matching Pirate Cave's status as
        // the beginner dungeon. Per direct request ("same table... but a
        // guaranteed chance of 1 item").
        protected override void SpawnLoot(List<Item> extraItems = null) =>
            ItemSpawner.SpawnGuaranteedSingleItem(Position, DropPool, DropTierRanges, extraItems);

        // Polls health each frame; once it crosses a threshold, briefly goes
        // Invulnerable (with a red flash) and advances currentPhase — same
        // shape as Stheno's PhaseTimer(), just health-gated instead of
        // time-gated. Re-checks after every transition, so a single big hit
        // that crosses more than one threshold at once still visits each
        // phase transition in order rather than skipping straight to the
        // final one.
        private IEnumerable<int> PhaseWatcher()
        {
            while (true)
            {
                Phase target = HealthFraction switch
                {
                    <= Phase4Threshold => Phase.Armored,
                    <= Phase3Threshold => Phase.ShipCannons,
                    <= Phase2Threshold => Phase.Circling,
                    _ => Phase.Kiting,
                };

                if (target != currentPhase)
                {
                    FlashRed();
                    Invulnerable = true;

                    for (int i = 0; i < TransitionFrames; i++)
                        yield return 0;

                    currentPhase = target;
                    Invulnerable = false;

                    // "Armored" is permanent from here on, unlike the
                    // temporary self-Armor buff phases 2-3 occasionally
                    // flash into (ArmorBurst() below, which never runs once
                    // this phase is reached).
                    if (currentPhase == Phase.Armored)
                        Defense = (int)(baseDefense * 1.5f);
                }

                yield return 0;
            }
        }

        // Phase 1: "attacks the nearest player with rapid yellow shots
        // while keeping his distance."
        private IEnumerable<int> KitingMovement()
        {
            while (true)
            {
                if (currentPhase == Phase.Kiting && !Invulnerable)
                {
                    Vector2 away = Position - Player.Instance.Position;
                    if (away != Vector2.Zero)
                        Velocity += away.ScaleTo(0.3f);
                }

                yield return 0;
            }
        }

        private int kitingCooldownRemaining = 0;
        private const int KitingCooldown = 45;
        private const int KitingDamage = 14;
        private const float KitingSpeed = 4.5f * 32f / 60f;
        private const float KitingRange = 13.5f * 32f;

        private IEnumerable<int> KitingShots()
        {
            while (true)
            {
                if (currentPhase == Phase.Kiting && !Invulnerable)
                {
                    if (kitingCooldownRemaining <= 0)
                    {
                        Vector2 aim = Player.Instance.Position - Position;
                        if (aim.LengthSquared() > 0 && aim.LengthSquared() <= KitingRange * KitingRange)
                        {
                            kitingCooldownRemaining = KitingCooldown;
                            EntityManager.Add(
                                new EnemyProjectile(Position, aim.ScaleTo(KitingSpeed), Art.GoldShot)
                                {
                                    Damage = KitingDamage,
                                }
                            );
                        }
                    }

                    if (kitingCooldownRemaining > 0)
                        kitingCooldownRemaining--;
                }

                yield return 0;
            }
        }

        // Phases 2-4: "starts circling players" around the ship's mast
        // (roomCenter) — same math as Enemy's own OrbitPoint primitive,
        // inlined rather than called directly since it only needs to
        // advance while gated (OrbitPoint's own while(true) loop has no way
        // to pause without also freezing whichever phase check gates it).
        private IEnumerable<int> CirclingMovement()
        {
            const float radius = 250f;
            const float angularSpeed = 0.015f;
            float angle = rand.NextFloat(0, MathHelper.TwoPi);

            while (true)
            {
                if (currentPhase != Phase.Kiting && !Invulnerable)
                {
                    angle = MathHelper.WrapAngle(angle + angularSpeed);
                    Vector2 target = roomCenter + Extensions.FromPolar(angle, radius);
                    Vector2 toTarget = target - Position;
                    if (toTarget != Vector2.Zero)
                        Velocity += toTarget.ScaleTo(0.4f);
                }

                yield return 0;
            }
        }

        // Phases 2-4: "alternating between single cutlass shots and single
        // cannonballs."
        private bool nextIsCannonball = false;
        private int circlingCooldownRemaining = 0;
        private const int CirclingCooldown = 50;
        private const int CutlassDamage = 18;
        private const float CutlassSpeed = 6f * 32f / 60f;
        private const int CannonballDamage = 22;
        private const float CannonballSpeed = 14f * 32f / 60f;

        private IEnumerable<int> CirclingAttack()
        {
            while (true)
            {
                if (currentPhase != Phase.Kiting && !Invulnerable)
                {
                    if (circlingCooldownRemaining <= 0)
                    {
                        circlingCooldownRemaining = CirclingCooldown;
                        Vector2 aim = Player.Instance.Position - Position;
                        if (aim.LengthSquared() > 0)
                        {
                            if (nextIsCannonball)
                                FireAt(aim, CannonballSpeed, CannonballDamage, Art.PirateCannonBullet);
                            else
                                FireAt(aim, CutlassSpeed, CutlassDamage, Art.PirateKingSword);
                            nextIsCannonball = !nextIsCannonball;
                        }
                    }

                    if (circlingCooldownRemaining > 0)
                        circlingCooldownRemaining--;
                }

                yield return 0;
            }
        }

        private void FireAt(Vector2 aim, float speed, int damage, Texture2D image = null)
        {
            EntityManager.Add(
                new EnemyProjectile(Position, aim.ScaleTo(speed), image) { Damage = damage }
            );
        }

        // Phases 2-3 only: "occasionally... flash red... Armoring himself
        // and firing three larger cannonballs." Phase 4 is permanently
        // Armored instead (set once in PhaseWatcher() above), so this never
        // runs there.
        private int armorBurstCooldownRemaining = ArmorBurstCooldown;
        private const int ArmorBurstCooldown = 400;
        private const int ArmorBurstDuration = 150;
        private const float ArmorMultiplier = 1.5f;
        private int armorBurstDurationRemaining = 0;

        private IEnumerable<int> ArmorBurst()
        {
            while (true)
            {
                if (
                    (currentPhase == Phase.Circling || currentPhase == Phase.ShipCannons)
                    && !Invulnerable
                )
                {
                    if (armorBurstDurationRemaining > 0)
                    {
                        armorBurstDurationRemaining--;
                        if (armorBurstDurationRemaining == 0)
                            Defense = baseDefense;
                    }
                    else if (armorBurstCooldownRemaining <= 0)
                    {
                        armorBurstCooldownRemaining = ArmorBurstCooldown;
                        armorBurstDurationRemaining = ArmorBurstDuration;
                        FlashRed();
                        Defense = (int)(baseDefense * ArmorMultiplier);

                        Vector2 aim = Player.Instance.Position - Position;
                        if (aim.LengthSquared() > 0)
                        {
                            float baseAngle = aim.ToAngle();
                            for (int i = -1; i <= 1; i++)
                                FireAt(
                                    Extensions.FromPolar(baseAngle + i * 0.15f, 1f),
                                    CannonballSpeed,
                                    CannonballDamage,
                                    Art.PirateCannonBullet
                                );
                        }
                    }
                    else
                    {
                        armorBurstCooldownRemaining--;
                    }
                }

                yield return 0;
            }
        }

        // Phases 3-4: "the four cannons near the front... and the two near
        // the back will start firing large cannonballs down the ship's
        // length" — fixed lane positions relative to roomCenter, each
        // firing straight outward. Simplified from 6 exact positions to 4
        // representative lanes.
        private static readonly Vector2[] CannonLaneOffsets =
        {
            new(-220, -80),
            new(-220, 80),
            new(220, -80),
            new(220, 80),
        };

        private int shipCannonCooldownRemaining = 0;
        private const int ShipCannonCooldown = 90;
        private const int ShipCannonDamage = 25;
        private const float ShipCannonSpeed = 6f * 32f / 60f;

        private IEnumerable<int> ShipCannons()
        {
            while (true)
            {
                if (
                    (currentPhase == Phase.ShipCannons || currentPhase == Phase.Armored)
                    && !Invulnerable
                )
                {
                    if (shipCannonCooldownRemaining <= 0)
                    {
                        shipCannonCooldownRemaining = ShipCannonCooldown;
                        foreach (Vector2 offset in CannonLaneOffsets)
                        {
                            Vector2 cannonPos = roomCenter + offset;
                            Vector2 direction = offset.LengthSquared() > 0 ? offset : Vector2.UnitX;
                            EntityManager.Add(
                                new EnemyProjectile(
                                    cannonPos,
                                    direction.ScaleTo(ShipCannonSpeed),
                                    Art.PirateCannonBullet
                                )
                                {
                                    Damage = ShipCannonDamage,
                                }
                            );
                        }
                    }

                    if (shipCannonCooldownRemaining > 0)
                        shipCannonCooldownRemaining--;
                }

                yield return 0;
            }
        }

        // Phase 4 only: "releases several larger fast cannonballs alongside
        // spreads of smaller ones."
        private int bigBurstCooldownRemaining = BigBurstCooldown;
        private const int BigBurstCooldown = 300;
        private const int BigBurstDamage = 15;
        private const float BigBurstSpeed = 7.5f * 32f / 60f;
        private const int BigBurstShots = 5;
        private const float BigBurstSpread = 0.2f;

        private IEnumerable<int> BigBursts()
        {
            while (true)
            {
                if (currentPhase == Phase.Armored && !Invulnerable)
                {
                    if (bigBurstCooldownRemaining <= 0)
                    {
                        bigBurstCooldownRemaining = BigBurstCooldown;
                        FlashRed();

                        Vector2 aim = Player.Instance.Position - Position;
                        if (aim.LengthSquared() > 0)
                        {
                            float baseAngle = aim.ToAngle();
                            float centerOffset = (BigBurstShots - 1) / 2f;
                            for (int i = 0; i < BigBurstShots; i++)
                            {
                                float angle = baseAngle + (i - centerOffset) * BigBurstSpread;
                                FireAt(
                                    Extensions.FromPolar(angle, 1f),
                                    BigBurstSpeed,
                                    BigBurstDamage,
                                    Art.PirateCannonBullet
                                );
                            }
                        }
                    }

                    if (bigBurstCooldownRemaining > 0)
                        bigBurstCooldownRemaining--;
                }

                yield return 0;
            }
        }
    }
}
