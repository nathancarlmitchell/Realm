namespace Realm.Data
{
    // Staves split out of WeaponData.json into their own catalog file/loader
    // (see Util.LoadStaffData()) — same reasoning as Data/WandData.cs: a
    // per-tier XpBonusPercent (see Equipment.XpBonusPercent) matching the
    // real wiki's "XP Bonus" column (https://www.realmeye.com/wiki/staves),
    // which plain WeaponData has no field for. Always
    // Weapon.WeaponType.Staff, so unlike WeaponData there's no Type field
    // here — Util.LoadStaffData() sets it.
    public class StaffData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Tier { get; set; }
        public int DamageMin { get; set; }
        public int DamageMax { get; set; }
        public float ProjectileMagnitude { get; set; }
        public int ProjectileDuration { get; set; }

        // The perpendicular sine-wave offset applied on top of each shot's
        // straight-line path (see SineWaveProjectile.cs) — carried over
        // unchanged from WeaponData's own Amplitude/Frequency fields, which
        // only Staff ever used.
        public float Amplitude { get; set; }
        public float Frequency { get; set; }

        public float XpBonusPercent { get; set; }
        public string ImageName { get; set; }
        public string ProjectileImageName { get; set; }
    }
}
