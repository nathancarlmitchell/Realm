using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Realm.CharacterClasses
{
    public class Priest : Player
    {
        // The Nova's own fixed shape — not per-tier data (see Data/
        // TomeData.cs's comment on Range, which *is* per-tier and governs
        // something different: how far from the Priest the Nova can be
        // centered, not how big the blast itself is once it lands).
        // 2.5 tiles * 32px/tile.
        private const float NovaRadius = 80f;

        // "Explodes 2 times for 0.8 seconds" — one pulse immediately on
        // cast, a second NovaPulse-scheduled one 0.8s (48 ticks at 60
        // ticks/sec) later. Total Nova Damage from Data/TomeData.cs is
        // split evenly across both.
        private const int NovaPulseDelayFrames = 48;

        public Priest()
        {
            PlayerClass = Class.Priest;
            Name = "Priest";
            Description = "The priest attacks at long range and can heal himself and his allies.";

            image = Art.Priest;
            Sound.PlayerHit = Game1.Instance.Content.Load<SoundEffect>("Sounds/Player/priest_hit");

            WeaponType = Weapon.WeaponType.Wand;
            ArmorType = Armor.ArmorType.Robe;

            baseHealth = 100;
            baseMana = 150;
            baseAttack = 26;
            baseDefense = 0;
            baseSpeed = 22;
            baseDexterity = 23;
            BaseVitality = 5;
            baseWisdom = 17;

            MaxHealth = 700;
            MaxMana = 400;
            MaxAttack = 65;
            MaxDefense = 25;
            MaxSpeed = 55;
            MaxDexterity = 60;
            MaxVitality = 40;
            MaxWisdom = 75;

            Weapon = Weapon.LoadWeapon("Fire Wand");
            Weapon.Type = Weapon.WeaponType.Wand;

            Armor = Armor.LoadArmor("Cloth Robe");
            // No starting Ring, regardless of class — see Wizard.cs's
            // matching comment.
            AbilityItem = Tome.LoadTome("Healing Tome");

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
        // the time this runs Level is already the new level. Rates below
        // (+1 ATT, +0.5 DEF, +1 SPD, +1 DEX, +1 VIT, +2 WIS per level) are
        // exactly what the class spec's own "Average at 20" column implies:
        // e.g. Attack 26 + 19*1 = 45, Wisdom 17 + 19*2 = 55.
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
                + ((Level - 1) * 2)
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

        public override bool CanEquipAbilityItem(AbilityItem item) => item is Tome;

        // How often (in ticks) Update() below spawns another small clump of
        // rising motes while the Healing HoT is active — a first-pass value,
        // easy to retune with just this one number.
        private const int HealingParticleIntervalFrames = 5;
        private int healingParticleCooldown;

        // A continuous trickle of small white motes for exactly as long as
        // the Healing HoT (Red Cross Healing) is active, rather than a
        // single burst at the moment of casting — HasDebuff(DebuffType.
        // Healing) mirrors ApplyHealing()'s own real duration exactly (see
        // Player.cs's ApplyHealing() comment), so this needs no separate
        // timer of its own to know when to stop. Reset to 0 (spawn
        // immediately) the instant the debuff drops, so a fresh cast never
        // has to wait out whatever was left of the interval from a previous
        // one.
        public override void Update()
        {
            base.Update();

            if (HasDebuff(DebuffType.Healing))
            {
                healingParticleCooldown--;
                if (healingParticleCooldown <= 0)
                {
                    RisingParticle.SpawnRisingBurst(
                        Position + new Vector2(0, Size.Y / 2f),
                        Color.White,
                        count: 3,
                        lifespanTicks: 40,
                        scale: 0.06f,
                        spawnWidth: Size.X
                    );
                    healingParticleCooldown = HealingParticleIntervalFrames;
                }
            }
            else
            {
                healingParticleCooldown = 0;
            }
        }

        // The cursor's world position, clamped to the Tome's own Range —
        // used by UseAbility() below (Unstable's own direction-
        // randomization is applied on top of this afterward, for the
        // actual cast).
        private Vector2 ComputeClampedCursorOffset(Tome tome)
        {
            Vector2 toCursor = Input.GetMousePosition() - Position;
            float rangePixels = tome.Range * 32f;
            if (toCursor.LengthSquared() > rangePixels * rangePixels)
                toCursor = Vector2.Normalize(toCursor) * rangePixels;
            return toCursor;
        }

        // Tome: instant self-heal, a Red Cross Healing HoT (self-only, per
        // the user's explicit choice — the original spec describes healing
        // nearby allies too, but this engine is single-player with no one
        // else to apply it to), and a two-pulse damage Nova centered on the
        // cursor (clamped to the Tome's own Range). All three fire from the
        // same cast, regardless of whether anything is standing in the
        // Nova's blast radius.
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

            Tome tome = (Tome)AbilityItem;

            if (Mana >= AbilityCost)
            {
                Mana -= AbilityCost;

                if (tome.HealAmount > 0)
                    Heal(tome.HealAmount);

                if (tome.HealingAmountPerSecond > 0)
                    ApplyHealing(
                        tome.HealingAmountPerSecond,
                        (int)(tome.HealingDurationSeconds * 60)
                    );

                // Nova center: the cursor's world position, clamped to the
                // Tome's own Range so it can't be cast further away than
                // intended.
                Vector2 toCursor = ComputeClampedCursorOffset(tome);

                // Unstable: same "keep the distance, randomize the
                // direction" treatment as Wizard's Spell Bomb — Nova is a
                // point-centered AoE burst with no directional shots of its
                // own, so the target point itself is the only "aim" this
                // ability has to disrupt (see DebuffType.Unstable).
                if (HasDebuff(DebuffType.Unstable))
                    toCursor = Extensions.FromPolar(
                        rand.NextFloat(0f, MathHelper.TwoPi),
                        toCursor.Length()
                    );

                Vector2 novaCenter = Position + toCursor;

                int damagePerPulse = tome.MinDamage / 2;

                EntityManager.DamageEnemiesInRadius(novaCenter, NovaRadius, damagePerPulse);
                Particle.SpawnBurst(
                    novaCenter,
                    Color.White,
                    count: 10,
                    minSpeed: 1.5f,
                    maxSpeed: 4f,
                    lifespanTicks: 20
                );
                // Orange sparks scattered in and just beyond the blast
                // radius, on top of the plain white center-burst above —
                // NovaPulse's own second pulse spawns the same on its own
                // delayed hit, so both hits read consistently.
                Particle.SpawnAreaBurst(
                    novaCenter,
                    NovaRadius,
                    Color.Orange,
                    count: 16,
                    minSpeed: 1f,
                    maxSpeed: 5f,
                    lifespanTicks: 18
                );
                EntityManager.Add(
                    new NovaPulse(novaCenter, NovaRadius, damagePerPulse, NovaPulseDelayFrames)
                );

                // Visual confirmation of the blast area at the moment it
                // lands — a solid gold disc fading out over NovaRadiusFlash's
                // own lifespan.
                EntityManager.Add(new NovaRadiusFlash(novaCenter, NovaRadius, Color.Gold));

                Sound.Play(Sound.MagicShoot, 0.3f);
            }
            else
            {
                Sound.Play(Sound.NoMana, 0.4f);
            }
        }
    }
}
