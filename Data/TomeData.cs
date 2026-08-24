namespace Realm.Data
{
    public class TomeData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Tier { get; set; }
        public int MaxHealthBonus { get; set; }
        public int MaxManaBonus { get; set; }
        public int AttackBonus { get; set; }
        public int DefenseBonus { get; set; }
        public float SpeedBonus { get; set; }
        public int DexterityBonus { get; set; }
        public int VitalityBonus { get; set; }
        public int WisdomBonus { get; set; }
        public int ManaCost { get; set; }

        // Nova Damage — a single fixed value per tier, not a random range,
        // so MinDamage/MaxDamage (inherited via Tome : AbilityItem) are set
        // equal to it. Reused directly rather than adding a third
        // "NovaDamage" field, since AbilityItem's existing tooltip/
        // comparison code already reads MinDamage/MaxDamage.
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public string ImageName { get; set; }

        // Max distance (tiles) the Nova can be centered from the Priest —
        // the cursor position is clamped to this before the Nova fires, not
        // the Nova's own blast radius (see Priest.cs's NovaRadius, a fixed
        // constant, not per-tier).
        public float Range { get; set; }

        // Instant self-heal on cast — 0 for Tier 0 (not yet unlocked; the
        // Healing Tome only has the Red Cross Healing HoT below).
        public int HealAmount { get; set; }

        // Red Cross Healing — the HoT applied to the Priest on cast (see
        // Player.ApplyHealing()). Multiple applications don't stack; only
        // the strongest (highest HealingAmountPerSecond) applies.
        public float HealingAmountPerSecond { get; set; }
        public float HealingDurationSeconds { get; set; }

        // % bonus to XP gained while this Tome is equipped — see
        // Equipment.XpBonusPercent.
        public float XpBonusPercent { get; set; }
    }
}
