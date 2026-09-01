namespace Realm.Data
{
    public class QuiverData
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

        // The Quiver ability's own shot — independent of whatever Bow is
        // currently equipped (previously it borrowed the equipped Weapon's
        // ProjectileMagnitude/ProjectileDuration/ProjectileImage; now it has
        // its own, same reasoning as Bow's own Main/Side split).
        public int Shots { get; set; }
        public float ArcGapDegrees { get; set; }
        public float ProjectileMagnitude { get; set; }
        public int ProjectileDuration { get; set; }
        public string ProjectileImageName { get; set; }

        // % bonus to XP gained while this Quiver is equipped — see
        // Equipment.XpBonusPercent.
        public float XpBonusPercent { get; set; }

        // The Quiver ability's damage scales with the Archer's own Wisdom
        // past 34 — the real wiki's own per-tier "Damage: X-Y (+Z per WIS
        // over 34)" stat, confirmed against each tiered Quiver's own
        // dedicated wiki page (not just the aggregate tiered-quivers table,
        // which doesn't show this at all). Added directly onto the rolled
        // MinDamage-MaxDamage result in Archer.UseAbility(), same as
        // Shield's DamagePerWisOver34.
        public float DamagePerWisOver34 { get; set; }
    }
}
