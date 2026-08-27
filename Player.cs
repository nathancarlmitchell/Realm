using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Realm.States;

namespace Realm
{
    public class Player : Entity
    {
        private static Player instance;
        public static Player Instance
        {
            get
            {
                if (instance == null)
                    instance = new Player();
                return instance;
            }
            set { instance = value; }
        }

        public static readonly Random rand = new();

        public Guid ID;
        public string Name;
        public string Description;

        public float Opacity;

        public InventorySystem Inventory { get; set; }

        public int Health;
        public int HealthMax;

        public int Mana;
        public int ManaMax;

        public int Attack;
        public int Defense;
        public float Speed;
        public int Dexterity;
        public int Vitality;
        public int Wisdom;

        public int baseHealth = 100;
        public int baseMana = 100;
        public int baseAttack = 17;
        public int baseDefense = 0;
        public float baseSpeed = 17;
        public int baseDexterity = 17;
        public int BaseVitality = 5;
        public int baseWisdom = 23;

        public int MaxHealth;
        public int MaxMana;
        public int MaxAttack;
        public int MaxDefense;
        public float MaxSpeed;
        public int MaxDexterity;
        public int MaxVitality;
        public int MaxWisdom;

        // Permanent bonuses earned from stat potions. Unlike equipment bonuses
        // (read live from Weapon/Armor/Ring below — the equipped item is
        // already the source of truth, so there's nothing to separately track),
        // potions have no object to re-derive the bonus from later, so they
        // need their own persisted running total.
        public int PotionAttackBonus;
        public int PotionDefenseBonus;
        public float PotionSpeedBonus;
        public int PotionDexterityBonus;
        public int PotionVitalityBonus;
        public int PotionWisdomBonus;
        public int PotionHealthMaxBonus;
        public int PotionManaMaxBonus;

        // Timed bonuses from abilities (e.g. Knight's Shield Slam) — same
        // idea as the Potion*Bonus fields above (a proper input to
        // RecalculateStats()'s formula, not a raw stat mutation, so any
        // other RecalculateStats() trigger mid-buff can't silently drop or
        // double-apply them), except these expire on their own instead of
        // being permanent. Not persisted — a buff that was mid-countdown at
        // save time simply isn't there anymore on reload, same as an
        // in-progress dungeon isn't saved either. Only Attack/Defense/Speed/
        // Dexterity currently draw an on-screen "+" indicator (see
        // DrawTemporaryBonusIndicators) — Vitality/Wisdom/HealthMax/ManaMax
        // work identically but have no assigned color yet.
        public int TemporaryAttackBonus;
        public int TemporaryDefenseBonus;
        public float TemporarySpeedBonus;
        public int TemporaryDexterityBonus;
        public int TemporaryVitalityBonus;
        public int TemporaryWisdomBonus;
        public int TemporaryHealthMaxBonus;
        public int TemporaryManaMaxBonus;

        private int temporaryAttackBonusFrames;
        private int temporaryDefenseBonusFrames;
        private int temporarySpeedBonusFrames;
        private int temporaryDexterityBonusFrames;
        private int temporaryVitalityBonusFrames;
        private int temporaryWisdomBonusFrames;
        private int temporaryHealthMaxBonusFrames;
        private int temporaryManaMaxBonusFrames;

        // A direct multiplier on incoming damage (Hit() below), not a
        // Defense stat bonus fed through RecalculateStats() like the
        // Temporary*Bonus fields above — e.g. Knight's Shield Slam sets this
        // to 0.75 ("you receive 75% damage") for its duration. 1f (no
        // reduction) outside an active effect.
        public float DamageTakenMultiplier = 1f;
        private int damageTakenMultiplierFrames;

        // The Healing status (e.g. a Priest's Tome, "Red Cross Healing") —
        // adds this flat rate on top of HealthRegenPerSecond above while
        // active. 0 (no bonus) outside an active effect. Multiple
        // applications don't stack; see ApplyHealing() below for the
        // "strongest one overrides" rule.
        public float HealingAmountPerSecond;
        private int healingDurationFrames;

        // Vital Combat: true while the player is "in combat" (IC) —
        // entered (or refreshed) by RegisterHit() below whenever a single
        // hit's raw damage meets or exceeds CombatTrigger, and exited
        // automatically once inCombatFrames counts down to 0 without
        // another qualifying hit in the meantime. While IC, VIT/WIS-driven
        // regeneration is halved — see HealthRegenPerSecond/
        // ManaRegenPerSecond above, which read this directly. The design
        // doc also calls out a 2-second delay on HP/MP recovery from Pets
        // while IC, but this engine has no pet system for that to apply
        // to, so there's nothing to hook it into.
        public bool InCombat { get; private set; }
        private int inCombatFrames;

        // Minimum raw (pre-Defense, post-DamageTakenMultiplier) hit damage
        // needed to (re-)enter combat. Scales with Defense across four
        // brackets of diminishing effectiveness — each bracket's own
        // contribution is folded into the next bracket's starting value
        // (15/30/45/60) rather than recomputed from 0 every time. Bracket
        // edges are 15/35/65/125 DEF, at rates 100%/75%/50%/25%, capping
        // permanently at 60 beyond 125 DEF (0% rate) — verified directly
        // against the three worked examples in the design doc (Archer 25
        // DEF -> 22, Rogue 45 DEF -> 35, Knight 77 DEF -> 48). An earlier
        // paragraph in the same doc described 15/30/45 edges instead, but
        // that layout doesn't reproduce those same worked examples — a
        // documentation error, not a second intended scaling. Floored to
        // an int, matching the doc's own "22.5 rounds down to 22" example,
        // with a 1-damage minimum ("the combat trigger starts at 1
        // damage") for the degenerate 0-Defense case, where the 1:1
        // bracket would otherwise give a meaningless trigger of 0.
        //
        // Reads PermanentDefense (Defense minus both EquipmentDefenseBonus
        // and TemporaryDefenseBonus, defined above) rather than raw Defense
        // — neither gear nor a temporary buff should let a player buy or
        // borrow their way into a higher trigger (and therefore
        // weaker/less-often regen halving); only base/level/potion Defense,
        // the parts that were actually earned and don't go away on their
        // own, count toward it.
        public int CombatTrigger
        {
            get
            {
                float def = PermanentDefense;
                float trigger;
                if (def <= 15f)
                    trigger = def;
                else if (def <= 35f)
                    trigger = 15f + (def - 15f) * 0.75f;
                else if (def <= 65f)
                    trigger = 30f + (def - 35f) * 0.5f;
                else if (def <= 125f)
                    trigger = 45f + (def - 65f) * 0.25f;
                else
                    trigger = 60f;
                return Math.Max(1, (int)trigger);
            }
        }

        // 7 seconds at 0 Vitality, reduced by 4% of Vitality (1 second per
        // 25 VIT). Clamped to a 1-frame minimum rather than 0 — a
        // literal 0 would let inCombatFrames get set to 0 by RegisterHit()
        // below, which UpdateTemporaryBonuses()'s `> 0` guard would then
        // never see, leaving InCombat stuck true forever. Purely a safety
        // floor: no class's Vitality cap comes remotely close to the 175
        // VIT it'd take to actually zero out the 7-second base.
        private int InCombatDurationFrames =>
            Math.Max(1, (int)(Math.Max(0f, 7f - Vitality * 0.04f) * 60f));

        // Same value as InCombatDurationFrames above, in seconds rather
        // than frames — for display only (Overlay.cs's hover tooltip,
        // alongside CombatTrigger). InCombatDurationFrames stays the real
        // source of truth for the actual timer; this just re-expresses it
        // in a unit a player reads more easily. Deliberately not floored/
        // clamped the way InCombatDurationFrames is (that clamp exists so
        // the *timer* can never get stuck, not because 0.0s is a
        // meaningless number to show).
        public float CombatDurationSeconds => Math.Max(0f, 7f - Vitality * 0.04f);

        // Called from Hit() with the raw hit (after DamageTakenMultiplier,
        // before Defense) — comparing against the raw hit rather than the
        // Defense-mitigated damage actually taken avoids double-counting
        // Defense's effect (it already shrinks both the trigger's
        // "hardness" and, separately, the HP actually lost). A qualifying
        // hit both enters combat for the first time and refreshes an
        // already-active one back to the full duration, matching "avoid
        // taking damage above the Combat Trigger for a period of time" to
        // exit.
        private void RegisterHit(int rawDamage)
        {
            if (rawDamage < CombatTrigger)
                return;

            InCombat = true;
            inCombatFrames = InCombatDurationFrames;
        }

        // ExperienceTotal is the only XP value actually stored — the sole
        // "how much has this character ever earned" running total,
        // incremented directly on each kill (Enemy.cs) and never reset by a
        // level-up. Level (also stored) plus this cumulative total are
        // enough to derive everything else: how much XP is required per
        // level is a fixed formula (ExperienceRequiredForLevel), so both
        // "progress within the current level" and "cumulative XP needed for
        // the next level" (below) are computed on demand instead of tracked
        // as their own separately-reset/assigned fields — nothing to fall
        // out of sync with ExperienceTotal, and nothing lost to a level-up
        // discarding whatever "overflow" XP was left in the old counter.
        public int ExperienceTotal;
        public int HighScore;

        // Starts at 50 for Level 1->2 and increases by 100 per level up
        // (100*level - 50 naturally gives 50 at level 1, so no special case
        // is needed there). Previously 50 + (level*2*50), which overshot by
        // exactly 100 XP per level from Level 2 onward — 1,800 XP too much
        // by the time a character reached Level 20 (19,850 instead of
        // 18,050 cumulative).
        private static int ExperienceRequiredForLevel(int level) => (100 * level) - 50;

        // Cumulative ExperienceTotal needed to REACH a given level (0 for
        // Level 1, the starting point). Sums each level's own requirement
        // rather than a closed-form formula — Level is capped at 20, so
        // this is at most 19 additions, cheap enough to just recompute
        // wherever it's needed instead of caching it.
        public static int CumulativeExperienceForLevel(int level)
        {
            int total = 0;
            for (int l = 1; l < level; l++)
                total += ExperienceRequiredForLevel(l);
            return total;
        }

        // Cumulative ExperienceTotal that unlocks the next level — the
        // natural "out of" value for a total-XP-based progress bar, since
        // it's on the same cumulative scale as ExperienceTotal itself
        // (unlike the old per-level ExperienceNextLevel, which resets every
        // level and was never comparable to a running total).
        public int ExperienceNextLevel => CumulativeExperienceForLevel(Level + 1);

        // Fame conversion rate changes once a character's cumulative XP
        // crosses the Level 20 threshold — computed live from
        // CumulativeExperienceForLevel(20) rather than a hardcoded XP
        // number, so it always matches whatever this engine's own leveling
        // curve says "Level 20" actually costs, instead of silently
        // drifting from it if that curve is ever retuned.
        private const int BaseFameRateBeforeLevel20 = 900; // 1 fame per 900 XP
        private const int BaseFameRateAfterLevel20 = 2000; // 1 fame per 2000 XP

        // Static + pure (same shape as ComputeStars below) so it works
        // identically against a live Player.Instance.ExperienceTotal or a
        // peeked PlayerData's saved value — e.g. Util.DeleteCharacterData
        // has no live Player instance to read from.
        public static int ComputeBaseFame(int experienceTotal)
        {
            int level20Threshold = CumulativeExperienceForLevel(20);
            if (experienceTotal <= level20Threshold)
                return experienceTotal / BaseFameRateBeforeLevel20;

            int beforeFame = level20Threshold / BaseFameRateBeforeLevel20;
            int afterFame = (experienceTotal - level20Threshold) / BaseFameRateAfterLevel20;
            return beforeFame + afterFame;
        }

        // Inverts ComputeBaseFame() above — the smallest ExperienceTotal
        // whose Base Fame reaches targetFame. Derived from the same two
        // piecewise rates rather than a hardcoded XP number, so debug
        // tooling (DebugGrantThreeStarsFame() below) stays correct if this
        // curve or those rates are ever retuned.
        private static int ExperienceForBaseFame(int targetFame)
        {
            int level20Threshold = CumulativeExperienceForLevel(20);
            int beforeFame = level20Threshold / BaseFameRateBeforeLevel20;
            if (targetFame <= beforeFame)
                return targetFame * BaseFameRateBeforeLevel20;

            int afterFame = targetFame - beforeFame;
            return level20Threshold + (afterFame * BaseFameRateAfterLevel20);
        }

        // "Base fame" — automatically converted from this character's own
        // cumulative XP throughout its life, at the rate above. Not a
        // separately-tracked/persisted field: it's purely a function of
        // ExperienceTotal (already persisted), so there's nothing to fall
        // out of sync.
        public int BaseFame => ComputeBaseFame(ExperienceTotal);

        // "Bonus fame, obtained from certain achievements during a
        // character's life" — no specific achievement grants this yet (none
        // exist in this codebase today), so this is infrastructure only: a
        // plain per-life counter starting at 0, persisted so it survives a
        // save/reload mid-run, but NOT preserved across death/delete
        // (unlike HighScore/HasReachedLevel20) — same one-life-only
        // treatment as ExperienceTotal itself, since it's meant to be
        // consumed into permanent account Fame at the moment of death, not
        // carried forward.
        public int BonusFame;

        public void AddBonusFame(int amount)
        {
            if (amount > 0)
                BonusFame += amount;
        }

        // Basic-attack rate: 1.5 attacks/sec at 0 Dexterity, scaling up to
        // 8 attacks/sec at 75 Dexterity (every point past 0 adds ~0.0867).
        // Drives Update()'s projectileCooldown accumulator below. No Berserk
        // multiplier — this engine has no Berserk status effect to hook one
        // into.
        public float AttacksPerSecond => 1.5f + 6.5f * (Dexterity / 75f);

        // Movement rate: 4 tiles/sec at 0 Speed, scaling up to 9.6 tiles/sec
        // at 75 Speed. Drives Update()'s Velocity calculation below. No
        // Speedy multiplier — this engine has no Speedy status effect to
        // hook one into (same situation as AttacksPerSecond's Berserk note
        // above).
        public float TilesPerSecond => 4f + 5.6f * (Speed / 75f);

        // Health regen rate: 2 HP/sec at 0 Vitality, +0.2407 HP/sec per
        // point past that (linear, no /75 scaling unlike AttacksPerSecond/
        // TilesPerSecond above), plus the Healing status's own rate on top
        // when active (e.g. a Priest's Tome — see HealingAmountPerSecond/
        // ApplyHealing() below). Drives Update()'s healthCooldown
        // accumulator below. Vital Combat: only the Vitality-driven term is
        // halved while InCombat (below) — the flat 2f base and Healing's
        // own rate are both untouched, matching the design doc's own
        // "regeneration caused by VIT and WIS" wording.
        public float HealthRegenPerSecond =>
            2f + 0.2407f * Vitality * (InCombat ? 0.5f : 1f) + HealingAmountPerSecond;

        // Mana regen rate: 0.5 MP/sec at 0 Wisdom, +0.12 MP/sec per point
        // past that. Drives Update()'s manaCooldown accumulator below. Same
        // Vital Combat halving as HealthRegenPerSecond above, applied only
        // to the Wisdom-driven term.
        public float ManaRegenPerSecond => 0.5f + 0.12f * Wisdom * (InCombat ? 0.5f : 1f);

        // See PlayerData.HasBeenPlayed — mirrors it on the live instance so a
        // later SavePlayerData() call doesn't regress it back to false.
        public bool HasBeenPlayed;

        // Not part of PlayerData/Util.BuildPlayerData()/LoadOrCreatePlayer()
        // (that's per-class save data) — persisted account-wide instead via
        // GameSettingsData (Util.SaveGameSettingsData()/LoadGameSettingsData()).
        // Toggled by the C key (Input.cs).
        public bool AutoFireEnabled;

        // Same account-wide GameSettingsData persistence as AutoFireEnabled
        // above. Defaults to false — bypasses Portal's confirm-before-
        // teleporting prompt entirely when true (see Portal.cs's Update()).
        // Toggled from the Settings > Gameplay tab.
        public bool AutoEnterPortalsEnabled;

        // Same account-wide GameSettingsData persistence, defaults to
        // false. Independent of (additive with) the F3/Game1._Debug
        // toggle — F3 still shows hitboxes plus the full debug HUD panel
        // together as before; this setting shows just the hitbox outlines
        // on their own, persisted across sessions, without needing the
        // rest of the debug HUD. See RealmState/NexusState.Draw()'s
        // EntityManager.DrawHitboxes() gate. Toggled from the Settings >
        // Graphics tab.
        public bool ShowHitboxesEnabled;

        // Same account-wide GameSettingsData persistence, but defaults to
        // TRUE (unlike every other toggle above) — see GameSettingsData.cs's
        // matching comment on why the DTO's own default also has to be
        // `true`, not just this field. Drives the low-health flash in
        // Update()/Draw() below. Toggled from the Settings > Graphics tab.
        public bool LowHealthIndicatorEnabled = true;

        // Same account-wide GameSettingsData persistence, 0-100, defaults
        // to 25 — the threshold both the flash and the below-sprite health
        // bar (see DrawLowHealthBar()) key off, replacing what used to be
        // a hardcoded 25% (LowHealthThresholdFraction). Adjustable from
        // the Settings > Graphics tab via a +/- stepper (SettingsState.cs's
        // NumericRow), in steps of 5.
        public int LowHealthThresholdPercent = 25;

        // Same account-wide GameSettingsData persistence, defaults to TRUE
        // (same reasoning as LowHealthIndicatorEnabled above — see
        // GameSettingsData.cs's matching comment on why the DTO's own
        // default also has to be `true`). Gates the floating "+XP" number
        // spawned in Enemy.WasShot()'s death branch. Toggled from the
        // Settings > Graphics tab.
        public bool ShowXpDropsEnabled = true;

        // Same account-wide GameSettingsData persistence, but defaults to
        // FALSE — "one can also reactivate this XP icon after level 20
        // under Video Settings: Always Show EXP." Below Level 20,
        // ShowXpDropsEnabled above is still what gates the floating "+XP"
        // number; from Level 20 onward, this setting takes over instead
        // (Enemy.WasShot()'s death branch), since the icon otherwise
        // disappears once ShowXpDropsEnabled stops mattering. Toggled from
        // the Settings > Graphics tab.
        public bool AlwaysShowExpEnabled = false;

        // Same account-wide GameSettingsData persistence, defaults to TRUE
        // (same reasoning as ShowXpDropsEnabled above). Gates the player's
        // own "I took damage" number (Player.Hit()) — separate from
        // ShowXpDropsEnabled, which only covers the XP gain number, and
        // ShowEnemyDamageNumbersEnabled below, which covers the other
        // direction (damage the player deals). Toggled from the Settings >
        // Graphics tab.
        public bool ShowPlayerDamageNumbersEnabled = true;

        // Same account-wide GameSettingsData persistence, defaults to TRUE.
        // Gates the hit number that appears over an enemy when the player
        // damages it (Enemy.WasShot()). Toggled from the Settings > Graphics
        // tab.
        public bool ShowEnemyDamageNumbersEnabled = true;

        // Same account-wide GameSettingsData persistence, defaults to TRUE.
        // Gates the two Particle.SpawnBurst() calls in Enemy.WasShot() (the
        // white burst on a hit, the orange-red burst on a kill) — not
        // Player.LevelUp()'s gold swirl, which uses a different particle
        // flavor (SwirlParticle) for a distinct celebratory moment, not a
        // combat hit. Toggled from the Settings > Graphics tab.
        public bool ShowHitParticlesEnabled = true;

        // Same account-wide GameSettingsData persistence, defaults to TRUE.
        // Gates only the yellow border Overlay.cs draws around the sidebar
        // HP bar while InCombat — the sword icon itself (and its "lighting
        // up" while InCombat) always shows regardless of this setting, per
        // the design doc's own wording only conditioning the border on
        // "if they have it enabled". Toggled from the Settings > Graphics
        // tab.
        public bool ShowCombatIndicatorEnabled = true;

        // Same account-wide GameSettingsData persistence — see Sound.cs's
        // RefreshMusicState()/ShouldPlaySfx() for how these actually gate
        // playback, and SettingsState.cs's Audio tab for the controls.
        // MusicEnabled/MusicVolumePercent/SfxVolumePercent all default to
        // their real intended values directly (matching what Sound.cs
        // already hardcoded before these settings existed), same as
        // LowHealthIndicatorEnabled/LowHealthThresholdPercent above.
        public bool MusicEnabled = true;
        public int MusicVolumePercent = 25;
        public bool MusicMuted;
        public int SfxVolumePercent = 100;
        public bool SfxMuted;
        public bool WeaponShotsMuted;

        // Set once this class first reaches the level cap (20) and never
        // cleared again — same permanent-through-death/delete treatment as
        // HighScore (see DeleteCharacterData/GameOverState). No longer feeds
        // ComputeStars() below (Class Quests are Fame-based now, not gated
        // on reaching 20), but still drives the Level-20 XP-icon switch
        // (Enemy.WasShot()). Level itself resets to 1 on death/delete, so
        // this needs its own persisted flag rather than checking Level
        // directly.
        public bool HasReachedLevel20;

        // "Class Quests" — five tiers, attained purely by cumulative Fame
        // earned during a character's lifetime (no Level-20 gate, unlike the
        // star system this replaced). Fed by ComputeBaseFame(HighScore) —
        // HighScore is the permanent best-ever ExperienceTotal (survives
        // death/delete), and Base Fame is a monotonic function of XP, so
        // "the most Base Fame this character ever displayed" is exactly
        // ComputeBaseFame(HighScore), with no separate persisted star count
        // needed. Public/static — shared by CharacterSelectState (display,
        // permanent record) and RealmState (detecting a threshold crossing
        // to persist immediately, see UpdateHighScore() below) rather than
        // living only in the UI.
        //
        // Note this means a character can now earn Star 1 (20 Fame, ≈18,000
        // XP under the current leveling curve) slightly before actually
        // reaching Level 20 (≈19,850 XP) — a deliberate reading of "gaining
        // certain amounts of Fame during your character's lifetime", not an
        // oversight.
        public static readonly int[] ClassQuestFameThresholds = { 20, 500, 1500, 5000, 15000 };
        public const int MaxStars = 5;

        public static int ComputeStars(int highScore)
        {
            int fame = ComputeBaseFame(highScore);
            int stars = 0;
            for (int i = 0; i < ClassQuestFameThresholds.Length; i++)
            {
                if (fame < ClassQuestFameThresholds[i])
                    break;
                stars = i + 1;
            }

            return stars;
        }

        public int Level;

        public Weapon Weapon;
        public Armor Armor;
        public Ring Ring;
        public AbilityItem AbilityItem;

        public Texture2D Texture;

        public enum Class
        {
            Wizard, // 0
            Archer, // 1
            Knight, // 2
            Priest, // 3
        }

        public static Class PlayerClass { get; set; }

        public Weapon.WeaponType WeaponType { get; set; }
        public Armor.ArmorType ArmorType { get; set; }

        public Player()
        {
            ID = Guid.NewGuid();

            Opacity = 1f;

            Inventory = new InventorySystem();

            instance = this;

            ExperienceTotal = 0;

            Level = 1;

            Position = new Vector2(Game1.WorldWidth / 2, Game1.WorldHeight / 2);

            Weapon = new Weapon();
            Armor = new Armor();
            Ring = new Ring();
            AbilityItem = new AbilityItem();

            // Radius is not accurate,
            // Archer is smaller.
            Radius = 64 / 2f;
        }

        // Level++ happens in the subclass override, before it calls
        // RecalculateStats() — see Wizard/Archer.LevelUp().
        public virtual void LevelUp()
        {
            Health = HealthMax;
            Mana = ManaMax;

            // Persisted the instant Star 1 is actually earned, rather than
            // waiting for whatever save checkpoint happens to come next
            // (death, leaving to the Nexus, entering another dungeon...) —
            // otherwise closing the game right after hitting 20 without
            // triggering one of those first would silently lose the star.
            if (Level >= 20 && !HasReachedLevel20)
            {
                HasReachedLevel20 = true;
                Util.SavePlayerData();
                Util.SaveInventoryData();
                Util.SaveBankData();
                Util.SaveFameData();
            }

            Sound.Play(Sound.LevelUp, 0.4f);

            // Sparkling gold/white swirl rather than Particle.SpawnBurst()'s
            // straight-line scatter (used for the enemy hit/death effects
            // in Enemy.WasShot()) — a level up reads as a celebratory
            // flourish, not a combat reaction. () => Position (not a
            // captured Vector2) so the swirl keeps tracking the player if
            // they keep moving while it plays out.
            SwirlParticle.SpawnSwirl(
                () => Position,
                Microsoft.Xna.Framework.Color.Gold,
                Microsoft.Xna.Framework.Color.Silver,
                count: 48,
                lifespanTicks: 100,
                maxRadius: 80f,
                startScale: 0.18f
            );
        }

        // Shared with Overlay.DrawSidebar's ability section, so the
        // HUD and the actual gate in UseAbility() can never drift apart. Set
        // directly by the equipped AbilityItem (Spell/Quiver/Shield),
        // floored at 1 so it can never reach free (and so Overlay's
        // Mana * 100 / AbilityCost readiness calc can't divide by zero).
        public virtual int AbilityCost => Math.Max(1, AbilityItem.ManaCost);

        public virtual void UseAbility() { }

        // Overridden per class (e.g. Wizard: item is Spell) — no shared enum,
        // since each future player class gets its own AbilityItem subclass
        // rather than another enum value.
        public virtual bool CanEquipAbilityItem(AbilityItem item) => false;

        private void Shoot()
        {
            if (!Weapon.IsEquipped)
            {
                Sound.Play(Sound.Error, 0.4f);
                return;
            }

            Weapon.Shoot();
        }

        public void Hit(int damage = 25)
        {
            // Damage Reduction (e.g. Knight's Shield Slam) scales the raw
            // hit before Defense's own reduction/floor below — the two
            // stack rather than one replacing the other.
            damage = (int)(damage * DamageTakenMultiplier);

            // Vital Combat: checked against the raw hit, before Defense
            // reduces it below — see RegisterHit()'s own comment for why.
            RegisterHit(damage);

            int damageModified = damage - Defense;
            if (damageModified <= damage / 10)
            {
                damageModified = damage / 10;
            }

            if (ShowPlayerDamageNumbersEnabled)
            {
                EntityManager.Add(
                    new DamageNumber(
                        Position,
                        damageModified,
                        Microsoft.Xna.Framework.Color.Red,
                        followsPlayer: true
                    )
                );
            }

            Health = Health - damageModified;
            if (Health <= 0)
            {
                Kill();
            }
            else
            {
                Sound.Play(Sound.PlayerHit, 0.45f);
            }
        }

        // Slows this player, halving movement speed (Update() above) for
        // durationFrames. Backed by Entity's general debuff system, same
        // shape as Enemy.Paralyze()/Stun(). Default is 3 seconds at 60fps.
        public void Slow(int durationFrames = 180)
        {
            ApplyDebuff(DebuffType.Slow, durationFrames);
        }

        public void EquipWeapon(Weapon newWeapon)
        {
            Weapon = newWeapon;
            RecalculateStats();
            ClampVitals();
        }

        public void EquipArmor(Armor newArmor)
        {
            Armor = newArmor;
            RecalculateStats();
            ClampVitals();
        }

        public void EquipRing(Ring newRing)
        {
            Ring = newRing;
            RecalculateStats();
            ClampVitals();
        }

        public void EquipAbilityItem(AbilityItem newAbilityItem)
        {
            AbilityItem = newAbilityItem;
            RecalculateStats();
            ClampVitals();
        }

        // Debug/testing only (F4 in Input.cs) — not real gameplay. Sets
        // Level straight to the cap and calls RecalculateStats() so
        // Attack/Defense/etc. actually reflect it (every class's
        // RecalculateStats() derives them from Level — see
        // CharacterClasses/Wizard.cs etc.). Also equips the current class's
        // highest-tier item for every equipment slot, replacing whatever
        // was equipped before — each is a fresh instance built from the
        // matching catalog entry's data fields, same as Weapon.LoadWeapon()/
        // Armor.LoadArmor()/etc. already do. Health/Mana are topped off last
        // (same as LevelUp()'s own Health = HealthMax/Mana = ManaMax), after
        // gear is equipped rather than right after RecalculateStats() —
        // equipping can itself raise HealthMax/ManaMax further (e.g. a
        // higher-tier armor's MaxHealthBonus), and topping off first would
        // leave Health/Mana stuck below the true final max once that gear
        // lands.
        public void DebugMaxLevelAndEquipTopGear()
        {
            Level = 20;
            RecalculateStats();

            EquipHighestTierWeapon();
            EquipHighestTierArmor();
            EquipHighestTierRing();
            EquipHighestTierAbilityItem();

            Health = HealthMax;
            Mana = ManaMax;
        }

        // Debug/testing only (F4 in Input.cs) — sets ExperienceTotal (and
        // HighScore, so Character Select's permanent star record reflects
        // it immediately, without needing an actual RealmState.Update()
        // tick to sync HighScore from ExperienceTotal the way real play
        // does) to the exact XP needed for 3 stars — ClassQuestFameThresholds
        // index 2, "3 stars" per ComputeStars() above. Handy for testing the
        // class-unlock chain (CharacterSelectState.cs), which requires 3
        // stars in the previous class to unlock the next one. Never lowers
        // either value if this character already has more.
        public void DebugGrantThreeStarsFame()
        {
            int requiredXp = ExperienceForBaseFame(ClassQuestFameThresholds[2]);
            ExperienceTotal = Math.Max(ExperienceTotal, requiredXp);
            HighScore = Math.Max(HighScore, ExperienceTotal);
        }

        private void EquipHighestTierWeapon()
        {
            Weapon best = Game1
                .Instance.Weapons.Where(w => w.Type == WeaponType)
                .OrderByDescending(w => w.Tier)
                .FirstOrDefault();
            if (best == null)
                return;

            Texture2D weaponTexture = Game1.Instance.Content.Load<Texture2D>(best.ImageName);
            Texture2D projectileTexture = Game1.Instance.Content.Load<Texture2D>(
                best.ProjectileImageName
            );
            // Bow-only — every other weapon type's SideProjectileImageName
            // is null, and Content.Load can't take a null asset name.
            Texture2D sideProjectileTexture =
                best.SideProjectileImageName != null
                    ? Game1.Instance.Content.Load<Texture2D>(best.SideProjectileImageName)
                    : null;
            Weapon copy = new(weaponTexture, projectileTexture)
            {
                Type = best.Type,
                Name = best.Name,
                Description = best.Description,
                Tier = best.Tier,
                DamageMin = best.DamageMin,
                DamageMax = best.DamageMax,
                ProjectileMagnitude = best.ProjectileMagnitude,
                ProjectileDuration = best.ProjectileDuration,
                ImageName = best.ImageName,
                ProjectileImageName = best.ProjectileImageName,
                Amplitude = best.Amplitude,
                Frequency = best.Frequency,
                SideDamageMin = best.SideDamageMin,
                SideDamageMax = best.SideDamageMax,
                SideProjectileImage = sideProjectileTexture,
                SideProjectileImageName = best.SideProjectileImageName,
                ArcGapDegrees = best.ArcGapDegrees,
            };

            EquipWeapon(copy);
        }

        private void EquipHighestTierArmor()
        {
            Armor best = Game1
                .Instance.Armors.Where(a => a.Type == ArmorType)
                .OrderByDescending(a => a.Tier)
                .FirstOrDefault();
            if (best == null)
                return;

            Texture2D armorTexture = Game1.Instance.Content.Load<Texture2D>(best.ImageName);
            Armor copy = new(armorTexture)
            {
                Name = best.Name,
                Description = best.Description,
                Type = best.Type,
                Tier = best.Tier,
                MaxHealthBonus = best.MaxHealthBonus,
                MaxManaBonus = best.MaxManaBonus,
                AttackBonus = best.AttackBonus,
                DefenseBonus = best.DefenseBonus,
                SpeedBonus = best.SpeedBonus,
                DexterityBonus = best.DexterityBonus,
                VitalityBonus = best.VitalityBonus,
                WisdomBonus = best.WisdomBonus,
                ImageName = best.ImageName,
            };

            EquipArmor(copy);
        }

        private void EquipHighestTierRing()
        {
            // No class restriction on Ring, same as Ring.LoadRing().
            Ring best = Game1.Instance.Rings.OrderByDescending(r => r.Tier).FirstOrDefault();
            if (best == null)
                return;

            Texture2D ringTexture = Game1.Instance.Content.Load<Texture2D>(best.ImageName);
            Ring copy = new(ringTexture)
            {
                Name = best.Name,
                Description = best.Description,
                Tier = best.Tier,
                MaxHealthBonus = best.MaxHealthBonus,
                MaxManaBonus = best.MaxManaBonus,
                AttackBonus = best.AttackBonus,
                DefenseBonus = best.DefenseBonus,
                SpeedBonus = best.SpeedBonus,
                DexterityBonus = best.DexterityBonus,
                VitalityBonus = best.VitalityBonus,
                WisdomBonus = best.WisdomBonus,
                ImageName = best.ImageName,
            };

            EquipRing(copy);
        }

        // Spell/Quiver/Shield share an identical field set (only their
        // runtime type differs, which is what CanEquipAbilityItem() actually
        // gates on) — picked from all three catalogs combined via the same
        // CanEquipAbilityItem() filter AbilityItem.PlaceholderImage already
        // uses, rather than three near-duplicate class-specific methods.
        private void EquipHighestTierAbilityItem()
        {
            AbilityItem best = Game1
                .Instance.Spells.Cast<AbilityItem>()
                .Concat(Game1.Instance.Quivers)
                .Concat(Game1.Instance.Shields)
                .Concat(Game1.Instance.Tomes)
                .Where(item => CanEquipAbilityItem(item))
                .OrderByDescending(item => item.Tier)
                .FirstOrDefault();
            if (best == null)
                return;

            Texture2D texture = Game1.Instance.Content.Load<Texture2D>(best.ImageName);
            AbilityItem copy = best switch
            {
                Spell => new Spell(texture),
                Quiver => new Quiver(texture),
                Shield => new Shield(texture),
                Tome => new Tome(texture),
                _ => null,
            };
            if (copy == null)
                return;

            copy.Name = best.Name;
            copy.Description = best.Description;
            copy.Tier = best.Tier;
            copy.MaxHealthBonus = best.MaxHealthBonus;
            copy.MaxManaBonus = best.MaxManaBonus;
            copy.AttackBonus = best.AttackBonus;
            copy.DefenseBonus = best.DefenseBonus;
            copy.SpeedBonus = best.SpeedBonus;
            copy.DexterityBonus = best.DexterityBonus;
            copy.VitalityBonus = best.VitalityBonus;
            copy.WisdomBonus = best.WisdomBonus;
            copy.ManaCost = best.ManaCost;
            copy.MinDamage = best.MinDamage;
            copy.MaxDamage = best.MaxDamage;
            copy.ImageName = best.ImageName;
            copy.XpBonusPercent = best.XpBonusPercent;

            // Quiver-only — its ability shot's own fields, not shared by
            // Spell/Shield. See Data/QuiverData.cs.
            if (copy is Quiver quiverCopy && best is Quiver quiverBest)
            {
                quiverCopy.Shots = quiverBest.Shots;
                quiverCopy.ArcGapDegrees = quiverBest.ArcGapDegrees;
                quiverCopy.ProjectileMagnitude = quiverBest.ProjectileMagnitude;
                quiverCopy.ProjectileDuration = quiverBest.ProjectileDuration;
                quiverCopy.ProjectileImage = Game1.Instance.Content.Load<Texture2D>(
                    quiverBest.ProjectileImageName
                );
                quiverCopy.ProjectileImageName = quiverBest.ProjectileImageName;
            }

            // Shield-only — Shield Slam's shot fan. See Data/ShieldData.cs.
            if (copy is Shield shieldCopy && best is Shield shieldBest)
            {
                shieldCopy.Shots = shieldBest.Shots;
                shieldCopy.ArcGapDegrees = shieldBest.ArcGapDegrees;
            }

            // Tome-only — its Heal/Healing/Range fields. See Data/TomeData.cs.
            if (copy is Tome tomeCopy && best is Tome tomeBest)
            {
                tomeCopy.Range = tomeBest.Range;
                tomeCopy.HealAmount = tomeBest.HealAmount;
                tomeCopy.HealingAmountPerSecond = tomeBest.HealingAmountPerSecond;
                tomeCopy.HealingDurationSeconds = tomeBest.HealingDurationSeconds;
            }

            EquipAbilityItem(copy);
        }

        // Sum of whichever equipped item(s) carry this bonus — always read
        // live from Weapon/Armor/Ring/AbilityItem rather than tracked
        // separately, so there's no accumulator to keep in sync (and nothing
        // to double-count when the same gear gets re-equipped, e.g. on
        // save/reload). Public (unlike the bonuses below, which
        // RecalculateStats() folds straight into HealthMax/ManaMax) — read
        // directly by Overlay.DrawHealthSection()/DrawManaSection() to show
        // the "(+N)" next to the bar numbers, same reasoning as
        // EquipmentXpBonusPercent below.
        public int EquipmentMaxHealthBonus =>
            Weapon.MaxHealthBonus
            + Armor.MaxHealthBonus
            + Ring.MaxHealthBonus
            + AbilityItem.MaxHealthBonus;
        public int EquipmentMaxManaBonus =>
            Weapon.MaxManaBonus + Armor.MaxManaBonus + Ring.MaxManaBonus + AbilityItem.MaxManaBonus;
        // Public (same reasoning as EquipmentMaxHealthBonus/EquipmentMaxManaBonus
        // above) — read directly by Overlay.DrawStats() to show each stat's
        // gear contribution as a gold "+N" next to it.
        public int EquipmentAttackBonus =>
            Weapon.AttackBonus + Armor.AttackBonus + Ring.AttackBonus + AbilityItem.AttackBonus;
        public int EquipmentDefenseBonus =>
            Weapon.DefenseBonus + Armor.DefenseBonus + Ring.DefenseBonus + AbilityItem.DefenseBonus;
        public float EquipmentSpeedBonus =>
            Weapon.SpeedBonus + Armor.SpeedBonus + Ring.SpeedBonus + AbilityItem.SpeedBonus;
        public int EquipmentDexterityBonus =>
            Weapon.DexterityBonus
            + Armor.DexterityBonus
            + Ring.DexterityBonus
            + AbilityItem.DexterityBonus;
        public int EquipmentVitalityBonus =>
            Weapon.VitalityBonus
            + Armor.VitalityBonus
            + Ring.VitalityBonus
            + AbilityItem.VitalityBonus;
        public int EquipmentWisdomBonus =>
            Weapon.WisdomBonus + Armor.WisdomBonus + Ring.WisdomBonus + AbilityItem.WisdomBonus;

        // Public (unlike the bonuses above, which RecalculateStats() folds
        // into a real stat) — read directly by Enemy.WasShot()'s death
        // branch to scale XP gained. Only Tome sets this nonzero today.
        public float EquipmentXpBonusPercent =>
            Weapon.XpBonusPercent
            + Armor.XpBonusPercent
            + Ring.XpBonusPercent
            + AbilityItem.XpBonusPercent;

        // Each stat minus whatever equipment/temporary bonuses are currently
        // folded into it — i.e. the level+potion value alone. "Permanent"
        // since that's exactly what distinguishes these two excluded sources
        // from the level/potion value: equipment can be unequipped and a
        // temporary bonus expires on its own, but level-ups and potions
        // never go away. Used by Overlay.DrawStats() to decide whether to
        // highlight a stat as "maxed", and by InventorySystem's stat-potion
        // gating, to decide whether a potion would actually do anything:
        // gear or a timed buff pushing the displayed number above MaxAttack
        // etc. shouldn't count as actually being maxed, and shouldn't block
        // a potion that would otherwise still raise the permanent value.
        // (Can't call these "BaseX" — Vitality already has a same-named
        // field for its level-1 starting value.)
        public int PermanentAttack => Attack - EquipmentAttackBonus - TemporaryAttackBonus;
        public int PermanentDefense => Defense - EquipmentDefenseBonus - TemporaryDefenseBonus;
        public float PermanentSpeed => Speed - EquipmentSpeedBonus - TemporarySpeedBonus;
        public int PermanentDexterity =>
            Dexterity - EquipmentDexterityBonus - TemporaryDexterityBonus;
        public int PermanentVitality => Vitality - EquipmentVitalityBonus - TemporaryVitalityBonus;
        public int PermanentWisdom => Wisdom - EquipmentWisdomBonus - TemporaryWisdomBonus;

        // HealthMax/ManaMax used to be the one pair of "stats" with no
        // equipment/temporary component at all (see PotionHealthMaxBonus
        // above) — now that equipment/temporary bonuses can push them past
        // MaxHealth/MaxMana too, matching every other stat, they need this
        // same Permanent split for the same two reasons as above.
        public int PermanentHealthMax =>
            HealthMax - EquipmentMaxHealthBonus - TemporaryHealthMaxBonus;
        public int PermanentManaMax => ManaMax - EquipmentMaxManaBonus - TemporaryManaMaxBonus;

        // Recomputes every derived stat from this class's base-at-current-level
        // formula, plus permanent potion bonuses, plus whatever's currently
        // equipped. Called on level-up, on equip/unequip, and after drinking a
        // stat potion — anywhere one of those inputs changes.
        public virtual void RecalculateStats() { }

        // Starts (or refreshes) a timed stat bonus. Re-triggering while one's
        // already active just resets the countdown rather than stacking the
        // amount — the bonus is only (re-)applied when starting fresh from
        // zero remaining frames.
        public void AddTemporaryAttackBonus(int amount, int durationFrames)
        {
            if (temporaryAttackBonusFrames <= 0)
                TemporaryAttackBonus = amount;
            temporaryAttackBonusFrames = durationFrames;
            RecalculateStats();
        }

        public void AddTemporaryDefenseBonus(int amount, int durationFrames)
        {
            if (temporaryDefenseBonusFrames <= 0)
                TemporaryDefenseBonus = amount;
            temporaryDefenseBonusFrames = durationFrames;
            RecalculateStats();
        }

        // No RecalculateStats() call — DamageTakenMultiplier isn't one of
        // the stats that formula derives; it's read directly by Hit() below.
        public void AddTemporaryDamageTakenMultiplier(float multiplier, int durationFrames)
        {
            if (damageTakenMultiplierFrames <= 0)
                DamageTakenMultiplier = multiplier;
            damageTakenMultiplierFrames = durationFrames;
        }

        // "Multiple Red Cross Healing effects do not stack, with the
        // strongest one overriding all others" — a weaker application while
        // a stronger one is still active is ignored entirely (neither the
        // rate nor the remaining duration changes); a stronger (or equal)
        // application always takes over, resetting the duration to its own.
        public void ApplyHealing(float amountPerSecond, int durationFrames)
        {
            if (healingDurationFrames > 0 && amountPerSecond < HealingAmountPerSecond)
                return;

            HealingAmountPerSecond = amountPerSecond;
            healingDurationFrames = durationFrames;

            // Purely cosmetic — the floating icon above the player, driven by
            // Entity's own generic debuff-timer/indicator system (which
            // already ticks down and draws automatically once applied). The
            // real rate/duration/strongest-wins logic above is Player-only
            // and stays that way: DebuffType has no room for a magnitude,
            // only a duration, so it can't be the source of truth here — it
            // only mirrors the same duration for display, applied in lockstep
            // with (never instead of) the fields above.
            ApplyDebuff(DebuffType.Healing, durationFrames);
        }

        // Instant, flat self-heal — separate from ApplyHealing()'s
        // over-time rate above (e.g. a Priest's Tome applies both from the
        // same cast: an immediate chunk plus the HoT).
        public void Heal(int amount)
        {
            Health = Math.Min(Health + amount, HealthMax);
        }

        public void AddTemporarySpeedBonus(float amount, int durationFrames)
        {
            if (temporarySpeedBonusFrames <= 0)
                TemporarySpeedBonus = amount;
            temporarySpeedBonusFrames = durationFrames;
            RecalculateStats();
        }

        public void AddTemporaryDexterityBonus(int amount, int durationFrames)
        {
            if (temporaryDexterityBonusFrames <= 0)
                TemporaryDexterityBonus = amount;
            temporaryDexterityBonusFrames = durationFrames;
            RecalculateStats();
        }

        public void AddTemporaryVitalityBonus(int amount, int durationFrames)
        {
            if (temporaryVitalityBonusFrames <= 0)
                TemporaryVitalityBonus = amount;
            temporaryVitalityBonusFrames = durationFrames;
            RecalculateStats();
        }

        public void AddTemporaryWisdomBonus(int amount, int durationFrames)
        {
            if (temporaryWisdomBonusFrames <= 0)
                TemporaryWisdomBonus = amount;
            temporaryWisdomBonusFrames = durationFrames;
            RecalculateStats();
        }

        public void AddTemporaryHealthMaxBonus(int amount, int durationFrames)
        {
            if (temporaryHealthMaxBonusFrames <= 0)
                TemporaryHealthMaxBonus = amount;
            temporaryHealthMaxBonusFrames = durationFrames;
            RecalculateStats();
        }

        public void AddTemporaryManaMaxBonus(int amount, int durationFrames)
        {
            if (temporaryManaMaxBonusFrames <= 0)
                TemporaryManaMaxBonus = amount;
            temporaryManaMaxBonusFrames = durationFrames;
            RecalculateStats();
        }

        // Ticks every active timed bonus's countdown down by one frame,
        // zeroing (and re-triggering RecalculateStats() for) whichever ones
        // hit zero this frame. Called from Update() every frame, so it
        // applies to every class automatically rather than each ability
        // needing its own expiry-tracking Update() override.
        private void UpdateTemporaryBonuses()
        {
            bool expired = false;

            if (temporaryAttackBonusFrames > 0 && --temporaryAttackBonusFrames == 0)
            {
                TemporaryAttackBonus = 0;
                expired = true;
            }
            if (temporaryDefenseBonusFrames > 0 && --temporaryDefenseBonusFrames == 0)
            {
                TemporaryDefenseBonus = 0;
                expired = true;
            }
            if (temporarySpeedBonusFrames > 0 && --temporarySpeedBonusFrames == 0)
            {
                TemporarySpeedBonus = 0;
                expired = true;
            }
            if (temporaryDexterityBonusFrames > 0 && --temporaryDexterityBonusFrames == 0)
            {
                TemporaryDexterityBonus = 0;
                expired = true;
            }
            if (temporaryVitalityBonusFrames > 0 && --temporaryVitalityBonusFrames == 0)
            {
                TemporaryVitalityBonus = 0;
                expired = true;
            }
            if (temporaryWisdomBonusFrames > 0 && --temporaryWisdomBonusFrames == 0)
            {
                TemporaryWisdomBonus = 0;
                expired = true;
            }
            if (temporaryHealthMaxBonusFrames > 0 && --temporaryHealthMaxBonusFrames == 0)
            {
                TemporaryHealthMaxBonus = 0;
                expired = true;
            }
            if (temporaryManaMaxBonusFrames > 0 && --temporaryManaMaxBonusFrames == 0)
            {
                TemporaryManaMaxBonus = 0;
                expired = true;
            }

            if (expired)
                RecalculateStats();

            // Not part of RecalculateStats() — ticked down separately.
            if (damageTakenMultiplierFrames > 0 && --damageTakenMultiplierFrames == 0)
                DamageTakenMultiplier = 1f;

            if (healingDurationFrames > 0 && --healingDurationFrames == 0)
                HealingAmountPerSecond = 0f;

            // Vital Combat: ticks down independently of RecalculateStats()
            // above, same as damageTakenMultiplierFrames/
            // healingDurationFrames — InCombat isn't a stat bonus, just a
            // gate HealthRegenPerSecond/ManaRegenPerSecond/RegisterHit()
            // read directly.
            if (inCombatFrames > 0 && --inCombatFrames == 0)
                InCombat = false;
        }

        private void ClampVitals()
        {
            Health = Math.Min(Health, HealthMax);
            Mana = Math.Min(Mana, ManaMax);
        }

        public void Kill()
        {
            EnemySpawner.Reset();
            EntityManager.Reset();
            Position = Game1.WorldSize / 2;
            Camera.Reset();
            StateManager.GameOver();
        }

        // Float accumulators, same reasoning/pattern as projectileCooldown
        // below — HealthRegenPerSecond/ManaRegenPerSecond need to land on
        // real fractional values (e.g. 11.63 HP/sec at 40 Vitality), and an
        // int-tick-count threshold with a reset-to-0 discards the leftover
        // fraction every cycle instead of carrying it forward.
        private float healthCooldown = 0f;

        private float manaCooldown = 0f;

        // Float accumulator, not the int-tick-count style the other
        // cooldowns above use — AttacksPerSecond needs to land on real
        // fractional values (e.g. 5.833 A/s at 50 Dexterity), which an
        // integer per-tick increment can't represent without rounding error
        // compounding over time. Fires once this reaches 1.0 ("one whole
        // attack accumulated"), then resets to 0.
        private float projectileCooldown = 0f;

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(
                image,
                Position,
                null,
                color * Opacity,
                Orientation,
                Size / 2f,
                1f,
                0,
                0
            );

            DrawLowHealthBar(spriteBatch);
            DrawTemporaryBonusIndicators(spriteBatch);
            DrawDebuffIndicators(spriteBatch);
        }

        // Small bar centered beneath the sprite, only while IsLowHealth —
        // same "stretched Art.HealthBar rect" technique Overlay.cs's own
        // sidebar health bar already uses (a 1x1 pixel texture, sized via
        // the source rectangle's Width/Height rather than actual sampled
        // pixel content), just a compact in-world version rather than a
        // fixed HUD element. Proportional to Health/HealthMax (not to the
        // threshold), so a fuller bar reads as "closer to the threshold"
        // and an emptier one as "closer to death," same as any other
        // health bar.
        private void DrawLowHealthBar(SpriteBatch spriteBatch)
        {
            if (!IsLowHealth)
                return;

            float fraction = MathHelper.Clamp(Health / (float)HealthMax, 0f, 1f);
            Vector2 barPos = new(
                Position.X - LowHealthBarWidth / 2f,
                Position.Y + Size.Y / 2f + LowHealthBarOffsetY
            );

            spriteBatch.Draw(
                Art.HealthBar,
                barPos,
                new Microsoft.Xna.Framework.Rectangle(0, 0, LowHealthBarWidth, LowHealthBarHeight),
                Microsoft.Xna.Framework.Color.Black * 0.6f
            );
            spriteBatch.Draw(
                Art.HealthBar,
                barPos,
                new Microsoft.Xna.Framework.Rectangle(0, 0, (int)(LowHealthBarWidth * fraction), LowHealthBarHeight),
                Microsoft.Xna.Framework.Color.Red
            );
        }

        // One "+" above the sprite per active temporary bonus that has an
        // assigned color, color-coded to which stat it is, side by side if
        // more than one is active at once. Vitality/Wisdom/HealthMax/ManaMax
        // temporary bonuses work the same as the other four but have no
        // color assigned yet, so they don't draw an indicator here.
        private void DrawTemporaryBonusIndicators(SpriteBatch spriteBatch)
        {
            List<Microsoft.Xna.Framework.Color> activeColors = [];

            if (TemporaryAttackBonus != 0)
                activeColors.Add(Microsoft.Xna.Framework.Color.Pink);
            if (TemporaryDefenseBonus != 0)
                activeColors.Add(Microsoft.Xna.Framework.Color.Gray);
            if (TemporarySpeedBonus != 0)
                activeColors.Add(Microsoft.Xna.Framework.Color.Green);
            if (TemporaryDexterityBonus != 0)
                activeColors.Add(Microsoft.Xna.Framework.Color.Orange);
            if (TemporaryVitalityBonus != 0)
                activeColors.Add(Microsoft.Xna.Framework.Color.Red);
            if (TemporaryWisdomBonus != 0)
                activeColors.Add(Microsoft.Xna.Framework.Color.Blue);

            if (activeColors.Count == 0)
                return;

            const string symbol = "+";
            Vector2 symbolSize = Art.RetroFont.MeasureString(symbol);
            float totalWidth = symbolSize.X * activeColors.Count;
            float startX = Position.X - (totalWidth / 2f);
            float y = Position.Y - (Size.Y / 2f) - symbolSize.Y - 4;

            for (int i = 0; i < activeColors.Count; i++)
            {
                Util.DrawOutlinedText(
                    spriteBatch,
                    Art.RetroFont,
                    symbol,
                    new Vector2(startX + (i * symbolSize.X), y),
                    activeColors[i]
                );
            }
        }

        // Settings > Graphics > "Low Health Indicator" — flashes the
        // player sprite red once Health drops under LowHealthThresholdPercent
        // of HealthMax, speeding up the closer Health gets to 0. Same
        // accumulating-phase shape as LootBag's own despawn-warning blink
        // (see LootBag.cs) — the per-tick phase increment (1 / halfPeriod)
        // grows as Health approaches 0, so the flash visibly speeds up
        // rather than blinking at one constant rate for the whole time
        // it's active.
        private const float LowHealthSlowFlashHalfPeriodTicks = 20f;
        private const float LowHealthFastFlashHalfPeriodTicks = 5f;
        private float lowHealthFlashPhase = 0f;

        // Below-sprite bar shown under the same condition as the flash
        // above (LowHealthIndicatorEnabled + under threshold) — see
        // DrawLowHealthBar(), called from Draw().
        private const int LowHealthBarWidth = 40;
        private const int LowHealthBarHeight = 6;
        private const int LowHealthBarOffsetY = 8;

        // Shared by both the flash (Update()) and the bar (DrawLowHealthBar())
        // so the two conditions can never drift apart.
        private bool IsLowHealth =>
            LowHealthIndicatorEnabled
            && HealthMax > 0
            && Health < HealthMax * (LowHealthThresholdPercent / 100f);

        public override void Update()
        {
            // Update position. slowMultiplier halves speed while Slowed
            // (Entity's general debuff system) is active — e.g. a Stheno
            // Pet's trailing orb.
            float slowMultiplier = HasDebuff(DebuffType.Slow) ? 0.5f : 1f;
            // TilesPerSecond * 32px/tile / 60 ticks/sec (MonoGame's default
            // fixed timestep) converts the tiles/sec formula into this
            // engine's px/tick Velocity unit — no int truncation, unlike the
            // old formula, which threw away real precision (e.g. rounded a
            // true 5.7333 px/tick down to 5 at 50 Speed).
            float pixelsPerTick = TilesPerSecond * 32f / 60f;
            Velocity = pixelsPerTick * slowMultiplier * Input.GetMovementDirection();
            Position += Velocity;

            // Update camera position. Syncs directly to the player's actual
            // position rather than accumulating the same Velocity
            // separately — the two are otherwise independent state, so if
            // Camera.Pos's own boundary clamp (see Camera.cs) ever kicks in
            // (e.g. near a bounded instance's edge), a permanent gap opens
            // between camera and player that repeated += Velocity can never
            // close again, even after moving back away from the edge.
            Game1.Camera.Pos = Position;

            // Check for level up.
            if (Level < 20 && ExperienceTotal >= ExperienceNextLevel)
            {
                LevelUp();
            }

            // Regenerate Health. HealthRegenPerSecond / 60 is the fraction
            // of one HP completed this tick; subtracting 1 (not resetting
            // to 0) on regen carries the leftover fraction into the next
            // cycle instead of discarding it, same precision fix as
            // AttacksPerSecond/TilesPerSecond above.
            healthCooldown += HealthRegenPerSecond / 60f;
            if (healthCooldown >= 1f)
            {
                healthCooldown -= 1f;
                if (Health < HealthMax)
                    Health++;
            }

            // Low-health flash (see the fields above Update()).
            if (IsLowHealth)
            {
                float threshold = HealthMax * (LowHealthThresholdPercent / 100f);
                float progress = MathHelper.Clamp(1f - (Health / threshold), 0f, 1f);
                float halfPeriod = MathHelper.Lerp(
                    LowHealthSlowFlashHalfPeriodTicks,
                    LowHealthFastFlashHalfPeriodTicks,
                    progress
                );
                lowHealthFlashPhase += 1f / halfPeriod;
                color =
                    ((int)lowHealthFlashPhase % 2) == 0
                        ? Microsoft.Xna.Framework.Color.White
                        : Microsoft.Xna.Framework.Color.Red;
            }
            else
            {
                color = Microsoft.Xna.Framework.Color.White;
                lowHealthFlashPhase = 0f;
            }

            // Regenerate mana. Same fraction-per-tick/carry-forward pattern
            // as Health above.
            manaCooldown += ManaRegenPerSecond / 60f;
            if (manaCooldown >= 1f)
            {
                manaCooldown -= 1f;
                if (Mana < ManaMax)
                    Mana++;
            }

            // Shoot
            // This may be moved to new Weapon class.
            // AttacksPerSecond / 60 is the fraction of one attack completed
            // this tick (60 ticks/sec, MonoGame's default fixed timestep).
            // Only accumulates while actually trying to fire — accumulating
            // unconditionally (the old behavior) let cooldown bank up
            // indefinitely while idle, so the first click after any pause
            // fired instantly regardless of DEX, which isn't "attacks per
            // second" at all. Subtracting 1 (not resetting to 0) on fire
            // carries the leftover fraction into the next cycle instead of
            // discarding it — resetting to 0 systematically undercounts real
            // fire rate whenever 1.0 isn't an exact multiple of the per-tick
            // increment (confirmed: was firing ~54 times over 600 ticks at
            // 50 DEX where the formula calls for ~58.3 — a ~7% shortfall).
            if (Input.mouse.LeftButton == ButtonState.Pressed || AutoFireEnabled)
            {
                projectileCooldown += AttacksPerSecond / 60f;
                if (projectileCooldown >= 1f)
                {
                    projectileCooldown -= 1f;
                    Shoot();
                }
            }

            // Update weapon.
            this.Weapon.Update();

            // Update armor.
            this.Armor.Update();

            // Update ring.
            this.Ring.Update();

            // Update ability item.
            this.AbilityItem.Update();

            // Update
            this.Inventory.Update();

            // Tick down any active timed stat bonuses (e.g. Knight's Shield
            // Slam), expiring whichever hit zero this frame.
            UpdateTemporaryBonuses();

            // Tick down any active debuffs (Entity's general system — nothing
            // currently applies one to the player, but this keeps it live for
            // whenever something does).
            UpdateDebuffs();
        }
    }
}
