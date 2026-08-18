using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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

        public int Experience;
        public int ExperienceNextLevel;
        public int ExperienceTotal;
        public int HighScore;

        // See PlayerData.HasBeenPlayed — mirrors it on the live instance so a
        // later SavePlayerData() call doesn't regress it back to false.
        public bool HasBeenPlayed;

        // Set once this class first reaches the level cap (20) and never
        // cleared again — same permanent-through-death/delete treatment as
        // HighScore (see DeleteCharacterData/GameOverState), since this is
        // the Star 1 unlock condition for the Character Select star rating
        // (see ComputeStars() below). Level itself resets to 1 on
        // death/delete, so this needs its own persisted flag rather than
        // checking Level directly.
        public bool HasReachedLevel20;

        // Star 1 is HasReachedLevel20; each star beyond that needs HighScore
        // (permanent best-ever run Score, also survives death/delete) to
        // clear an exponentially rising bar. Public/static — shared by
        // CharacterSelectState (display) and RealmState (detecting a
        // threshold crossing to persist immediately, see UpdateHighScore()
        // below) rather than living only in the UI. Takes the two raw
        // inputs rather than a Player object so it works the same whether
        // the source is the live instance or a peeked save for a class
        // that isn't currently loaded.
        private const int Star2ScoreRequirement = 20000;
        private const int StarScoreMultiplier = 2;
        public const int MaxStars = 5;

        public static int ComputeStars(bool hasReachedLevel20, int highScore)
        {
            if (!hasReachedLevel20)
                return 0;

            int stars = 1;
            int requirement = Star2ScoreRequirement;
            for (int star = 2; star <= MaxStars; star++)
            {
                if (highScore < requirement)
                    break;

                stars = star;
                requirement *= StarScoreMultiplier;
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

            Experience = 0;
            ExperienceNextLevel = 50;
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

            Experience = 0;
            ExperienceNextLevel = 50 + ((Level * 2) * 50);

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
            int damageModified = damage - Defense;
            if (damageModified <= damage / 10)
            {
                damageModified = damage / 10;
            }

            EntityManager.Add(
                new DamageNumber(Position, damageModified, Microsoft.Xna.Framework.Color.Red)
            );

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

        // Sum of whichever equipped item(s) carry this bonus — always read
        // live from Weapon/Armor/Ring/AbilityItem rather than tracked
        // separately, so there's no accumulator to keep in sync (and nothing
        // to double-count when the same gear gets re-equipped, e.g. on
        // save/reload).
        protected int EquipmentMaxHealthBonus =>
            Weapon.MaxHealthBonus
            + Armor.MaxHealthBonus
            + Ring.MaxHealthBonus
            + AbilityItem.MaxHealthBonus;
        protected int EquipmentMaxManaBonus =>
            Weapon.MaxManaBonus + Armor.MaxManaBonus + Ring.MaxManaBonus + AbilityItem.MaxManaBonus;
        protected int EquipmentAttackBonus =>
            Weapon.AttackBonus + Armor.AttackBonus + Ring.AttackBonus + AbilityItem.AttackBonus;
        protected int EquipmentDefenseBonus =>
            Weapon.DefenseBonus + Armor.DefenseBonus + Ring.DefenseBonus + AbilityItem.DefenseBonus;
        protected float EquipmentSpeedBonus =>
            Weapon.SpeedBonus + Armor.SpeedBonus + Ring.SpeedBonus + AbilityItem.SpeedBonus;
        protected int EquipmentDexterityBonus =>
            Weapon.DexterityBonus
            + Armor.DexterityBonus
            + Ring.DexterityBonus
            + AbilityItem.DexterityBonus;
        protected int EquipmentVitalityBonus =>
            Weapon.VitalityBonus
            + Armor.VitalityBonus
            + Ring.VitalityBonus
            + AbilityItem.VitalityBonus;
        protected int EquipmentWisdomBonus =>
            Weapon.WisdomBonus + Armor.WisdomBonus + Ring.WisdomBonus + AbilityItem.WisdomBonus;

        // Each stat minus whatever equipment/temporary bonuses are currently
        // folded into it — i.e. the level+potion value alone, matching what
        // HealthMax/ManaMax already only count (see PotionHealthMaxBonus
        // above). "Permanent" since that's exactly what distinguishes these
        // two excluded sources from the level/potion value: equipment can be
        // unequipped and a temporary bonus expires on its own, but level-ups
        // and potions never go away. Used by Overlay.DrawStats() to decide
        // whether to highlight a stat as "maxed": gear or a timed buff
        // pushing the displayed number above MaxAttack etc. shouldn't count
        // as actually being maxed. (Can't call these "BaseX" — Vitality
        // already has a same-named field for its level-1 starting value.)
        public int PermanentAttack => Attack - EquipmentAttackBonus - TemporaryAttackBonus;
        public int PermanentDefense => Defense - EquipmentDefenseBonus - TemporaryDefenseBonus;
        public float PermanentSpeed => Speed - EquipmentSpeedBonus - TemporarySpeedBonus;
        public int PermanentDexterity =>
            Dexterity - EquipmentDexterityBonus - TemporaryDexterityBonus;
        public int PermanentVitality =>
            Vitality - EquipmentVitalityBonus - TemporaryVitalityBonus;
        public int PermanentWisdom => Wisdom - EquipmentWisdomBonus - TemporaryWisdomBonus;

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

        private int healthCooldown = 0;
        private int healthCooldownCount = 160;

        private int manaCooldown = 0;
        private int manaCooldownCount = 320;

        private int projectileCooldown = 0;
        private readonly int projectileCooldownCount = 240;

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

            DrawTemporaryBonusIndicators(spriteBatch);
            DrawDebuffIndicators(spriteBatch);
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
            Vector2 symbolSize = Art.HudFont.MeasureString(symbol);
            float totalWidth = symbolSize.X * activeColors.Count;
            float startX = Position.X - (totalWidth / 2f);
            float y = Position.Y - (Size.Y / 2f) - symbolSize.Y - 4;

            for (int i = 0; i < activeColors.Count; i++)
            {
                spriteBatch.DrawString(
                    Art.HudFont,
                    symbol,
                    new Vector2(startX + (i * symbolSize.X), y),
                    activeColors[i]
                );
            }
        }

        public override void Update()
        {
            // Update position.
            Velocity = (int)((Speed / 75) * 5.6 + 2) * Input.GetMovementDirection();
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
            if (Level < 20 && Experience >= ExperienceNextLevel)
            {
                LevelUp();
            }

            // Regenerate Health.
            healthCooldown += 1 + (int)(0.24 * Vitality);
            if (healthCooldown >= healthCooldownCount)
            {
                healthCooldown = 0;
                if (Health < HealthMax)
                    Health++;
            }

            // Regenerate mana.
            manaCooldown += 1 + (int)(0.12 * Wisdom);
            if (manaCooldown >= manaCooldownCount)
            {
                manaCooldown = 0;
                if (Mana < ManaMax)
                    Mana++;
            }

            // Shoot
            // This may be moved to new Weapon class.
            projectileCooldown += ((Dexterity * 100) / 150 * 100) / 100;
            if (
                Input.mouse.LeftButton == ButtonState.Pressed
                && projectileCooldown >= projectileCooldownCount
            )
            {
                projectileCooldown = 0;
                Shoot();
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
