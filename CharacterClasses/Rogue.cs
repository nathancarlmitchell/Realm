using Microsoft.Xna.Framework.Audio;

namespace Realm.CharacterClasses
{
    // The 5th playable class — https://www.realmeye.com/wiki/rogue,
    // https://www.realmeye.com/wiki/rogue-class-guide. Uses Dagger weapons
    // (Data/DaggerData.cs/json) and Cloak ability items (Cloak.cs,
    // Data/CloakData.cs/json). Shares Archer's Leather ArmorType — per the
    // class guide's own "the armor of choice for the Rogue is the Leather
    // Armor" — rather than getting a dedicated new armor type/catalog.
    public class Rogue : Player
    {
        public Rogue()
        {
            PlayerClass = Class.Rogue;
            Name = "Rogue";
            Description =
                "The rogue relies on speed and invisibility to strike from the shadows.";

            image = Art.Rogue;

            // Rogue_hit.ogg/Rogue_death.ogg were originally staged as .ogg,
            // which this engine's sound pipeline can't load as a SoundEffect
            // (WavImporter/SoundEffectProcessor only accepts .wav — see every
            // other class's own Sounds/Player/*_hit.wav, and the still-
            // orphaned level_up.mp3/no_mana.mp3, which hit the same mismatch
            // and were simply never wired in) — converted to .wav (matching
            // the existing hit sounds' own mono/16-bit/44.1kHz PCM format)
            // and wired in here. Rogue is also the first class with its own
            // death sound — see Sound.PlayerDeath's own comment.
            Sound.PlayerHit = Game1.Instance.Content.Load<SoundEffect>("Sounds/Player/Rogue_hit");
            Sound.PlayerDeath = Game1.Instance.Content.Load<SoundEffect>(
                "Sounds/Player/Rogue_death"
            );

            WeaponType = Weapon.WeaponType.Dagger;
            ArmorType = Armor.ArmorType.Leather;

            // Real base stats/growth-per-level/caps from the wiki's own
            // stats table (see the plan this was built from) — every
            // Average-at-20 value there reconciles exactly against
            // base + 19*rate, confirming these rates.
            baseHealth = 150;
            baseMana = 100;
            baseAttack = 16;
            baseDefense = 0;
            baseSpeed = 26;
            baseDexterity = 17;
            BaseVitality = 5;
            baseWisdom = 15;

            MaxHealth = 750;
            MaxMana = 300;
            MaxAttack = 55;
            MaxDefense = 25;
            MaxSpeed = 65;
            MaxDexterity = 75;
            MaxVitality = 40;
            MaxWisdom = 50;

            Weapon = Weapon.LoadWeapon("Rusty Dagger");
            Weapon.Type = Weapon.WeaponType.Dagger;

            // Same starter Armor Archer already has (shared ArmorType.Leather
            // — see this class's own doc comment above), exactly mirroring
            // how Priest reuses Wizard's "Cloth Robe".
            Armor = Armor.LoadArmor("Leather Vest");
            // No starting Ring, regardless of class — see Wizard.cs's
            // matching comment.
            AbilityItem = Cloak.LoadCloak("Tattered Cloak");

            // HealthMax/ManaMax/Attack/Defense/etc. are already set by
            // RecalculateStats(), triggered above by equipping starting gear
            // (at Level 1 its formula reduces to exactly the plain base
            // values). Health/Mana (current, not max) still need setting.
            Health = HealthMax;
            Mana = ManaMax;
        }

        // Uses (Level - 1) rather than Level so a fresh Level-1 character gets
        // exactly the base values with no formula bonus yet — matching
        // LevelUp() below, which increments Level before calling this, so by
        // the time this runs Level is already the new level.
        public override void RecalculateStats()
        {
            Attack =
                baseAttack
                + ((Level - 1) * 1)
                + PotionAttackBonus
                + EquipmentAttackBonus
                + TemporaryAttackBonus;
            Defense =
                baseDefense
                + (int)((Level - 1) * 0.5)
                + PotionDefenseBonus
                + EquipmentDefenseBonus
                + TemporaryDefenseBonus;
            Vitality =
                BaseVitality
                + ((Level - 1) * 1)
                + PotionVitalityBonus
                + EquipmentVitalityBonus
                + TemporaryVitalityBonus;
            Wisdom =
                baseWisdom
                + ((Level - 1) * 1)
                + PotionWisdomBonus
                + EquipmentWisdomBonus
                + TemporaryWisdomBonus;
            Speed =
                baseSpeed
                + ((Level - 1) * 1)
                + PotionSpeedBonus
                + EquipmentSpeedBonus
                + TemporarySpeedBonus;
            Dexterity =
                baseDexterity
                + ((Level - 1) * 2)
                + PotionDexterityBonus
                + EquipmentDexterityBonus
                + TemporaryDexterityBonus;

            HealthMax =
                baseHealth
                + ((Level - 1) * 25)
                + PotionHealthMaxBonus
                + EquipmentMaxHealthBonus
                + TemporaryHealthMaxBonus;
            ManaMax =
                baseMana
                + ((Level - 1) * 5)
                + PotionManaMaxBonus
                + EquipmentMaxManaBonus
                + TemporaryManaMaxBonus;
        }

        public override void LevelUp()
        {
            Level++;
            RecalculateStats();

            base.LevelUp();
        }

        public override bool CanEquipAbilityItem(AbilityItem item) => item is Cloak;

        // "The rogue's cloak will grant the user invisibility, preventing
        // most enemies from seeing (and targeting) the player." Real
        // un-targetability lives in Enemy.cs (IsInvisible gates both
        // ApplyAttackBehaviours() and FollowPlayer()) — this just starts the
        // timer via the generic Player.EnterInvisibility(). Shoot()'s own
        // "1 second grace, then cancel + grant Lethal Strike" logic lives on
        // Player directly (see Player.cs), since it's a harmless no-op for
        // every other class.
        public override void UseAbility()
        {
            base.UseAbility();

            if (!Weapon.IsEquipped)
            {
                Sound.Play(Sound.Error, 0.4f);
                return;
            }

            if (!AbilityItem.IsEquipped)
            {
                Sound.Play(Sound.Error, 0.4f);
                return;
            }

            if (Mana >= AbilityCost)
            {
                Mana -= AbilityCost;

                Cloak cloak = (Cloak)AbilityItem;
                EnterInvisibility(cloak.InvisibilityDurationFrames);
            }
            else
            {
                Sound.Play(Sound.NoMana, 0.4f);
            }
        }
    }
}
