namespace Realm.Data
{
    public class ShieldData
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
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public string ImageName { get; set; }

        // Shield Slam's shot fan — how many shots and how far apart they're
        // angled. Speed/lifetime/art stay Knight.cs's own fixed constants
        // (entry 169) since this ask is only about shot count/spread, not
        // the shot's other stats.
        public int Shots { get; set; }
        public float ArcGapDegrees { get; set; }

        // % bonus to XP gained while this Shield is equipped — see
        // Equipment.XpBonusPercent.
        public float XpBonusPercent { get; set; }

        // Shield Slam's damage scales with the Knight's own Wisdom past 34
        // — the real wiki's own per-tier "Damage: X-Y (+Z per WIS over 34)"
        // stat, confirmed against each tiered Shield's own dedicated wiki
        // page (not just the aggregate comparison table). Added directly
        // onto the rolled MinDamage-MaxDamage result in Knight.UseAbility().
        public float DamagePerWisOver34 { get; set; }
    }
}
