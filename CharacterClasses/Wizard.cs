using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Realm.Projectiles;
using static Realm.Weapon;

namespace Realm.CharacterClasses
{
    public class Wizard : Player
    {
        // Spell Bomb's shot-count and damage both scale with Wisdom past
        // this threshold — higher than Shield/Quiver's 34, matching
        // Wizard's own much higher Wisdom cap. Confirmed identical across
        // every tiered Spell's own dedicated wiki page.
        private const int SpellWisThreshold = 42;

        // "16 (+1 per 15 WIS over 42)" — uniform across every tier (unlike
        // damage, which is per-tier data — see Data/SpellData.cs's
        // DamagePerWisOver42), so this is a plain constant here instead.
        private const int SpellBombBaseShots = 16;
        private const int SpellBombWisPerExtraShot = 15;

        public Wizard()
        {
            PlayerClass = Class.Wizard;
            Name = "Wizard";
            Description =
                "The Wizard deals damage from a long distance and blasts enemies with powerful spells.";

            image = Art.Wizard;
            Sound.PlayerHit = Game1.Instance.Content.Load<SoundEffect>("Sounds/Player/wizard_hit");

            WeaponType = Weapon.WeaponType.Staff;
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

            Weapon = LoadWeapon("Gnarled Staff");
            Weapon.Type = Weapon.WeaponType.Staff;

            Armor = Armor.LoadArmor("Cloth Robe");
            // No starting Ring, regardless of class — Player()'s base
            // constructor already leaves Ring as a fresh, unequipped
            // `new Ring()` (IsEquipped is just `image != null`, and the
            // parameterless constructor never sets image), so simply not
            // overriding it here is enough.
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

            if (!AbilityItem.IsEquipped)
            {
                Sound.Play(Sound.Error, 0.4f);
                return;
            }

            Spell spell = (Spell)AbilityItem;

            // Both scale with the Wizard's own Wisdom past SpellWisThreshold
            // — shots by a uniform engine-level rate (SpellBombWisPerExtraShot),
            // damage by this Spell's own per-tier DamagePerWisOver42. A flat
            // addition to a uniform roll is mathematically identical to
            // shifting the range before rolling, same reasoning as Shield/
            // Quiver's own scaling.
            int wisOverThreshold = Math.Max(0, Wisdom - SpellWisThreshold);
            int spellBombProjectileCount =
                SpellBombBaseShots + wisOverThreshold / SpellBombWisPerExtraShot;
            int damage =
                rand.Next(AbilityItem.MinDamage, AbilityItem.MaxDamage)
                + (int)(spell.DamagePerWisOver42 * wisOverThreshold);

            if (Mana >= AbilityCost)
            {
                Mana -= AbilityCost;

                // Spell Bomb radiates evenly in every direction already, so
                // "fire in random directions" (see DebuffType.Unstable) has
                // nothing to bite for the shots themselves — rotating a full,
                // symmetric 360° ring by any amount looks identical. The one
                // actual aim this ability has is *where* it detonates
                // (Input.GetMousePosition(), the cursor's world position);
                // Unstable randomizes that instead, keeping the same
                // distance from the caster the player was actually aiming
                // for but picking a random direction to put it in, matching
                // how the debuff already treats a "direction" everywhere
                // else.
                Vector2 spawnPosition = Input.GetMousePosition();
                if (HasDebuff(DebuffType.Unstable))
                {
                    float distanceFromCaster = Vector2.Distance(spawnPosition, Position);
                    spawnPosition =
                        Position
                        + Extensions.FromPolar(rand.NextFloat(0f, MathHelper.TwoPi), distanceFromCaster);
                }

                // Spell bomb. Always expires on hit regardless of what the
                // currently-equipped weapon's own basic attack does (e.g.
                // Wand's pass-through) — an ability shot, not a basic
                // attack bolt. Uses the Spell's own independent projectile
                // speed/lifetime/art (Data/SpellData.cs) rather than the
                // equipped Weapon's — previously borrowed the Weapon's,
                // drifting with whatever Wand/Staff was equipped instead of
                // the real, fixed 16 tiles/sec every tiered Spell's own wiki
                // page gives, same fix Quiver/Shield already got. Shots
                // stay evenly spaced around the full circle regardless of
                // count, so a higher-Wisdom Wizard's extra shots (see
                // spellBombProjectileCount above) still radiate symmetrically
                // rather than overlapping the base 16.
                for (int i = 0; i < spellBombProjectileCount; i++)
                {
                    Vector2 vel = Extensions.FromPolar(
                        i * (MathHelper.TwoPi / spellBombProjectileCount),
                        spell.ProjectileMagnitude
                    );
                    EntityManager.Add(
                        new Projectile(spawnPosition, vel)
                        {
                            Damage = damage,
                            Duration = spell.ProjectileDuration,
                            image = spell.ProjectileImage,
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
