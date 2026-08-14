using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using static Realm.Weapon;

namespace Realm.CharacterClasses
{
    public class Wizard : Player
    {
        public Wizard()
        {
            PlayerClass = Class.Wizard;
            Name = "Wizard";
            Description =
                "The Wizard deals damage from a long distance and blasts enemies with powerful spells.";

            image = Art.Wizard;
            Sound.PlayerHit = Game1.Instance.Content.Load<SoundEffect>("Sounds/Player/wizard_hit");

            WeaponType = Weapon.WeaponType.Wand;
            ArmorType = Armor.ArmorType.Robe;

            baseHealth = 100;
            baseMana = 150;
            baseAttack = 23;
            baseDefense = 0;
            baseSpeed = 17;
            baseDexterity = 17;
            BaseVitality = 5;
            baseWisdom = 23;

            MaxHealth = 700;
            MaxMana = 400;
            MaxAttack = 60;
            MaxDefense = 25;
            MaxSpeed = 50;
            MaxDexterity = 75;
            MaxVitality = 40;
            MaxWisdom = 60;

            Weapon = LoadWeapon("Fire Wand");
            Weapon.Type = Weapon.WeaponType.Wand;

            Armor = Armor.LoadArmor("Cloth Robe");
            Ring = Ring.LoadRing("Plain Ring");
            AbilityItem = Spell.LoadSpell("Novice Spellbook");

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
                + ((Level - 1))
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
                + ((Level - 1) * 7)
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

        public override bool CanEquipAbilityItem(AbilityItem item) => item is Spell;

        public override void UseAbility()
        {
            base.UseAbility();

            if (!Weapon.IsEquipped)
            {
                Sound.Play(Sound.Error, 0.4f);
                return;
            }

            int damage = rand.Next(AbilityItem.MinDamage, AbilityItem.MaxDamage);

            if (Mana >= AbilityCost)
            {
                Mana -= AbilityCost;

                int SpellBombProjectileCount = 16;
                // Spell bomb. Expires on hit even though the Wand basic
                // attack passes through — an ability shot, not a wand bolt.
                for (int i = 0; i < SpellBombProjectileCount; i++)
                {
                    Vector2 vel = Extensions.FromPolar(
                        i * (MathHelper.TwoPi / SpellBombProjectileCount),
                        Instance.Weapon.ProjectileMagnitude
                    );
                    EntityManager.Add(
                        new Projectile(Input.GetMousePosition(), vel)
                        {
                            Damage = damage,
                            ExpiresOnHit = true,
                        }
                    );
                }
            }
            else
            {
                Sound.Play(Sound.NoMana, 0.4f);
            }
        }
    }
}
