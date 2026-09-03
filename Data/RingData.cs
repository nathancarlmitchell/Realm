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

        // Untiered (UT) — see Equipment.IsUntiered's own doc comment.
        // false (the default) for every normal tiered ring. A UT ring
        // should also set Tier to a negative number (e.g. -1) in its own
        // JSON entry so it's automatically excluded from every tier-based
        // catalog query — this flag alone doesn't do that, it only affects
        // display/bag-rank.
        public bool IsUntiered { get; set; }

        // A UT ring's own signature mechanic, e.g. Snake Eye Ring's "On
        // Ability Use: gain Speedy." A plain Entity.DebuffType name (parsed
        // in Util.LoadRingData()), not the enum itself — same "string in
        // JSON, parsed in code" convention DungeonTypeData's own tile-name
        // fields already use, since System.Text.Json has no enum-as-string
        // support configured anywhere in this codebase. Null/empty (the
        // default) means this ring has no reactive proc at all.
        public string ReactiveProc { get; set; }
        public int ReactiveProcDurationFrames { get; set; }
        public int ReactiveProcCooldownFrames { get; set; }
    }
}
