namespace Realm.Data
{
    // Rogue's ability item (see Cloak.cs and CharacterClasses/Rogue.cs) —
    // https://www.realmeye.com/wiki/cloaks. On the real wiki "Cloaks" sits
    // under Ability Items (alongside Quivers/Spells/Tomes/Shields), not
    // Armor — Rogue's actual Armor slot reuses Archer's Leather Armor, per
    // the Rogue Class Guide's own "the armor of choice for the Rogue is the
    // Leather Armor."
    public class CloakData
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

        // The wiki's "Cost" column — mana spent per Cloak activation.
        public int ManaCost { get; set; }

        // The wiki's "Invisibility Duration" column, in ticks (seconds*60).
        public int InvisibilityDurationFrames { get; set; }

        // Lethal Strike's damage formula — the wiki's own "Comparative
        // Cloaks Table": flat + percent bonus damage, each with a further
        // bonus scaling off Wisdom past 34. See
        // Cloak.ComputeLethalStrikeBonus() for how these combine (and why
        // "percent" scales off the shot's own damage here rather than the
        // real game's "percent of the target's Defense" — the projectile
        // architecture doesn't know which enemy it'll hit at fire-time).
        public float BaseFlatDamage { get; set; }
        public float FlatDamagePerWisOver34 { get; set; }
        public float BasePercentDamage { get; set; }
        public float PercentDamagePerWisOver34 { get; set; }

        // % bonus to XP gained while this Cloak is equipped — see
        // Equipment.XpBonusPercent.
        public float XpBonusPercent { get; set; }
        public string ImageName { get; set; }
    }
}
