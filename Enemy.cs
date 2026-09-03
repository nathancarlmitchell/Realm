using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Realm.Particles;
using Realm.Projectiles;
using Realm.States;

namespace Realm
{
    class Enemy : Entity
    {
        private static readonly Random rand = new();
        private int timeUntilStart = 60;
        public bool IsActive
        {
            get { return timeUntilStart <= 0; }
        }

        // protected set: a subclass (e.g. Boss/LimonTheSpriteGoddess) sets
        // these directly in its own constructor, same as Wizard/Archer/
        // Knight already do with baseHealth/baseMana/etc.
        public int PointValue { get; protected set; }

        // Flat reduction applied to incoming damage in WasShot() below,
        // floored at 0 (can't turn a hit into healing). Defaults to 0 for
        // every enemy unless a factory/subclass sets it explicitly.
        public int Defense { get; protected set; } = 0;

        // What fraction of the XP needed for the player's next level this
        // enemy's PointValue can be capped to (see WasShot() below) — 0.1
        // (10%) for every normal enemy. The spec calls out a 20% cap for
        // "quest monsters," but no such concept exists anywhere in this
        // codebase yet (every enemy today is authored the same way) — left
        // as an overridable field rather than building a whole quest-monster
        // system just to set one number.
        protected float NextLevelXpCapFraction = 0.1f;

        private List<IEnumerator<int>> behaviours = new List<IEnumerator<int>>();
        private List<IEnumerator<int>> attackBehaviours = new List<IEnumerator<int>>();

        // protected: a subclass constructor sets these directly to
        // establish its stats, same as every factory method below already
        // does via object-initializer syntax from within Enemy itself.
        protected int health;
        protected int healthMax;

        // Fraction of max health remaining, 0-1 — a convenience shorthand
        // for phase-transition logic (e.g. LimonTheSpriteGoddess) so
        // callers don't have to repeat the healthMax > 0 guard by hand.
        protected float HealthFraction => healthMax > 0 ? (float)health / healthMax : 0f;

        // Called exactly once by EntityManager.AddEntity() the moment this
        // enemy is actually added to the live game world — scales
        // health/healthMax by Difficulty.EnemyHealthMultiplier. A method
        // rather than baking the multiplier into every individual factory/
        // boss constructor's own health/healthMax assignment, so retuning
        // the global knob doesn't require touching per-enemy code —
        // matches Difficulty.EnemyDamageMultiplier/EnemyChaseSpeedMultiplier's
        // own "one knob, one injection site" shape. Public (not protected)
        // since EntityManager, not a subclass, is what calls it.
        public void ApplyHealthDifficultyScaling()
        {
            health = (int)(health * Difficulty.EnemyHealthMultiplier);
            healthMax = (int)(healthMax * Difficulty.EnemyHealthMultiplier);
        }

        protected SoundEffect deathSound;
        protected SoundEffect hitSound;
        public List<Guid> HitBy;

        // Which boss arena's portal (if any) this enemy drops on death, on
        // top of its normal loot roll — null for every enemy except the
        // specific ones a factory below wires up (CreateSpriteGod(),
        // CreateBigSnake()). One field instead of a bool per boss, since a
        // second real instance of "enemy X drops boss Y's portal" showed up
        // (mirrors this session's Portal.Destination enum->class cleanup).
        protected Portal.Destination portalDropOnDeath;

        // Independent per-portal chance (0.0-1.0) that death additionally
        // drops a portal to some destination — same "N separate rolls, one
        // per entry" shape as GuaranteedPotionChances below, just for
        // portals instead of stat potions, and a *chance* rather than
        // portalDropOnDeath's guaranteed drop. Empty by default (no enemy
        // drops a portal unless a factory/subclass sets this); first real
        // use: BeachPortalDropChances below, a 1% Pirate Cave portal chance
        // shared by every Beach enemy.
        protected Dictionary<Portal.Destination, float> PortalDropChances = new();

        // Invulnerable enemies take zero damage from WasShot() below — a
        // true no-op, not just reduced damage. Used by boss phase-transition
        // windows (e.g. Stheno between phases); false (damageable) for every
        // enemy unless a subclass explicitly sets it.
        protected bool Invulnerable;

        // Paralyzes this enemy, blocking its movement (Update() below) for
        // durationFrames. Backed by Entity's general debuff system (refreshes
        // on re-trigger rather than stacking, ticked/drawn via
        // UpdateDebuffs()/DrawDebuffIndicators() below). Default is 3 seconds
        // at 60fps.
        public void Paralyze(int durationFrames = 180)
        {
            ApplyDebuff(DebuffType.Paralyzed, durationFrames);
        }

        // Stuns this enemy, blocking its attacks (Update() below) for
        // durationFrames — movement is unaffected, unlike Paralyze() above.
        // Default is 3 seconds at 60fps.
        public void Stun(int durationFrames = 180)
        {
            ApplyDebuff(DebuffType.Stunned, durationFrames);
        }

        // Makes this enemy Vulnerable, increasing all damage it takes
        // (WasShot() below) by VulnerableDamageMultiplier for durationFrames
        // — unlike Paralyze()/Stun() above, this doesn't block movement or
        // attacks at all. No icon (see DebuffIcon()'s own comment) — per
        // direct request, this debuff has no visual indicator. Default is 3
        // seconds at 60fps, matching the real wiki's own "receive 110%
        // damage for 3 seconds after being hit."
        public void Vulnerable(int durationFrames = 180)
        {
            ApplyDebuff(DebuffType.Vulnerable, durationFrames);
        }

        // "Targets receive 110% damage... after being hit" — a flat +10%
        // multiplier, applied in WasShot() below.
        private const float VulnerableDamageMultiplier = 1.1f;

        // Permanent tint applied to this enemy's sprite, multiplied into
        // the same fade-in alpha every enemy already gets on spawn (see
        // Update() below) — lets a factory reuse an existing texture as a
        // visually distinct variant (a color-swapped "tougher cousin") with
        // its own stats/behaviors, without needing new art. White (the
        // default) means no change from today's plain sprite.
        protected Color tint = Color.White;

        // Brief red blink-flash cue, meant for a boss announcing a phase
        // transition (e.g. entering an "enraged" phase — see
        // LimonTheSpriteGoddess.PhaseWatcher()) — call FlashRed() once at
        // the moment the transition happens. Ticked in Update(), applied in
        // Draw() below as a one-frame color swap rather than mutating
        // `color` itself, since that field also carries the spawn fade-in
        // alpha (see the constructor/Update() below) and permanently
        // overwriting it would clobber that.
        private int blinkTicksRemaining;
        private int blinkPeriodFrames;
        private bool blinkOn;

        // blinkCount full on/off cycles, each half lasting periodFrames —
        // e.g. the defaults (3, 8) blink on/off 3 times over 48 frames
        // (0.8s at 60fps).
        protected void FlashRed(int blinkCount = 3, int periodFrames = 8)
        {
            blinkTicksRemaining = blinkCount * 2 * periodFrames;
            blinkPeriodFrames = periodFrames;
        }

        public Enemy(Texture2D image, Vector2 position)
        {
            this.image = image;
            Position = position;
            Radius = image.Width / 2f;
            color = Color.Transparent;

            deathSound = Sound.DefaultHit;
            hitSound = Sound.DefaultHit;

            HitBy = [];
        }

        public override void Update()
        {
            UpdateDebuffs();

            if (blinkTicksRemaining > 0)
            {
                blinkTicksRemaining--;
                blinkOn = (blinkTicksRemaining / blinkPeriodFrames) % 2 == 0;
            }
            else
            {
                blinkOn = false;
            }

            if (timeUntilStart <= 0)
            {
                ApplyBehaviours();

                // Attack if on screen, or just slightly off it. Stunned
                // blocks this (Paralyzed doesn't — it only blocks movement
                // below). Rogue's Cloak (see Player.IsInvisible) blocks it
                // too — "many enemies will stop shooting entirely if they
                // can't see any players" — a single centralized check here
                // covers every enemy/boss in the game, since every attack
                // funnels through this one call.
                if (
                    !HasDebuff(DebuffType.Stunned)
                    && !Player.Instance.IsInvisible
                    && Game1.GetWorldBounds(1.25f).Contains(Position.ToPoint())
                )
                {
                    ApplyAttackBehaviours();
                }
            }
            else
            {
                timeUntilStart--;
                color = tint * (1 - timeUntilStart / 60f);
            }

            // Paralyzed enemies keep "thinking" (behaviours above still run
            // and accumulate into Velocity), they just don't act on it —
            // Velocity still decays at its normal rate below, so there's no
            // backlog dump of pent-up motion the instant the paralysis ends.
            // Stunned doesn't block movement — only attacks, above.
            if (!HasDebuff(DebuffType.Paralyzed))
            {
                Position += Velocity;
            }

            Velocity *= 0.8f;

            // Despawn enemies that get too far away.
            if (Vector2.DistanceSquared(Position, Player.Instance.Position) > 25000000)
            {
                Debug.WriteLine(
                    "Despawn distance: "
                        + Vector2.DistanceSquared(Position, Player.Instance.Position)
                );
                IsExpired = true;
            }
        }

        // virtual: Boss overrides this to a no-op, since a boss's health is
        // shown by the dedicated top-of-screen bar (BossRealmState.
        // DrawBossHud()) instead — drawing both would be redundant.
        public virtual void DrawHealthBars(SpriteBatch spriteBatch)
        {
            if (health < healthMax)
            {
                // Position is scaled by drawScale (Entity.Draw() draws the
                // sprite that much bigger around the same center) — without
                // matching it here, a scaled-up enemy's actual on-screen
                // edges extend past where this assumes, so the bar ends up
                // underneath the sprite instead of below it.
                float x = Position.X - (this.image.Width * drawScale / 4);
                float y = Position.Y + (this.image.Height * drawScale / 2);

                int barScale = 1;
                int barHeight = 8;

                Vector2 healthBarPos = new(x, y);

                // Normalize values.
                int normalisedHealth = (health * 25 / healthMax * 25) / 25;

                // Health bars.
                Rectangle greenRect = new(0, 0, normalisedHealth * barScale, barHeight);
                Rectangle redRect = new(0, 0, 25 * barScale, barHeight);

                // Red bar.
                spriteBatch.Draw(
                    Art.HealthBar,
                    healthBarPos,
                    redRect,
                    Color.DarkRed,
                    0f,
                    Vector2.Zero * 0.25f,
                    1f,
                    0,
                    0
                );

                // Green bar.
                spriteBatch.Draw(
                    Art.HealthBar,
                    healthBarPos,
                    greenRect,
                    Color.DarkGreen,
                    0f,
                    Vector2.Zero,
                    1f,
                    0,
                    0
                );
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawHealthBars(spriteBatch);

            if (blinkOn)
            {
                // One-frame color swap rather than overwriting `color`
                // itself — `color` also carries the spawn fade-in alpha
                // (see Update() above), so mutating it here would clobber
                // that instead of just layering a flash on top of it.
                Color original = color;
                color = Color.Red;
                base.Draw(spriteBatch);
                color = original;
            }
            else
            {
                base.Draw(spriteBatch);
            }

            DrawDebuffIndicators(spriteBatch);
        }

        public void WasShot(int damage, bool ignoresDefense = false)
        {
            if (Invulnerable)
                return;

            Debug.WriteLine(damage);

            // Vulnerable (see Vulnerable() above) — applied to the raw hit,
            // before Defense's own reduction below, same layering
            // Player.Hit()'s own DamageTakenMultiplier uses.
            if (HasDebuff(DebuffType.Vulnerable))
                damage = (int)(damage * VulnerableDamageMultiplier);

            // Defense reduces damage 1-for-1 but caps at 90% reduction — a
            // shot always deals at least 10% of its raw damage, matching
            // Player.Hit()'s own damage/10 floor for the reverse direction
            // (the player's own Defense reducing incoming enemy damage).
            // Previously only floored at 0, so high enough Defense could
            // block an attack entirely instead of guaranteeing that 10%.
            int actualDamage = ignoresDefense
                ? Math.Max(0, damage)
                : Math.Max(damage - Defense, damage / 10);
            health -= actualDamage;

            if (Player.Instance.ShowEnemyDamageNumbersEnabled && actualDamage != 0)
                EntityManager.Add(new DamageNumber(Position, actualDamage, Color.Red, prefix: "-"));
            if (Player.Instance.ShowHitParticlesEnabled)
                Particle.SpawnBurst(
                    Position,
                    Color.White,
                    count: 5,
                    minSpeed: 1.5f,
                    maxSpeed: 3f,
                    lifespanTicks: 15
                );

            if (health <= 0)
            {
                Sound.Play(deathSound, 0.4f);
                IsExpired = true;
                // PointValue is this enemy's own specified base XP (the
                // spec's "base XP value, a parameter found in the game
                // XML") — capped at NextLevelXpCapFraction of the XP needed
                // for the player's next level before any multiplier is
                // applied, matching the source spec's own worked example
                // (a low-level player killing a high-value enemy gets only
                // a fraction of it; a multiplier can still push the final
                // total back above what the cap alone would allow). Removed
                // once the player hits Level 20, per direct request — there
                // is no real "next level" to cap progress toward anymore at
                // that point (Level 21 is never reachable), and the cap was
                // otherwise throttling Base Fame/Class Quest progress (both
                // driven by ExperienceTotal past 20) down to a small
                // fraction of an enemy's real PointValue for no remaining
                // balance reason.
                int cappedBaseXp;
                if (Player.Instance.Level < 20)
                {
                    int xpNeededForNextLevel =
                        Player.Instance.ExperienceNextLevel
                        - Player.CumulativeExperienceForLevel(Player.Instance.Level);
                    cappedBaseXp = Math.Min(
                        PointValue,
                        (int)(xpNeededForNextLevel * NextLevelXpCapFraction)
                    );
                }
                else
                {
                    cappedBaseXp = PointValue;
                }
                // Scaled by any equipped XP-bonus gear (e.g. a Priest's
                // Tome) — 0% bonus (the default for everything else) leaves
                // this an exact no-op multiply-by-1.
                int xpGained = (int)(
                    cappedBaseXp * (1f + Player.Instance.EquipmentXpBonusPercent / 100f)
                );
                Player.Instance.ExperienceTotal += xpGained;
                // Above the player's own head, not the enemy's — an XP gain
                // is the player's feedback, not a mark on what just died.
                // Goldenrod matches the sidebar's XP bar fill color
                // (Overlay.DrawExperience()), so it reads as "that" resource.
                // Bigger, longer-lived, and spawned further above the
                // player's head than a plain damage number (-45 vs the
                // default -20) so it reads as a distinct, more prominent
                // event rather than blending in with hit numbers.
                // ExperienceTotal above is unconditional either way, only
                // the floating number's visibility is gated — below Level
                // 20 by ShowXpDropsEnabled (Settings > Graphics, on by
                // default), and from Level 20 onward by AlwaysShowExpEnabled
                // instead (off by default — "the icon... disappears" at 20,
                // reactivated by that separate setting).
                bool showXpIcon =
                    Player.Instance.Level < 20
                        ? Player.Instance.ShowXpDropsEnabled
                        : Player.Instance.AlwaysShowExpEnabled;
                if (showXpIcon && xpGained != 0)
                {
                    EntityManager.Add(
                        new DamageNumber(
                            Player.Instance.Position,
                            xpGained,
                            Color.Goldenrod,
                            prefix: "+",
                            suffix: "XP",
                            scale: 1.3f,
                            lifespanTicks: 70,
                            verticalOffset: -45f,
                            followsPlayer: true
                        )
                    );
                }
                if (Player.Instance.ShowHitParticlesEnabled)
                    Particle.SpawnBurst(
                        Position,
                        Color.OrangeRed,
                        count: 14,
                        minSpeed: 2f,
                        maxSpeed: 5f,
                        lifespanTicks: 25,
                        startScale: 0.2f
                    );

                // Spawn loot — SpawnLoot() is virtual, so Boss subclasses
                // (guaranteed good loot) override this; every other enemy
                // uses the base implementation's normal random-chance table.
                // Gated on DropsLoot so a specific enemy (e.g. a boss's
                // non-loot-dropping pet/add) can opt out entirely, without
                // affecting anything else.
                if (DropsLoot)
                    SpawnLoot();

                // Some enemies additionally drop a portal into a boss arena,
                // on top of their normal loot above.
                if (portalDropOnDeath != null)
                {
                    Portal.DroppedPortals.Add(new Portal(this.Position, portalDropOnDeath));
                    Sound.Play(Sound.LootAppears, 0.4f);
                }

                // Chance-based portal drops (PortalDropChances above) — each
                // entry rolled independently, so more than one could drop on
                // the same kill. Separate from portalDropOnDeath above
                // (guaranteed, one destination) and from SpawnLoot()'s own
                // ItemSpawner-driven table (regular gear/potions) — this is
                // its own roll for a Portal object instead.
                foreach (var (destination, chance) in PortalDropChances)
                {
                    if (rand.NextDouble() < chance)
                    {
                        Portal.DroppedPortals.Add(new Portal(this.Position, destination));
                        Sound.Play(Sound.LootAppears, 0.4f);
                    }
                }
            }
            else
            {
                Sound.Play(hitSound, 0.5f);
            }
        }

        // Default: the normal random-chance drop table every enemy uses.
        // Boss overrides this for guaranteed good loot instead. PointValue
        // already ranks enemies by toughness (higher = more score for
        // killing it), so it doubles as the difficulty signal ItemSpawner
        // scales drop chance/tier off — no separate difficulty field needed.
        // Whether this even gets called at all is gated by DropsLoot below,
        // not by anything in here.
        protected virtual void SpawnLoot()
        {
            ItemSpawner.Spawn(
                this.Position,
                PointValue,
                DropPool,
                DropTierRanges,
                StatPotionPool,
                GuaranteedPotionChances,
                DropChances
            );
        }

        // Whether this enemy drops anything on death at all (SpawnLoot()
        // above only runs when true — see WasShot()). True for every enemy
        // by default; a specific enemy (e.g. a boss's pet/add, meant to be
        // a disposable obstacle rather than a source of loot) sets this
        // false in its own constructor.
        protected bool DropsLoot = true;

        // Which loot categories this enemy's drop table (SpawnLoot() above)
        // rolls against at all — the "real drop pool" backlog item's first
        // real lever. Defaults to every category, but a category only
        // actually rolls when DropChances below also has an explicit entry
        // for it (see ItemSpawner.RollsCategory) — this default alone no
        // longer produces any drops on its own. A specific factory below can
        // still narrow this to exclude categories that don't fit that
        // enemy's theme (e.g. CreateSnake() below drops gear only, no
        // potions) even when it does have DropChances entries for the rest.
        protected ItemSpawner.LootCategory DropPool = ItemSpawner.LootCategory.All;

        // Absolute tier range to roll a given category's dropped items
        // from, bypassing the PointValue/player-tier formula
        // (ResolveDropTier()) entirely for that category — the direct
        // "what tier of gear can this enemy drop" lever. Per-category
        // (e.g. Weapon at tier 7-10, Ring at tier 3-4 on the same enemy),
        // not one shared range — keyed by LootCategory, empty by default
        // (a category with no entry falls back to the PointValue-driven
        // formula, unchanged for any enemy that doesn't opt in for that
        // category — this only picks the *tier*, DropChances below still
        // decides whether the category rolls at all). Min/Max are
        // inclusive; Min must be <= Max, since a range where it isn't has no
        // valid roll.
        protected Dictionary<ItemSpawner.LootCategory, (int Min, int Max)> DropTierRanges = new();

        // Which specific stat potions (Attack/Defense/Dexterity/Life/
        // ManaMax/Speed/Vitality/Wisdom) a StatPotion drop can roll from —
        // narrows ItemSpawner.RollStatPotion()'s selection the same way
        // DropPool narrows categories, just one level deeper (inside the
        // category rather than across categories). Null/empty (the
        // default) rolls uniformly from all 8, today's unchanged behavior.
        // Has no effect on an enemy that sets GuaranteedPotionChances below
        // — that mechanism replaces the single-roll selection this narrows.
        protected List<Potions> StatPotionPool = null;

        // Independent per-potion drop chance (0.0-1.0), one entry per
        // specific stat potion type — a different shape from
        // StatPotionPool above: that's one roll picking one type out of an
        // allowed set (mutually exclusive), this is N separate rolls, one
        // per entry, each able to succeed independently — so a kill can
        // drop several of them at once (e.g. a guaranteed Dexterity potion
        // at 1.0 alongside an independent 25% chance at a Defense potion,
        // and both landing on the same kill is possible). Empty by default
        // (the existing single-roll StatPotionPool behavior, unaffected);
        // setting this on an enemy entirely replaces that enemy's normal
        // StatPotion roll rather than adding to it.
        protected Dictionary<Potions, float> GuaranteedPotionChances = new();

        // Absolute, literal drop chance (0.0-1.0) for a whole category —
        // Weapon/Armor/Ring/AbilityItem/StatPotion/HealthManaPotion. The
        // *only* way a category rolls at all (see ItemSpawner.
        // RollsCategory) — empty by default, meaning an enemy that doesn't
        // set an entry for a category simply never drops it, full stop, no
        // implicit PointValue-scaled fallback. Only affects Spawn() —
        // SpawnGuaranteedLoot's gear categories are already deterministic
        // (no chance roll to override) and it doesn't use HealthManaPotion
        // at all.
        protected Dictionary<ItemSpawner.LootCategory, float> DropChances = new();

        // Shared drop-rate override for every Beach-biome enemy (Pirate,
        // Bandit, Piratess, Sand Devil, their mini-boss/escort variants, and
        // the three Little Jellies) — one table instead of each enemy
        // duplicating it, so retuning Beach's loot only means editing this
        // once. No stat potions at all (StatPotion excluded from the pool
        // entirely — DropChances/DropTierRanges entries for a category that
        // never rolls would be meaningless); Weapon/Armor at 2.5% from tier
        // 1-3; Ring/AbilityItem at 1.25% from tier 1 only; HP/MP potions at
        // 2.5%. Halved from 5%/5%/2.5%/2.5%/5% per direct playtest feedback
        // — drop rates felt too high across the board, a flat "everything in
        // half" pass across the game's whole drop-chance formula at the
        // time (that formula has since been replaced entirely — see
        // ItemSpawner.RollsCategory — but these literal percentages weren't
        // retuned when that happened). static readonly since it's the same
        // table for every instance, not per-enemy state.
        protected static readonly ItemSpawner.LootCategory BeachDropPool =
            ItemSpawner.LootCategory.Weapon
            | ItemSpawner.LootCategory.Armor
            | ItemSpawner.LootCategory.Ring
            | ItemSpawner.LootCategory.AbilityItem
            | ItemSpawner.LootCategory.HealthManaPotion;

        protected static readonly Dictionary<ItemSpawner.LootCategory, float> BeachDropChances =
            new()
            {
                [ItemSpawner.LootCategory.Weapon] = 0.0125f,
                [ItemSpawner.LootCategory.Armor] = 0.0125f,
                [ItemSpawner.LootCategory.Ring] = 0.005f,
                [ItemSpawner.LootCategory.AbilityItem] = 0.005f,
                [ItemSpawner.LootCategory.HealthManaPotion] = 0.025f,
            };

        protected static readonly Dictionary<
            ItemSpawner.LootCategory,
            (int Min, int Max)
        > BeachDropTierRanges = new()
        {
            [ItemSpawner.LootCategory.Weapon] = (1, 3),
            [ItemSpawner.LootCategory.Armor] = (1, 3),
            [ItemSpawner.LootCategory.Ring] = (1, 1),
            [ItemSpawner.LootCategory.AbilityItem] = (1, 1),
        };

        // Same "one shared table, wired into every Beach enemy" shape as
        // BeachDropPool/BeachDropChances/BeachDropTierRanges above, for
        // PortalDropChances instead — first entry in what's meant to grow
        // into "certain enemies have a chance of dropping certain portals"
        // more generally; Beach -> Pirate Cave is just the first pairing.
        protected static readonly Dictionary<Portal.Destination, float> BeachPortalDropChances =
            new() { [Portal.Destination.PirateCaveDungeon] = 0.01f };

        // Shared drop table for every Pirate Cave enemy (the 13 regular
        // enemies below, and Dreadstump — see ItemSpawner.
        // SpawnGuaranteedSingleItem() for how the boss's own single-item
        // guarantee reuses this same pool/tier-range pair) — same "one
        // table, edit once to retune everything" shape as BeachDropPool/
        // BeachDropChances/BeachDropTierRanges above. Per direct request:
        // no stat potions at all; Weapon/Armor tier 2-3, Ring/AbilityItem
        // tier 1-2 (both narrower/lower than Beach's own 1-3/1-1); chance
        // percentages copied verbatim from BeachDropChances.
        protected static readonly ItemSpawner.LootCategory PirateCaveDropPool =
            ItemSpawner.LootCategory.Weapon
            | ItemSpawner.LootCategory.Armor
            | ItemSpawner.LootCategory.Ring
            | ItemSpawner.LootCategory.AbilityItem
            | ItemSpawner.LootCategory.HealthManaPotion;

        protected static readonly Dictionary<ItemSpawner.LootCategory, float> PirateCaveDropChances =
            new()
            {
                [ItemSpawner.LootCategory.Weapon] = 0.0125f,
                [ItemSpawner.LootCategory.Armor] = 0.0125f,
                [ItemSpawner.LootCategory.Ring] = 0.005f,
                [ItemSpawner.LootCategory.AbilityItem] = 0.005f,
                [ItemSpawner.LootCategory.HealthManaPotion] = 0.025f,
            };

        protected static readonly Dictionary<
            ItemSpawner.LootCategory,
            (int Min, int Max)
        > PirateCaveDropTierRanges = new()
        {
            [ItemSpawner.LootCategory.Weapon] = (2, 3),
            [ItemSpawner.LootCategory.Armor] = (2, 3),
            [ItemSpawner.LootCategory.Ring] = (1, 2),
            [ItemSpawner.LootCategory.AbilityItem] = (1, 2),
        };

        protected void AddBehaviour(IEnumerable<int> behaviour)
        {
            behaviours.Add(behaviour.GetEnumerator());
        }

        protected void AddAttackBehaviour(IEnumerable<int> behaviour)
        {
            attackBehaviours.Add(behaviour.GetEnumerator());
        }

        private void ApplyBehaviours()
        {
            for (int i = 0; i < behaviours.Count; i++)
            {
                if (!behaviours[i].MoveNext())
                    behaviours.RemoveAt(i--);
            }
        }

        private void ApplyAttackBehaviours()
        {
            for (int i = 0; i < attackBehaviours.Count; i++)
            {
                if (!attackBehaviours[i].MoveNext())
                    attackBehaviours.RemoveAt(i--);
            }
        }

        private int projectileCooldownRemaining = 0;
        private readonly int projectileCooldown = 250;

        private int healthCooldownRemaining = 0;
        private readonly int healthCooldown = 250;

        IEnumerable<int> RegenHealth(int amount = 1)
        {
            while (true)
            {
                if (healthCooldownRemaining <= 0)
                {
                    healthCooldownRemaining = healthCooldown - (1 * 1);

                    int heal = health;
                    heal += amount;

                    if (heal >= healthMax)
                    {
                        health = healthMax;
                    }
                    else
                    {
                        health += amount;
                    }
                }

                if (healthCooldownRemaining > 0)
                    healthCooldownRemaining--;

                yield return 0;
            }
        }

        #region Movement Behaviors

        // Radius (from the enemy's own Position) within which FollowPlayer()
        // below actually gives chase, instead of chasing from anywhere on
        // the map regardless of distance. Measured as the half-diagonal of
        // the visible gameplay viewport (GameplayViewportWidth/Height — the
        // sidebar-excluded play area, not the full window) — center-to-
        // corner, not center-to-edge, so nothing already visible anywhere
        // on screen fails to aggro — then padded 10% ("slightly larger than
        // the screen") so it also catches enemies just off-screen rather
        // than only ones already in view. AggroRadiusSquared avoids a sqrt
        // every tick for every chasing enemy (LengthSquared() vs it,
        // instead of Length() vs AggroRadius).
        private static readonly float AggroRadius =
            Vector2.Distance(
                Vector2.Zero,
                new Vector2(Game1.GameplayViewportWidth / 2f, Game1.GameplayViewportHeight / 2f)
            ) * 1.1f;
        private static readonly float AggroRadiusSquared = AggroRadius * AggroRadius;

        protected IEnumerable<int> FollowPlayer(float acceleration = 0.5f)
        {
            while (true)
            {
                // ScaleTo() divides by the vector's own Length() — a zero
                // vector (enemy and player at the exact same Position, down
                // to the float) would divide by zero and permanently poison
                // Velocity/Position with NaN from that tick on. Found via
                // BeachedBuccaneer.cs's scripted test spawning an enemy
                // directly on top of the player; not reachable through
                // normal movement/spawning, but a one-line guard costs
                // nothing for every other FollowPlayer() user (Seeker,
                // Brute, Limon) since it's a no-op unless the vector is
                // already exactly zero.
                Vector2 toPlayer = Player.Instance.Position - Position;

                // Rogue's Cloak (see Player.IsInvisible) — "do not follow
                // the player when invisible." Ambient/idle movement
                // (MoveRandomly/MoveSnake/MoveTethered to a non-player
                // anchor) is untouched; only this shared player-chasing
                // coroutine is gated, so pursuit stops without freezing
                // unrelated motion.
                if (
                    !Player.Instance.IsInvisible
                    && toPlayer != Vector2.Zero
                    && toPlayer.LengthSquared() <= AggroRadiusSquared
                )
                    Velocity += toPlayer.ScaleTo(
                        acceleration * Difficulty.EnemyChaseSpeedMultiplier
                    );
                yield return 0;
            }
        }

        // Mirror image of FollowPlayer() above — same zero-vector guard,
        // same shape, just accelerating away from the player instead of
        // toward them. First real use: BanditLeader.cs ("Runs away from
        // players when low on health").
        protected IEnumerable<int> FleePlayer(float acceleration = 0.5f)
        {
            while (true)
            {
                Vector2 awayFromPlayer = Position - Player.Instance.Position;
                if (awayFromPlayer != Vector2.Zero)
                    Velocity += awayFromPlayer.ScaleTo(acceleration);
                yield return 0;
            }
        }

        protected IEnumerable<int> MoveSnake(float speed = 0.2f)
        {
            float direction = rand.NextFloat(0, MathHelper.TwoPi);
            while (true)
            {
                direction += rand.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2);
                direction = MathHelper.WrapAngle(direction);
                for (int i = 0; i < 10; i++)
                {
                    Velocity += Extensions.FromPolar(direction, speed);
                    yield return 0;
                }
            }
        }

        // Same weaving randomness as MoveSnake above, but leashed to a
        // radius around wherever the enemy spawned. updateChance is a
        // per-frame probability (0-1) of picking a new direction, rolled
        // independently each frame, unlike MoveSnake's fixed 10-frame
        // cadence — the 0.1f default averages out to roughly the same turn
        // frequency.
        //
        // The boundary check predicts against Velocity (this enemy's real,
        // already-accumulating momentum — see Update() below, which decays
        // it by 0.8x/frame rather than resetting it) plus this frame's new
        // step, not just the new step alone — checking the step alone would
        // let several frames of sustained outward movement build up enough
        // carried momentum to blow well past wanderDistance before a
        // same-frame direction change could catch up. Once that predicted
        // position would cross the boundary, Velocity is zeroed (killing
        // the outward momentum outright) and direction points straight back
        // at origin, so the leash actually holds instead of just slowing
        // the drift.
        // anchor: tethers to another Enemy's live Position instead of this
        // enemy's own spawn point, re-read every frame — otherwise
        // identical logic. Default null preserves the original
        // self-tethered behavior for every existing caller. First real use
        // with a non-null anchor: LittleScorpion.cs ("wanders around close
        // to the Scorpion Queen" — must follow her, not just its own spawn
        // spot).
        protected IEnumerable<int> MoveTethered(
            float wanderDistance = 300f,
            float speed = 0.2f,
            float updateChance = 0.1f,
            Enemy anchor = null
        )
        {
            Vector2 origin = Position;
            float direction = rand.NextFloat(0, MathHelper.TwoPi);
            while (true)
            {
                Vector2 center = anchor != null ? anchor.Position : origin;

                if (rand.NextDouble() < updateChance)
                {
                    direction += rand.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2);
                    direction = MathHelper.WrapAngle(direction);
                }

                Vector2 candidateVelocity = Velocity + Extensions.FromPolar(direction, speed);
                if (
                    Vector2.DistanceSquared(Position + candidateVelocity, center)
                    > wanderDistance * wanderDistance
                )
                {
                    direction = (center - Position).ToAngle();
                    Velocity = Vector2.Zero;
                }

                Velocity += Extensions.FromPolar(direction, speed);
                yield return 0;
            }
        }

        // Was private (only CreateWanderer used it, from within this same
        // class) — widened to protected the moment a real subclass needed
        // it directly: SandDevil.cs's "wander erratically" when it gets too
        // close to the player during its Chase phase.
        protected IEnumerable<int> MoveRandomly()
        {
            float direction = rand.NextFloat(0, MathHelper.TwoPi);
            while (true)
            {
                direction += rand.NextFloat(-0.1f, 0.1f);
                direction = MathHelper.WrapAngle(direction);
                for (int i = 0; i < 6; i++)
                {
                    Velocity += Extensions.FromPolar(direction, 0.4f);
                    yield return 0;
                }
            }
        }

        // Steers toward a point orbiting `center` at `radius`, advancing
        // `angularSpeed` radians/frame — a constant inward pull toward
        // wherever on the circle is "next," rather than toward center
        // directly. First real use: Cave Pirate Veteran (Pirate Cave), which
        // runs this alongside FollowPlayer() so it closes in, then settles
        // into circling once near the target radius, with no separate
        // distance-gated state machine needed — FollowPlayer's constant pull
        // toward the player and this orbit's pull toward the circle just add
        // together. Dreadstump the Pirate King (the Pirate Cave boss) reuses
        // the same primitive to circle the ship's mast instead of the player
        // via OrbitPoint directly.
        protected IEnumerable<int> OrbitPoint(
            Func<Vector2> center,
            float radius,
            float angularSpeed = 0.02f,
            float acceleration = 0.4f
        )
        {
            float angle = rand.NextFloat(0, MathHelper.TwoPi);
            while (true)
            {
                angle = MathHelper.WrapAngle(angle + angularSpeed);
                Vector2 target = center() + Extensions.FromPolar(angle, radius);
                Vector2 toTarget = target - Position;
                if (toTarget != Vector2.Zero)
                    Velocity += toTarget.ScaleTo(acceleration);
                yield return 0;
            }
        }

        protected IEnumerable<int> OrbitPlayer(
            float radius,
            float angularSpeed = 0.02f,
            float acceleration = 0.4f
        ) => OrbitPoint(() => Player.Instance.Position, radius, angularSpeed, acceleration);

        #endregion

        #region Attack Behaviors

        protected IEnumerable<int> Spray(
            int projectileSpeed = 3,
            int projectileAmount = 5,
            int damage = 10,
            Texture2D projectileImage = null,
            Entity.CollisionShape collisionShape = Entity.CollisionShape.Circle
        )
        {
            while (true)
            {
                var aim = Player.Instance.Position - Position;
                if (aim.LengthSquared() > 0 && projectileCooldownRemaining <= 0)
                {
                    projectileCooldownRemaining = projectileCooldown - (1 * 1);
                    float aimAngle = aim.ToAngle();
                    Quaternion aimQuat = Quaternion.CreateFromYawPitchRoll(0, 0, aimAngle);

                    float randomSpread = rand.NextFloat(-0.1f, 0.1f) + rand.NextFloat(-0.1f, 0.1f);

                    float bulletOffset = 0.05f;
                    for (var i = 0; i < projectileAmount; i++)
                    {
                        Vector2 vel = Extensions.FromPolar(
                            aimAngle + randomSpread + (i * bulletOffset),
                            projectileSpeed
                        );

                        EntityManager.Add(
                            new EnemyProjectile(Position, vel, projectileImage)
                            {
                                Damage = damage,
                                Shape = collisionShape,
                            }
                        );
                    }
                }
                if (projectileCooldownRemaining > 0)
                    projectileCooldownRemaining--;

                yield return 0;
            }
        }

        IEnumerable<int> Shoot(int projectileSpeed = 1)
        {
            while (true)
            {
                var aim = Player.Instance.Position - Position;
                if (aim.LengthSquared() > 0 && projectileCooldownRemaining <= 0)
                {
                    projectileCooldownRemaining = projectileCooldown - (1 * 1);
                    float aimAngle = aim.ToAngle();
                    Quaternion aimQuat = Quaternion.CreateFromYawPitchRoll(0, 0, aimAngle);
                    float randomSpread = rand.NextFloat(-0.1f, 0.1f) + rand.NextFloat(-0.1f, 0.1f);
                    Vector2 vel = Extensions.FromPolar(aimAngle + randomSpread, projectileSpeed);
                    EntityManager.Add(new EnemyProjectile(Position, vel));
                }
                if (projectileCooldownRemaining > 0)
                    projectileCooldownRemaining--;

                yield return 0;
            }
        }

        IEnumerable<int> Bomb(int projectileSpeed = 3)
        {
            while (true)
            {
                if (projectileCooldownRemaining <= 0)
                {
                    projectileCooldownRemaining = projectileCooldown - (1 * 1);

                    for (int i = 0; i < 35; i++)
                    {
                        Vector2 vel = Extensions.FromPolar(i * 10, projectileSpeed);
                        EntityManager.Add(new EnemyProjectile(Position, vel) { duration = 50 });
                    }
                }

                if (projectileCooldownRemaining > 0)
                    projectileCooldownRemaining--;

                yield return 0;
            }
        }

        // Same shared projectileCooldown/aim-at-player shape as Shoot()
        // above, but only actually fires while the player is within range —
        // Shoot()/Spray()/Bomb() all fire regardless of distance. First
        // real use: CreatePirate() below ("fire a single shot towards them
        // if they get close enough").
        // cooldownFrames: overrides the shared projectileCooldown (250
        // ticks) with this call's own independent, locally-tracked cooldown
        // instead — needed the moment a caller's spec states an explicit
        // Cooldown that isn't 250 (e.g. Sandsman King's 10s / Sandsman
        // Archer's 1s — see SandsmanKing.cs/SandsmanArcher.cs). Left null,
        // this is a byte-for-byte no-op for every existing caller (Pirate,
        // Little Scorpion), which still share Enemy's own private cooldown
        // field exactly as before.
        //
        // collisionShape: forwarded straight through to EnemyProjectile's
        // own matching constructor parameter, overriding the projectile's
        // default CollisionShape.Circle (e.g. a wide slash/beam sprite that
        // reads better hit-testing as a rectangle — see Bandit.cs's own
        // Rectangle-shaped sword slash, built by hand rather than through
        // this method since Bandit doesn't use ShootIfInRange). Left null,
        // this is a byte-for-byte no-op for every existing caller.
        protected IEnumerable<int> ShootIfInRange(
            float range,
            int damage,
            float projectileSpeed,
            Texture2D projectileImage = null,
            int? cooldownFrames = null,
            CollisionShape? collisionShape = null
        )
        {
            float rangeSquared = range * range;
            int localCooldownRemaining = 0;
            while (true)
            {
                bool ready = cooldownFrames.HasValue
                    ? localCooldownRemaining <= 0
                    : projectileCooldownRemaining <= 0;

                var aim = Player.Instance.Position - Position;
                if (aim.LengthSquared() > 0 && aim.LengthSquared() <= rangeSquared && ready)
                {
                    if (cooldownFrames.HasValue)
                        localCooldownRemaining = cooldownFrames.Value;
                    else
                        projectileCooldownRemaining = projectileCooldown - (1 * 1);

                    float aimAngle = aim.ToAngle();
                    float randomSpread = rand.NextFloat(-0.1f, 0.1f) + rand.NextFloat(-0.1f, 0.1f);
                    Vector2 vel = Extensions.FromPolar(aimAngle + randomSpread, projectileSpeed);
                    EntityManager.Add(
                        new EnemyProjectile(Position, vel, projectileImage, collisionShape)
                        {
                            Damage = damage,
                        }
                    );
                }

                if (cooldownFrames.HasValue)
                {
                    if (localCooldownRemaining > 0)
                        localCooldownRemaining--;
                }
                else if (projectileCooldownRemaining > 0)
                {
                    projectileCooldownRemaining--;
                }

                yield return 0;
            }
        }

        // Same range/cooldown-override shape as ShootIfInRange above, but
        // fires `shots` projectiles in one volley, evenly spaced
        // `angleStep` apart and centered on the aim direction — e.g. 2
        // shots at a small angleStep read as a narrow "V", while N shots at
        // angleStep = 360/N read as a full symmetric star regardless of aim
        // (the two are the same formula; a full-circle step just happens to
        // make the centering irrelevant). First real use: Little Blue
        // Jelly's V-shaped shot and Little Green Jelly's 5-point star.
        protected IEnumerable<int> FanShot(
            float range,
            int damage,
            float projectileSpeed,
            int shots,
            float angleStep,
            Texture2D projectileImage = null,
            int? cooldownFrames = null
        )
        {
            float rangeSquared = range * range;
            int localCooldownRemaining = 0;
            while (true)
            {
                bool ready = cooldownFrames.HasValue
                    ? localCooldownRemaining <= 0
                    : projectileCooldownRemaining <= 0;

                var aim = Player.Instance.Position - Position;
                if (aim.LengthSquared() > 0 && aim.LengthSquared() <= rangeSquared && ready)
                {
                    if (cooldownFrames.HasValue)
                        localCooldownRemaining = cooldownFrames.Value;
                    else
                        projectileCooldownRemaining = projectileCooldown - (1 * 1);

                    float aimAngle = aim.ToAngle();
                    float randomSpread = rand.NextFloat(-0.1f, 0.1f) + rand.NextFloat(-0.1f, 0.1f);
                    float centerOffset = (shots - 1) / 2f;
                    for (int i = 0; i < shots; i++)
                    {
                        float shotAngle = aimAngle + randomSpread + (i - centerOffset) * angleStep;
                        Vector2 vel = Extensions.FromPolar(shotAngle, projectileSpeed);
                        EntityManager.Add(
                            new EnemyProjectile(Position, vel, projectileImage) { Damage = damage }
                        );
                    }
                }

                if (cooldownFrames.HasValue)
                {
                    if (localCooldownRemaining > 0)
                        localCooldownRemaining--;
                }
                else if (projectileCooldownRemaining > 0)
                {
                    projectileCooldownRemaining--;
                }

                yield return 0;
            }
        }

        // Non-damaging flavor behavior — a random line from taunts floats
        // above this enemy (see TauntBubble.cs) once every intervalFrames,
        // while the player is within range. Added via AddBehaviour (not
        // AddAttackBehaviour), same as movement — talking isn't blocked by
        // Stunned the way a real attack is. First real use:
        // BeachedBuccaneer.cs.
        protected IEnumerable<int> TauntWhenPlayerNear(
            float range,
            string[] taunts,
            int intervalFrames = 300
        )
        {
            float rangeSquared = range * range;
            int cooldownRemaining = 0;
            while (true)
            {
                if (cooldownRemaining <= 0)
                {
                    if (Vector2.DistanceSquared(Player.Instance.Position, Position) <= rangeSquared)
                    {
                        string taunt = taunts[rand.Next(taunts.Length)];
                        EntityManager.Add(new TauntBubble(this, taunt));
                        cooldownRemaining = intervalFrames;
                    }
                }
                else
                {
                    cooldownRemaining--;
                }

                yield return 0;
            }
        }

        // Periodically raises Defense by `multiplier` for durationFrames,
        // then reverts — a simple self-buff cycle. FlashRed() signals the
        // moment it triggers, the same visual cue phase transitions already
        // use. First real use: Pirate Cave's Pirate Captain/Admiral
        // ("occasionally Armored"). Assumes nothing else mutates Defense
        // while this is running.
        protected IEnumerable<int> PeriodicArmor(
            int intervalFrames,
            int durationFrames,
            float multiplier = 1.5f
        )
        {
            int baseDefense = Defense;
            while (true)
            {
                for (int i = 0; i < intervalFrames; i++)
                    yield return 0;

                FlashRed();
                Defense = (int)(baseDefense * multiplier);

                for (int i = 0; i < durationFrames; i++)
                    yield return 0;

                Defense = baseDefense;
            }
        }

        #endregion

        #region Enemy Types

        public static Enemy CreateWanderer(Vector2 position)
        {
            var enemy = new Enemy(Art.Enemy, position)
            {
                health = 150,
                healthMax = 150,
                PointValue = 15,
            };

            enemy.AddBehaviour(enemy.MoveRandomly());
            enemy.AddAttackBehaviour(enemy.Bomb());
            enemy.AddBehaviour(enemy.RegenHealth());

            return enemy;
        }

        // Mid-level — a tougher, faster-closing cousin of Wanderer. Reuses
        // Wanderer's sprite tinted orange-red (no new art), a step up from
        // both Wanderer and Seeker in health/PointValue, and a movement +
        // attack combo ("rush the player, then burst") not used by any
        // existing enemy: FollowPlayer (as fast as Seeker's own chase) paired
        // with Bomb (previously only ever paired with MoveRandomly/MoveSnake).
        public static Enemy CreateBrute(Vector2 position)
        {
            var enemy = new Enemy(Art.Enemy, position)
            {
                health = 300,
                healthMax = 300,
                PointValue = 120,
                tint = Color.OrangeRed,
            };

            enemy.AddBehaviour(enemy.FollowPlayer(0.35f));
            enemy.AddAttackBehaviour(enemy.Bomb(4));
            enemy.AddBehaviour(enemy.RegenHealth());

            return enemy;
        }

        public static Enemy CreateSeeker(Vector2 position)
        {
            var enemy = new Enemy(Art.Enemy2, position)
            {
                health = 50,
                healthMax = 50,
                PointValue = 7,
            };

            enemy.AddBehaviour(enemy.FollowPlayer(0.25f));
            enemy.AddAttackBehaviour(enemy.Spray());

            return enemy;
        }

        public static Enemy CreateSnake(Vector2 position)
        {
            var enemy = new Enemy(Art.Snake, position)
            {
                health = 5,
                healthMax = 5,
                PointValue = 2,
                deathSound = Sound.SnakesDeath,
                hitSound = Sound.SnakesHit,

                // Low-tier gear only, per the backlog's own example — the
                // weakest trash enemy in the game shouldn't ever hand out a
                // stat potion, just (low-tier — see ItemSpawner.
                // IsWeakEnemy) equipment.
                DropPool =
                    ItemSpawner.LootCategory.Weapon
                    | ItemSpawner.LootCategory.Armor
                    | ItemSpawner.LootCategory.Ring
                    | ItemSpawner.LootCategory.AbilityItem,
            };

            enemy.AddBehaviour(enemy.MoveSnake());
            enemy.AddAttackBehaviour(enemy.Shoot(2));

            return enemy;
        }

        // Low-level — a tougher snake-family sibling, using real art
        // (Art.BigSnake) rather than a tint. Same weaving MoveSnake()
        // movement as the base Snake so it reads as the same family, with
        // more health and a faster Shoot to feel like a real step up rather
        // than a reskin. Grouped with Snake in EnemySpawner.BasicEnemyPool
        // at the same Level 1 requirement, so it can appear in the same
        // waves as Snake from the very start.
        public static Enemy CreateBigSnake(Vector2 position)
        {
            var enemy = new Enemy(Art.BigSnake, position)
            {
                health = 500,
                healthMax = 500,
                PointValue = 250,
                Defense = 10,
                deathSound = Sound.SnakesDeath,
                hitSound = Sound.SnakesHit,
                portalDropOnDeath = Portal.Destination.SthenoBossRealm,

                // Leans toward potions per the backlog's own example — gear
                // stays fully in the pool (DropPool defaults to All, unlike
                // Snake's gear-only exclusion), just less likely to roll
                // than potions. Literal percentages (DropChances is now the
                // only lever — see its own doc comment), carried over
                // exactly from this enemy's old DropWeights-multiplier
                // values (weight/16 for the PointValue>=100 gear bucket,
                // weight/30 for StatPotion's fixed base, weight/20 for
                // HealthManaPotion's) rather than retuned.
                DropChances = new()
                {
                    [ItemSpawner.LootCategory.StatPotion] = 0.08333f,
                    [ItemSpawner.LootCategory.HealthManaPotion] = 0.125f,
                    [ItemSpawner.LootCategory.Weapon] = 0.03125f,
                    [ItemSpawner.LootCategory.Armor] = 0.03125f,
                    [ItemSpawner.LootCategory.Ring] = 0.03125f,
                    [ItemSpawner.LootCategory.AbilityItem] = 0.03125f,
                },
            };

            enemy.AddBehaviour(enemy.MoveSnake());
            enemy.AddAttackBehaviour(enemy.Shoot(3));

            return enemy;
        }

        // Beach biome's basic wave enemy — same "easiest tier" positioning
        // as Snake (near-identical HP/PointValue), just with different
        // movement/attack behavior: chases the player outright (FollowPlayer)
        // rather than Snake's weaving dash, and only fires while the player
        // is within Range (ShootIfInRange, not the unconditional Shoot()).
        // Also the escort enemy BeachedBuccaneer.cs's mini-boss pack spawns
        // alongside it, same relationship as Snake/CreateBigSnake.
        public static Enemy CreatePirate(Vector2 position)
        {
            var enemy = new Enemy(Art.Pirate, position)
            {
                health = 5,
                healthMax = 5,
                PointValue = 2,
                DropPool = BeachDropPool,
                DropChances = BeachDropChances,
                DropTierRanges = BeachDropTierRanges,
                PortalDropChances = BeachPortalDropChances,
            };

            enemy.AddBehaviour(enemy.FollowPlayer(0.2f));
            enemy.AddAttackBehaviour(
                enemy.ShootIfInRange(range: 2.4f * 32f, damage: 4, projectileSpeed: 4f)
            );

            return enemy;
        }

        // Low-level — a slightly sturdier alternative to Snake for variety
        // early on. Reuses Snake's sprite tinted light green (no new art),
        // sitting between Snake and Seeker in both health and PointValue. A
        // movement + attack combo not used by any existing enemy:
        // MoveRandomly (previously only Wanderer's) paired with Spray
        // (previously only Seeker's/bosses') — a slow-wandering blob that
        // sprays when the player gets close, instead of Snake's tight
        // weaving dash-and-shoot.
        public static Enemy CreateSlime(Vector2 position)
        {
            var enemy = new Enemy(Art.Snake, position)
            {
                health = 20,
                healthMax = 20,
                PointValue = 4,
                tint = Color.LightGreen,
            };

            enemy.AddBehaviour(enemy.MoveRandomly());
            enemy.AddAttackBehaviour(enemy.Spray(2, 3, damage: 8));

            return enemy;
        }

        public static Enemy CreateSpriteGod(Vector2 position)
        {
            var enemy = new Enemy(Art.EnemySpriteGod, position)
            {
                health = 1500,
                healthMax = 1500,
                PointValue = 200,
                deathSound = Sound.SpriteGodDeath,
                hitSound = Sound.SpriteGodHit,
                portalDropOnDeath = Portal.Destination.BossRealm,
            };

            enemy.AddBehaviour(enemy.MoveSnake());
            enemy.AddBehaviour(enemy.Bomb(10));

            return enemy;
        }

        // Trigger enemy for the third boss, Cube God (see Bosses/CubeGod.cs)
        // — same "plain Enemy factory, portalDropOnDeath set" shape as
        // CreateSpriteGod/CreateBigSnake above, not biome-restricted (the
        // real fight's own lore is "hordes of sentient squares" left behind
        // everywhere, unlike CreateSpriteGod's Sprite-forest theming, so no
        // Beach exclusion either — see EnemySpawner.cs's own independent
        // roll for this). No dedicated art exists for the "cube system" —
        // drawn as a plain tinted square via Art.HealthBar, same as every
        // other entity in this family (see CubeGod.cs's own comment).
        public static Enemy CreateCube(Vector2 position)
        {
            var enemy = new Enemy(Art.HealthBar, position)
            {
                health = 40,
                healthMax = 40,
                Defense = 1,
                PointValue = 90,
                portalDropOnDeath = Portal.Destination.CubeGodBossRealm,
                drawScale = 24f,
                Radius = 12f,
                tint = Color.LightGray,

                // Generic non-Beach pool, same shape as CreateBigSnake's own
                // table — no StatPotion (simply has no entry below, now that
                // DropChances is the only lever — see its own doc comment),
                // gear/ability items all still reachable, just not the sole
                // outcome. Literal percentages carried over exactly from
                // this enemy's old DropWeights-multiplier values (weight/30
                // for the PointValue<100 gear bucket, weight/20 for
                // HealthManaPotion's fixed base) rather than retuned.
                DropChances = new()
                {
                    [ItemSpawner.LootCategory.Weapon] = 0.03333f,
                    [ItemSpawner.LootCategory.Armor] = 0.03333f,
                    [ItemSpawner.LootCategory.Ring] = 0.01667f,
                    [ItemSpawner.LootCategory.AbilityItem] = 0.01667f,
                    [ItemSpawner.LootCategory.HealthManaPotion] = 0.05f,
                },
            };

            enemy.AddBehaviour(enemy.MoveRandomly());
            enemy.AddAttackBehaviour(enemy.ShootIfInRange(range: 6f * 32f, damage: 6, projectileSpeed: 3f));

            return enemy;
        }

        // Pirate Cave (see Data/DungeonType_PirateCave.json and
        // docs/DEVLOG.md) — every factory below sourced directly from each
        // enemy's own realmeye.com/wiki page. Real art supplied for each
        // (Content/Dungeons/Pirate Cave/), no tinted reskins needed.
        // tiles/sec -> world units/frame via `* 32 / 60`; tiles -> world
        // units via `* 32` — same conversion Player's own tile-speed stats
        // already use.

        // Harmless "critters" — HP5/DEF0/PointValue1, wander only, never
        // attack, identical on the wiki apart from sprite/drop flavor.
        // Water-avoidance from the wiki text is skipped: MoveRandomly() has
        // no tile-awareness to build it on, and it's purely cosmetic with
        // zero mechanical effect.
        public static Enemy CreateCavePirateCabinBoy(Vector2 position)
        {
            var enemy = new Enemy(Art.CavePirateCabinBoy, position)
            {
                health = 5,
                healthMax = 5,
                PointValue = 1,
                DropPool = PirateCaveDropPool,
                DropChances = PirateCaveDropChances,
                DropTierRanges = PirateCaveDropTierRanges,
            };
            enemy.AddBehaviour(enemy.MoveRandomly());
            return enemy;
        }

        public static Enemy CreateCavePirateHunchback(Vector2 position)
        {
            var enemy = new Enemy(Art.CavePirateHunchback, position)
            {
                health = 5,
                healthMax = 5,
                PointValue = 1,
                DropPool = PirateCaveDropPool,
                DropChances = PirateCaveDropChances,
                DropTierRanges = PirateCaveDropTierRanges,
            };
            enemy.AddBehaviour(enemy.MoveRandomly());
            return enemy;
        }

        public static Enemy CreateCavePirateMacaw(Vector2 position)
        {
            var enemy = new Enemy(Art.CavePirateMacaw, position)
            {
                health = 5,
                healthMax = 5,
                PointValue = 1,
                DropPool = PirateCaveDropPool,
                DropChances = PirateCaveDropChances,
                DropTierRanges = PirateCaveDropTierRanges,
            };
            enemy.AddBehaviour(enemy.MoveRandomly());
            return enemy;
        }

        public static Enemy CreateCavePirateMoll(Vector2 position)
        {
            var enemy = new Enemy(Art.CavePirateMoll, position)
            {
                health = 5,
                healthMax = 5,
                PointValue = 1,
                DropPool = PirateCaveDropPool,
                DropChances = PirateCaveDropChances,
                DropTierRanges = PirateCaveDropTierRanges,
            };
            enemy.AddBehaviour(enemy.MoveRandomly());
            return enemy;
        }

        public static Enemy CreateCavePirateMonkey(Vector2 position)
        {
            var enemy = new Enemy(Art.CavePirateMonkey, position)
            {
                health = 5,
                healthMax = 5,
                PointValue = 1,
                DropPool = PirateCaveDropPool,
                DropChances = PirateCaveDropChances,
                DropTierRanges = PirateCaveDropTierRanges,
            };
            enemy.AddBehaviour(enemy.MoveRandomly());
            return enemy;
        }

        public static Enemy CreateCavePirateParrot(Vector2 position)
        {
            var enemy = new Enemy(Art.CavePirateParrot, position)
            {
                health = 5,
                healthMax = 5,
                PointValue = 1,
                DropPool = PirateCaveDropPool,
                DropChances = PirateCaveDropChances,
                DropTierRanges = PirateCaveDropTierRanges,
            };
            enemy.AddBehaviour(enemy.MoveRandomly());
            return enemy;
        }

        // Melee "sword" chasers — FollowPlayer + a short-range sword swing
        // (an EnemyProjectile via ShootIfInRange, same as every other
        // "melee" enemy this engine already has).
        public static Enemy CreateCavePirateBrawler(Vector2 position)
        {
            var enemy = new Enemy(Art.CavePirateBrawler, position)
            {
                health = 20,
                healthMax = 20,
                PointValue = 2,
                DropPool = PirateCaveDropPool,
                DropChances = PirateCaveDropChances,
                DropTierRanges = PirateCaveDropTierRanges,
            };
            enemy.AddBehaviour(enemy.FollowPlayer(0.4f));
            enemy.AddAttackBehaviour(
                enemy.ShootIfInRange(
                    range: 3.9f * 32f,
                    damage: 4,
                    projectileSpeed: 6.5f * 32f / 60f,
                    projectileImage: Art.PirateSword
                )
            );
            return enemy;
        }

        public static Enemy CreateCavePirateSailor(Vector2 position)
        {
            var enemy = new Enemy(Art.CavePirateSailor, position)
            {
                health = 30,
                healthMax = 30,
                PointValue = 3,
                DropPool = PirateCaveDropPool,
                DropChances = PirateCaveDropChances,
                DropTierRanges = PirateCaveDropTierRanges,
            };
            enemy.AddBehaviour(enemy.FollowPlayer(0.4f));
            enemy.AddAttackBehaviour(
                enemy.ShootIfInRange(
                    range: 3.9f * 32f,
                    damage: 7,
                    projectileSpeed: 6.5f * 32f / 60f,
                    projectileImage: Art.PirateSword
                )
            );
            return enemy;
        }

        // "Chases the nearest player, circling them when close enough" —
        // FollowPlayer's constant pull toward the player and OrbitPlayer's
        // pull toward the circle just add together, so it closes in, then
        // settles into circling once near the target radius, with no
        // separate distance-gated state machine needed (see OrbitPoint's
        // own doc comment).
        public static Enemy CreateCavePirateVeteran(Vector2 position)
        {
            var enemy = new Enemy(Art.CavePirateVeteran, position)
            {
                health = 35,
                healthMax = 35,
                Defense = 2,
                PointValue = 4,
                DropPool = PirateCaveDropPool,
                DropChances = PirateCaveDropChances,
                DropTierRanges = PirateCaveDropTierRanges,
            };
            enemy.AddBehaviour(enemy.FollowPlayer(0.2f));
            enemy.AddBehaviour(enemy.OrbitPlayer(radius: 5.2f * 32f));
            enemy.AddAttackBehaviour(
                enemy.ShootIfInRange(
                    range: 5.2f * 32f,
                    damage: 8,
                    projectileSpeed: 6.5f * 32f / 60f,
                    projectileImage: Art.PirateSword
                )
            );
            return enemy;
        }

        // Ranged "cannon" sentries — no movement stat on the wiki at all,
        // and the wiki never says they chase (only "fires at the nearest
        // player," "always have other pirates with them... protecting
        // Dreadstump") — stationary, no AddBehaviour movement at all.
        public static Enemy CreatePirateLieutenant(Vector2 position)
        {
            var enemy = new Enemy(Art.PirateLieutenant, position)
            {
                health = 70,
                healthMax = 70,
                Defense = 2,
                PointValue = 7,
                DropPool = PirateCaveDropPool,
                DropChances = PirateCaveDropChances,
                DropTierRanges = PirateCaveDropTierRanges,
            };
            enemy.AddAttackBehaviour(
                enemy.ShootIfInRange(
                    range: 10f * 32f,
                    damage: 10,
                    projectileSpeed: 5f * 32f / 60f,
                    projectileImage: Art.PirateCannonBullet,
                    cooldownFrames: 150
                )
            );
            return enemy;
        }

        public static Enemy CreatePirateCommander(Vector2 position)
        {
            var enemy = new Enemy(Art.PirateCommander, position)
            {
                health = 80,
                healthMax = 80,
                Defense = 3,
                PointValue = 8,
                DropPool = PirateCaveDropPool,
                DropChances = PirateCaveDropChances,
                DropTierRanges = PirateCaveDropTierRanges,
            };
            enemy.AddAttackBehaviour(
                enemy.ShootIfInRange(
                    range: 10f * 32f,
                    damage: 12,
                    projectileSpeed: 5f * 32f / 60f,
                    projectileImage: Art.PirateCannonBullet,
                    cooldownFrames: 110
                )
            );
            return enemy;
        }

        // "Shoot one fast cannonball, 2 slower cannonballs, and occasionally
        // be Armored" — the two shot types run as independent
        // ShootIfInRange behaviours on their own cooldowns rather than one
        // combined volley, which already reads as "sometimes both, mostly
        // the fast one" given the slower shot's longer cooldown.
        public static Enemy CreatePirateCaptain(Vector2 position)
        {
            var enemy = new Enemy(Art.PirateCaptain, position)
            {
                health = 100,
                healthMax = 100,
                Defense = 4,
                PointValue = 10,
                DropPool = PirateCaveDropPool,
                DropChances = PirateCaveDropChances,
                DropTierRanges = PirateCaveDropTierRanges,
            };
            enemy.AddAttackBehaviour(
                enemy.ShootIfInRange(
                    range: 11.25f * 32f,
                    damage: 14,
                    projectileSpeed: 5f * 32f / 60f,
                    projectileImage: Art.PirateCannonBullet,
                    cooldownFrames: 100
                )
            );
            enemy.AddAttackBehaviour(
                enemy.ShootIfInRange(
                    range: 10.4f * 32f,
                    damage: 18,
                    projectileSpeed: 2f * 32f / 60f,
                    projectileImage: Art.PirateShot,
                    cooldownFrames: 160
                )
            );
            enemy.AddBehaviour(enemy.PeriodicArmor(intervalFrames: 300, durationFrames: 120));
            return enemy;
        }

        // "Always shoot single, fast cannonballs and sometimes shoot two
        // slower cannonballs. Occasionally... Armored" — same two-
        // independent-attacks shape as Captain, a step more aggressive on
        // every stat.
        public static Enemy CreatePirateAdmiral(Vector2 position)
        {
            var enemy = new Enemy(Art.PirateAdmiral, position)
            {
                health = 120,
                healthMax = 120,
                Defense = 5,
                PointValue = 12,
                DropPool = PirateCaveDropPool,
                DropChances = PirateCaveDropChances,
                DropTierRanges = PirateCaveDropTierRanges,
            };
            enemy.AddAttackBehaviour(
                enemy.ShootIfInRange(
                    range: 11.25f * 32f,
                    damage: 15,
                    projectileSpeed: 5f * 32f / 60f,
                    projectileImage: Art.PirateCannonBullet,
                    cooldownFrames: 90
                )
            );
            enemy.AddAttackBehaviour(
                enemy.ShootIfInRange(
                    range: 10.4f * 32f,
                    damage: 20,
                    projectileSpeed: 2f * 32f / 60f,
                    projectileImage: Art.PirateShot,
                    cooldownFrames: 140
                )
            );
            enemy.AddBehaviour(enemy.PeriodicArmor(intervalFrames: 260, durationFrames: 120));
            return enemy;
        }

        #endregion
    }
}
