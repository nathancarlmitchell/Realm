namespace Realm.Data
{
    // Per-tier Armor stats — shared shape across all three ArmorTypes
    // (Robe/Leather/Heavy), each of which lives in its own catalog file
    // (Data/RobeData.json/Data/LeatherData.json/Data/HeavyData.json — see
    // Util.LoadRobeData()/LoadLeatherData()/LoadHeavyData()). No Type field
    // here — each loader hardcodes its own Armor.ArmorType, same as
    // Weapon.WeaponType is hardcoded per weapon-type loader (LoadWandData()
    // etc.) rather than read from JSON.
    public class ArmorData
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

        // % bonus to XP gained while this Armor is equipped — see
        // Equipment.XpBonusPercent.
        public float XpBonusPercent { get; set; }
        public string ImageName { get; set; }
    }
}
