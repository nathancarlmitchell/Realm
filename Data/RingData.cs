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
        // Equipment.XpBonusPercent.
        public float XpBonusPercent { get; set; }
        public string ImageName { get; set; }
    }
}
