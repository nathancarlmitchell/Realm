namespace Realm.Data
{
    public class SpellData
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

        // Spell Bomb's own shot — previously borrowed the equipped Weapon's
        // ProjectileMagnitude/Duration (drifting with whatever Wand/Staff was
        // equipped instead of the real, independent 16 tiles/sec, 1s
        // lifetime every tiered Spell's own wiki page gives), same fix
        // Quiver/Shield already got. ProjectileImageName defaults to
        // "projectile" (Art.Projectile) for every tier — the same generic
        // bolt Spell Bomb already rendered as before this field existed, not
        // a visual change.
        public float ProjectileMagnitude { get; set; }
        public int ProjectileDuration { get; set; }
        public string ProjectileImageName { get; set; }

        // Spell Bomb's damage scales with the Wizard's own Wisdom past 42
        // (a higher threshold than Shield/Quiver's 34, matching Wizard's own
        // much higher Wisdom cap) — the real wiki's own per-tier "+X per WIS
        // over 42" stat, confirmed against each tiered Spell's own dedicated
        // wiki page. Shot count *also* scales with Wisdom past 42 in the
        // same way ("+1 shot per 15 WIS over 42"), but that scaling is
        // identical across every tier (not per-item data), so it's a plain
        // constant in Wizard.cs instead of a field here.
        public float DamagePerWisOver42 { get; set; }

        // % bonus to XP gained while this Spell is equipped — see
        // Equipment.XpBonusPercent.
        public float XpBonusPercent { get; set; }
    }
}
