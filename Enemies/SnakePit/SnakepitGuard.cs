using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Realm
{
    // The Snake Pit Treasure Room's own mini-boss (realmeye.com/wiki/
    // snakepit-guard) — spawned by TreasureRoomController.cs at the room's
    // center once its button triggers. A plain Enemy subclass, not Boss:
    // it fights inline in the regular dungeon with its own small floating
    // health bar like any other enemy, not a dedicated BossRealmState HUD.
    // Health-threshold 2-phase design (Enemy.HealthFraction), same
    // PhaseWatcher() shape LimonTheSpriteGoddess/DreadstumpThePirateKing
    // already use, health-gated rather than time-gated since the wiki's
    // own phase description ("after losing enough HP") reads as
    // progression-driven. HP/DEF/PV are the wiki's own numbers as-is, same
    // "use the real number" precedent every other boss this session
    // followed. A first pass built from real wiki numbers, not yet
    // playtested — flagged in docs/BACKLOG.md.
    class SnakepitGuard : Enemy
    {
        private static readonly Random rand = new();

        private enum Phase
        {
            Phase1,
            Phase2,
        }

        private Phase currentPhase = Phase.Phase1;
        private const float Phase2Threshold = 0.5f;
        private const int TransitionFrames = 60; // ~1s — the invulnerable window

        // Phase 2's "moves back and forth along the room's length" —
        // computed once from the Treasure Room's own world-space bounds
        // (handed in by TreasureRoomController.cs), oscillating along
        // whichever axis is longer rather than assuming a fixed
        // orientation. A simplification from the wiki's own distinctly-
        // shaped "long room" (see DungeonTypeData.TreasureRoomChance's own
        // doc comment) — this room is a normal generated rectangle, so
        // "the room's length" is just whichever of its own two axes is
        // longer.
        private readonly Vector2 oscillationStart;
        private readonly Vector2 oscillationEnd;
        private bool movingToEnd = true;
        private const float OscillationSpeed = 1.2f;
        private const float OscillationMargin = 48f; // stay clear of the walls

        public SnakepitGuard(Vector2 position, Rectangle roomBoundsWorld)
            : base(Art.SnakepitGuard, position)
        {
            health = 7500;
            healthMax = 7500;
            Defense = 20;
            PointValue = 2000;

            // No dedicated Snakepit Guard audio yet — reuses the shared
            // default, same placeholder-audio status as Cube God/
            // Dreadstump.
            deathSound = Sound.DefaultHit;
            hitSound = Sound.DefaultHit;

            // A real mini-boss drop table (Tier 7-9 weapons, Tier 6-8
            // armor per the wiki's own drop table). Ring/AbilityItem tiers
            // aren't called out by the wiki explicitly; 4-6 is a
            // reasonable mini-boss-tier estimate, not a wiki number.
            // StatPotion is in the pool specifically for the guaranteed
            // Speed potion below — the wiki's own drop table lists a
            // "Potion of Speed," this engine's closest equivalent.
            DropPool =
                ItemSpawner.LootCategory.Weapon
                | ItemSpawner.LootCategory.Armor
                | ItemSpawner.LootCategory.Ring
                | ItemSpawner.LootCategory.AbilityItem
                | ItemSpawner.LootCategory.StatPotion;
            DropTierRanges = new()
            {
                [ItemSpawner.LootCategory.Weapon] = (7, 9),
                [ItemSpawner.LootCategory.Armor] = (6, 8),
                [ItemSpawner.LootCategory.Ring] = (4, 6),
                [ItemSpawner.LootCategory.AbilityItem] = (4, 6),
            };

            // Guarantees the Speed potion every kill (100% — same shape
            // Stheno/CubeGod already use for their own signature guaranteed
            // potions), rather than a single random pick among all 8 stat
            // types the way an unset GuaranteedPotionChances would roll.
            GuaranteedPotionChances = new() { [Potions.Speed] = 1.0f };

            // Snake Eye Ring — the game's first UT item (see Equipment.
            // IsUntiered's own doc comment) — independently of the
            // guaranteed loot above, matching the wiki's own listed drop
            // sources (Stheno the Snake Queen and Snakepit Guard both drop
            // it). 2% is an estimate, same as Stheno's own identical entry.
            UniqueItemDropChances = new() { ["Snake Eye Ring"] = 0.02f };

            if (roomBoundsWorld.Width >= roomBoundsWorld.Height)
            {
                oscillationStart = new Vector2(roomBoundsWorld.Left + OscillationMargin, position.Y);
                oscillationEnd = new Vector2(roomBoundsWorld.Right - OscillationMargin, position.Y);
            }
            else
            {
                oscillationStart = new Vector2(position.X, roomBoundsWorld.Top + OscillationMargin);
                oscillationEnd = new Vector2(position.X, roomBoundsWorld.Bottom - OscillationMargin);
            }

            AddBehaviour(PhaseWatcher());
            AddBehaviour(Phase1Movement());
            AddBehaviour(Phase2Movement());
            AddAttackBehaviour(SnakeSpit());
            AddAttackBehaviour(SnakeSpinners());
            AddAttackBehaviour(SnakeBalls());
            AddAttackBehaviour(GrenadePairs());
            AddAttackBehaviour(Phase2SnakeBalls());
            AddAttackBehaviour(Phase2Grenades());
        }

        // Overrides Enemy's own default (the chance-based Spawn() table,
        // which requires a literal DropChances entry per category to roll
        // at all) with the same guaranteed-loot path every real boss in
        // this game uses (Boss.SpawnLoot() -> ItemSpawner.
        // SpawnGuaranteedLoot()) — every category in DropPool above always
        // contributes an item, and GuaranteedPotionChances' Speed entry
        // always fires. Without this override, SnakepitGuard had no
        // DropChances set at all and was dropping nothing on death.
        protected override void SpawnLoot(List<Item> extraItems = null)
        {
            ItemSpawner.SpawnGuaranteedLoot(
                Position,
                PointValue,
                DropPool,
                DropTierRanges,
                StatPotionPool,
                GuaranteedPotionChances,
                extraItems
            );
        }

        // Every boss/mini-boss with health-threshold phases in this
        // codebase re-checks after every transition so a single big hit
        // crossing more than one threshold still visits each phase in
        // order — only meaningful here once a 3rd+ phase exists, kept for
        // consistency with LimonTheSpriteGoddess/DreadstumpThePirateKing's
        // own shape anyway.
        private IEnumerable<int> PhaseWatcher()
        {
            while (true)
            {
                Phase target = HealthFraction <= Phase2Threshold ? Phase.Phase2 : Phase.Phase1;

                if (target != currentPhase)
                {
                    FlashRed();
                    Invulnerable = true;

                    for (int i = 0; i < TransitionFrames; i++)
                        yield return 0;

                    currentPhase = target;
                    Invulnerable = false;
                }

                yield return 0;
            }
        }

        // Phase 1: "chases players at a slow pace."
        private IEnumerable<int> Phase1Movement()
        {
            while (true)
            {
                if (currentPhase == Phase.Phase1 && !Invulnerable)
                {
                    Vector2 toPlayer = Player.Instance.Position - Position;
                    if (toPlayer != Vector2.Zero)
                        Velocity += toPlayer.ScaleTo(0.1f);
                }

                yield return 0;
            }
        }

        // Phase 2: "moves back and forth along the room's length" —
        // oscillates between the two endpoints computed in the
        // constructor, reversing whenever it gets close to whichever end
        // it's currently heading toward.
        private IEnumerable<int> Phase2Movement()
        {
            while (true)
            {
                if (currentPhase == Phase.Phase2 && !Invulnerable)
                {
                    Vector2 target = movingToEnd ? oscillationEnd : oscillationStart;
                    Vector2 toTarget = target - Position;
                    if (toTarget.LengthSquared() < 32f * 32f)
                        movingToEnd = !movingToEnd;
                    else
                        Velocity += toTarget.ScaleTo(OscillationSpeed);
                }

                yield return 0;
            }
        }

        // "Firing 3-shot spreads of Snake Spit" (phase 1) / "Snake Spit to
        // its side" (phase 2, approximated as the same 3-shot spread —
        // "to its side" isn't a materially different attack shape here).
        private const int SnakeSpitCooldown = 100;
        private const int SnakeSpitDamage = 65;
        private const float SnakeSpitSpeed = 6f * 32f / 60f;
        private const float SnakeSpitRange = 12f * 32f;

        private IEnumerable<int> SnakeSpit()
        {
            var fanShot = FanShot(
                range: SnakeSpitRange,
                damage: SnakeSpitDamage,
                projectileSpeed: SnakeSpitSpeed,
                shots: 3,
                angleStep: 0.3f,
                cooldownFrames: SnakeSpitCooldown,
                projectileImage: Art.SnakeBite
            ).GetEnumerator();

            while (true)
            {
                if (!Invulnerable)
                    fanShot.MoveNext();
                yield return 0;
            }
        }

        // "Rings of Snake Spinners that daze" (phase 1) — a full-circle
        // Bomb() burst with DazesOnHit, same "ring of bullets" stand-in for
        // a nova attack this session's other Snake Pit enemies already use.
        private const int SpinnerCooldown = 220;
        private const int SpinnerDamage = 55;
        private const float SpinnerSpeed = 5f * 32f / 60f;
        private const int SpinnerDazeFrames = 120; // 2s

        private IEnumerable<int> SnakeSpinners()
        {
            var bomb = Bomb(
                projectileSpeed: SpinnerSpeed,
                damage: SpinnerDamage,
                dazesOnHit: true,
                dazeDurationFrames: SpinnerDazeFrames,
                projectileImage: Art.SnakeBite
            ).GetEnumerator();

            while (true)
            {
                if (currentPhase == Phase.Phase1 && !Invulnerable)
                    bomb.MoveNext();
                yield return 0;
            }
        }

        // "3-directional Snake Balls that inflict heavy damage and Daze,
        // with one of them aimed at the nearest player" (phase 1) —
        // FanShot's own aim-at-player center shot plus two more 120
        // degrees apart approximates "3-directional, one aimed at the
        // player" without needing a bespoke fixed-direction attack.
        private const int SnakeBallCooldown = 300;
        private const int SnakeBallDamage = 100;
        private const float SnakeBallSpeed = 3.5f * 32f / 60f;
        private const float SnakeBallRange = 10.5f * 32f;
        private const int SnakeBallDazeFrames = 240; // 4s

        private IEnumerable<int> SnakeBalls()
        {
            var fanShot = FanShot(
                range: SnakeBallRange,
                damage: SnakeBallDamage,
                projectileSpeed: SnakeBallSpeed,
                shots: 3,
                angleStep: MathHelper.TwoPi / 3f,
                cooldownFrames: SnakeBallCooldown,
                dazesOnHit: true,
                dazeDurationFrames: SnakeBallDazeFrames,
                projectileImage: Art.SnakeBite
            ).GetEnumerator();

            while (true)
            {
                if (currentPhase == Phase.Phase1 && !Invulnerable)
                    fanShot.MoveNext();
                yield return 0;
            }
        }

        // "Throwing pairs of red grenades that deal area damage on
        // impact" (phase 1) — two ThrowGrenades calls fired together each
        // time the shared cooldown allows, both aimed at the player.
        private const int GrenadeCooldown = 200;
        private const int GrenadeDamage = 70;
        private const float GrenadeRadius = 1.5f * 32f;

        private IEnumerable<int> GrenadePairs()
        {
            var first = ThrowGrenades(
                damage: GrenadeDamage,
                radius: GrenadeRadius,
                cooldownFrames: GrenadeCooldown,
                targetPosition: () => Player.Instance.Position
            ).GetEnumerator();
            var second = ThrowGrenades(
                damage: GrenadeDamage,
                radius: GrenadeRadius,
                cooldownFrames: GrenadeCooldown,
                targetPosition: () => Player.Instance.Position + new Vector2(32, 0)
            ).GetEnumerator();

            while (true)
            {
                if (currentPhase == Phase.Phase1 && !Invulnerable)
                {
                    first.MoveNext();
                    second.MoveNext();
                }
                yield return 0;
            }
        }

        // Phase 2: "firing Snake Balls in four directions" — same FanShot
        // shape as phase 1's own Snake Balls, just an even 4-way spread
        // instead of 3, and gated to phase 2 only.
        private const int Phase2SnakeBallCooldown = 240;

        private IEnumerable<int> Phase2SnakeBalls()
        {
            var fanShot = FanShot(
                range: SnakeBallRange,
                damage: SnakeBallDamage,
                projectileSpeed: SnakeBallSpeed,
                shots: 4,
                angleStep: MathHelper.PiOver2,
                cooldownFrames: Phase2SnakeBallCooldown,
                dazesOnHit: true,
                dazeDurationFrames: SnakeBallDazeFrames,
                projectileImage: Art.SnakeBite
            ).GetEnumerator();

            while (true)
            {
                if (currentPhase == Phase.Phase2 && !Invulnerable)
                    fanShot.MoveNext();
                yield return 0;
            }
        }

        // Phase 2: "constantly throwing red AoE bombs along its path" —
        // ThrowGrenades targeting its own current position, approximating
        // a trail of hazards as it oscillates.
        private const int Phase2GrenadeCooldown = 50;

        private IEnumerable<int> Phase2Grenades()
        {
            var grenades = ThrowGrenades(
                damage: GrenadeDamage,
                radius: GrenadeRadius,
                cooldownFrames: Phase2GrenadeCooldown,
                targetPosition: () => Position
            ).GetEnumerator();

            while (true)
            {
                if (currentPhase == Phase.Phase2 && !Invulnerable)
                    grenades.MoveNext();
                yield return 0;
            }
        }
    }
}
