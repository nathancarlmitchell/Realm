namespace Realm.Data
{
    // Swords split out of WeaponData.json into their own catalog file/loader
    // (see Util.LoadSwordData()) — same reasoning as Data/BowData.cs, just
    // for a different reason: swords needed a per-tier XpBonusPercent
    // (see Equipment.XpBonusPercent) matching the real wiki's "XP Bonus"
    // column (https://www.realmeye.com/wiki/swords), which WeaponData has
    // no field for and which no other WeaponData-backed type currently
    // needs. Always Weapon.WeaponType.Sword, so unlike WeaponData there's
    // no Type field here — Util.LoadSwordData() sets it.
    public class SwordData
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
