using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Realm.CharacterClasses
{
    public class Knight : Player
    {
        private const int ShieldBuffDefenseAmount = 20;
        private const int ShieldBuffDurationFrames = 180; // 3 seconds at 60fps

        public Knight()
        {
            PlayerClass = Class.Knight;
            Name = "Knight";
            Description =
                "The Knight is a stalwart defender, trading range and speed for raw toughness.";

            image = Art.Knight;
            Sound.PlayerHit = Game1.Instance.Content.Load<SoundEffect>("Sounds/Player/knight_hit");

            WeaponType = Weapon.WeaponType.Sword;
            ArmorType = Armor.ArmorType.Heavy;

            baseHealth = 200;
            baseMana = 100;
            baseAttack = 15;
            baseDefense = 1;
            baseSpeed = 17;
            baseDexterity = 15;
            BaseVitality = 17;
            baseWisdom = 15;

            MaxHealth = 800;
            MaxMana = 300;
            MaxAttack = 50;
            MaxDefense = 40;
            MaxSpeed = 40;
            MaxDexterity = 50;
            MaxVitality = 75;
            MaxWisdom = 50;

            Weapon = Weapon.LoadWeapon("Iron Sword");
            Weapon.Type = Weapon.WeaponType.Sword;

            Armor = Armor.LoadArmor("Iron Plate");
            Ring = Ring.LoadRing("Plain Ring");
            AbilityItem = Shield.LoadShield("Wooden Shield");

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
                + ((Level - 1) * 1)
                + PotionDefenseBonus
                + EquipmentDefenseBonus
                + TemporaryDefenseBonus;
            Vitality =
                BaseVitality
                + ((Level - 1) * 2)
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
                + ((Level - 1) * 1)
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

        public override bool CanEquipAbilityItem(AbilityItem item) => item is Shield;

        // Shield Slam: one large projectile toward the mouse aim (same shape
        // as Archer's ability, and now the same damage formula too) that
        // stuns whatever it hits (blocks attacks only — unlike Archer's
        // Quiver, which paralyzes and blocks movement only), plus a
        // temporary Defense buff on the Knight regardless of whether the
        // shot connects.
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

                var aim = Input.GetMouseAimDirection();
                float aimAngle = aim.ToAngle();

                Vector2 vel = Extensions.FromPolar(aimAngle, Weapon.ProjectileMagnitude);

                EntityManager.Add(
                    new Projectile(Player.Instance.Position, vel)
                    {
                        Damage = damage,
                        Duration = Weapon.ProjectileDuration + 15,
                        image = Art.ShieldProjectile,
                        ExpiresOnHit = false,
                        StunsOnHit = true,
                    }
                );

                AddTemporaryDefenseBonus(ShieldBuffDefenseAmount, ShieldBuffDurationFrames);
            }
            else
            {
                Sound.Play(Sound.NoMana, 0.4f);
            }
        }
    }
}
