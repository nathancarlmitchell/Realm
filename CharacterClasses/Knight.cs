using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Realm.CharacterClasses
{
    public class Knight : Player
    {
        // Damage Reduction: "you receive 75% damage for 5 seconds."
        private const float ShieldDamageReductionMultiplier = 0.75f;
        private const int ShieldDamageReductionDurationFrames = 300; // 5 seconds at 60fps

        // Shield Slam's own shot stats — independent of whatever Sword is
        // equipped (previously borrowed Weapon.ProjectileMagnitude/Duration,
        // which drifted with gear instead of holding the spec's fixed 16
        // tiles/sec, 0.2s lifetime, 3.2-tile range). 16 tiles/sec * 32px per
        // tile / 60 ticks/sec = 8.533333 px/tick; 0.2s * 60 ticks/sec = 12
        // ticks (8.533333 * 12 = 102.4px = 3.2 tiles, consistent).
        private const float ShieldProjectileMagnitude = 8.533333f;
        private const int ShieldProjectileDuration = 12;

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
            MaxSpeed = 50;
            MaxDexterity = 50;
            MaxVitality = 75;
            MaxWisdom = 50;

            Weapon = Weapon.LoadWeapon("Iron Sword");
            Weapon.Type = Weapon.WeaponType.Sword;

            Armor = Armor.LoadArmor("Iron Plate");
            // No starting Ring, regardless of class — see Wizard.cs's
            // matching comment.
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
        // temporary Damage Reduction buff on the Knight regardless of
        // whether any shot connects. Each shot pierces (hits multiple
        // targets, same as Bow/Quiver) and uses the ability's own fixed
        // speed/lifetime rather than the equipped Sword's. Higher-tier
        // Shields fire more shots in a wider fan — see Data/ShieldData.cs's
        // Shots/ArcGapDegrees.
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
            Shield shield = (Shield)AbilityItem;

            if (Mana >= AbilityCost)
            {
                Mana -= AbilityCost;

                var aim = Input.GetMouseAimDirection();
                float aimAngle = aim.ToAngle();

                // Unstable: same full-random-angle treatment as Archer's
                // Quiver fan — see that file's own comment.
                if (HasDebuff(DebuffType.Unstable))
                    aimAngle = rand.NextFloat(0f, MathHelper.TwoPi);

                float arcGapRad = MathHelper.ToRadians(shield.ArcGapDegrees);

                // Symmetric fan, same formula as Archer's Quiver (entry
                // 156): an odd Shots count centers one shot on the aim
                // line, an even count straddles it evenly. Shots=1 (Tier 0)
                // degenerates to a single shot straight down the aim line.
                for (int i = 0; i < shield.Shots; i++)
                {
                    float angle = aimAngle + (i - (shield.Shots - 1) / 2f) * arcGapRad;
                    Vector2 vel = Extensions.FromPolar(angle, ShieldProjectileMagnitude);

                    EntityManager.Add(
                        new Projectile(Player.Instance.Position, vel)
                        {
                            Damage = damage,
                            Duration = ShieldProjectileDuration,
                            image = Art.ShieldProjectile,
                            ExpiresOnHit = false,
                            StunsOnHit = true,
                        }
                    );
                }

                AddTemporaryDamageTakenMultiplier(
                    ShieldDamageReductionMultiplier,
                    ShieldDamageReductionDurationFrames
                );
            }
            else
            {
                Sound.Play(Sound.NoMana, 0.4f);
            }
        }
    }
}
