namespace Realm.Data
{
    // Daggers split out of WeaponData.json into their own catalog file/loader
    // (see Util.LoadDaggerData()) — same reasoning as Data/SwordData.cs:
    // daggers need a per-tier XpBonusPercent (see Equipment.XpBonusPercent),
    // matching the real wiki's "XP Bonus" column
    // (https://www.realmeye.com/wiki/daggers), which plain WeaponData has no
    // field for. Always Weapon.WeaponType.Dagger, so unlike WeaponData
    // there's no Type field here — Util.LoadDaggerData() sets it.
    public class DaggerData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Tier { get; set; }
        public int DamageMin { get; set; }
        public int DamageMax { get; set; }
        public float ProjectileMagnitude { get; set; }
        public int ProjectileDuration { get; set; }
        public float XpBonusPercent { get; set; }
        public string ImageName { get; set; }
        public string ProjectileImageName { get; set; }
    }
}
