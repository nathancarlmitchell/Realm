namespace Realm.Data
{
    public class RingData
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

        // % bonus to XP gained while this Ring is equipped — see
        // Equipment.XpBonusPercent. Only populated for the tiered per-stat
        // Rings (Attack/Defense/Dexterity/Health/Magic/Speed/Vitality/
        // Wisdom Rings, T1-T7) for now — the pre-existing "Ring of Minor
        // Defense"/"Ring of Vigor" entries are out of scope until asked.
        public float XpBonusPercent { get; set; }
        public string ImageName { get; set; }
    }
}
