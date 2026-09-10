namespace Realm.Data
{
    // Wands split out of WeaponData.json into their own catalog file/loader
    // (see Util.LoadWandData()) — same reasoning as Data/SwordData.cs/
    // Data/BowData.cs: a per-tier XpBonusPercent (see Equipment.XpBonusPercent)
    // matching the real wiki's "XP Bonus" column
    // (https://www.realmeye.com/wiki/wands), which plain WeaponData has no
    // field for. Always Weapon.WeaponType.Wand, so unlike WeaponData
    // there's no Type field here — Util.LoadWandData() sets it.
    public class WandData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Tier { get; set; }

        // UT-only — see Equipment.IsUntiered's own doc comment. False (the
        // default) for every tiered wand; a UT entry leaves Tier at -1 by
        // convention (same as Data/RingData.json's Snake Eye Ring and
        // Data/StaffData.json's Staff of Extreme Prejudice), purely so it
        // reads as "not a real tier."
        public bool IsUntiered { get; set; }

        public int DamageMin { get; set; }
        public int DamageMax { get; set; }
        public float ProjectileMagnitude { get; set; }
        public int ProjectileDuration { get; set; }

        // Wand-wavy-shot only (Sprite Wand — realmeye.com/wiki/sprite-wand,
        // "shoots in a wavy pattern"). Both 0 (the default) for every
        // tiered wand, which keeps the straight Projectile Weapon.Shoot()
        // fires for a normal wand. Nonzero Amplitude routes the shot
        // through a SineWaveProjectile instead, same primitive every Staff
        // shot uses — Amplitude in pixels (tile * 32), Frequency directly
        // in cycles/shot (unconverted, matching Data/StaffData.cs).
        public float Amplitude { get; set; }
        public float Frequency { get; set; }

        public float XpBonusPercent { get; set; }
        public string ImageName { get; set; }
        public string ProjectileImageName { get; set; }
    }
}
