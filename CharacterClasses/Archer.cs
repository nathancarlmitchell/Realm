using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Realm.CharacterClasses
{
    public class Archer : Player
    {
        public Archer()
        {
            PlayerClass = Class.Archer;
            Name = "Archer";
            Description =
                "The archer has a long-range attack and can acquire very powerful weapons.";

            image = Art.Archer;
            Sound.PlayerHit = Game1.Instance.Content.Load<SoundEffect>("Sounds/Player/archer_hit");

            WeaponType = Weapon.WeaponType.Bow;
            ArmorType = Armor.ArmorType.Leather;

            baseHealth = 150;
            baseMana = 100;
            baseAttack = 17;
            baseDefense = 0;
            baseSpeed = 22;
            baseDexterity = 15;
            BaseVitality = 5;
            baseWisdom = 15;

            MaxHealth = 750;
            MaxMana = 300;
            MaxAttack = 75;
            MaxDefense = 25;
            MaxSpeed = 55;
            MaxDexterity = 50;
            MaxVitality = 40;
            MaxWisdom = 50;

            Weapon = Weapon.LoadWeapon("Shortbow");
            Weapon.Type = Weapon.WeaponType.Bow;

            Armor = Armor.LoadArmor("Leather Vest");
            // No starting Ring, regardless of class — see Wizard.cs's
            // matching comment.
            AbilityItem = Quiver.LoadQuiver("Worn Quiver");

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
                + ((Level - 1) * 2)
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

        public override bool CanEquipAbilityItem(AbilityItem item) => item is Quiver;

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

            int damage = rand.Next(AbilityItem.MinDamage, AbilityItem.MaxDamage);
            Quiver quiver = (Quiver)AbilityItem;

            if (Mana >= AbilityCost)
            {
                Mana -= AbilityCost;

                var aim = Input.GetMouseAimDirection();
                float aimAngle = aim.ToAngle();

                // Unstable: "abilities that require aiming ... will fire in
                // random directions" — a full random angle, not just a
                // wider cone like Weapon.Shoot()'s own version of this,
                // since this fan is a single aimed cast rather than a
                // continuous stream of individually-aimable shots.
                if (HasDebuff(DebuffType.Unstable))
                    aimAngle = rand.NextFloat(0f, MathHelper.TwoPi);

                float arcGapRad = MathHelper.ToRadians(quiver.ArcGapDegrees);

                // A symmetric fan of Shots projectiles, each adjacent pair
                // ArcGapDegrees apart — an odd count centers one shot exactly
                // on the aim line, an even count straddles it evenly (e.g.
                // Shots=2 fires at +-half the gap, Shots=3 fires at
                // -gap/0/+gap). Same shot for every position: piercing
                // (ExpiresOnHit=false — "Piercing Shots hit multiple
                // targets"), paralyzing, using the Quiver's own independent
                // speed/lifetime/art rather than the equipped Bow's.
                for (int i = 0; i < quiver.Shots; i++)
                {
                    float angle = aimAngle + (i - (quiver.Shots - 1) / 2f) * arcGapRad;
                    Vector2 vel = Extensions.FromPolar(angle, quiver.ProjectileMagnitude);

                    EntityManager.Add(
                        new Projectile(Player.Instance.Position, vel)
                        {
                            Damage = damage,
                            Duration = quiver.ProjectileDuration,
                            image = quiver.ProjectileImage,
                            ExpiresOnHit = false,
                            ParalyzesOnHit = true,
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
