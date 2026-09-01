namespace Realm.Data
{
    // Bows split out of WeaponData.json into their own catalog file/loader
    // (see Util.LoadBowData()) since — unlike every other weapon type — a
    // Bow fires two independently-tuned shot kinds (Main and Side) at once,
    // needing its own damage range and projectile art per kind rather than
    // the single DamageMin/DamageMax/ProjectileImageName every other
    // WeaponData entry has. Always Weapon.WeaponType.Bow, so unlike
    // WeaponData there's no Type field here — Util.LoadBowData() sets it.
    public class BowData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Tier { get; set; }

        public int MainDamageMin { get; set; }
        public int MainDamageMax { get; set; }
        public int SideDamageMin { get; set; }
        public int SideDamageMax { get; set; }

        public float ProjectileMagnitude { get; set; }
        public int ProjectileDuration { get; set; }

        // Degrees each side shot is angled away from the aim line (so the
        // two side shots sit 2x this apart from each other). Same value
        // for every tier today, but kept as data (like Staff's
        // Amplitude/Frequency) rather than a hardcoded constant.
        public float ArcGapDegrees { get; set; }

        public string ImageName { get; set; }
        public string MainProjectileImageName { get; set; }
        public string SideProjectileImageName { get; set; }

        // % bonus to XP gained while this Bow is equipped — see
        // Equipment.XpBonusPercent. Missing entirely until reviewed against
        // each tiered Bow's own dedicated wiki page — every other
        // WeaponData-backed type with a real per-tier XP Bonus (Sword,
        // Dagger) already had this field.
        public float XpBonusPercent { get; set; }
    }
}
