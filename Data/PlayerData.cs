using System;

namespace Realm.Data;

public class PlayerData
{
    public Guid ID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Player.Class PlayerClass { get; set; }

    //public int Health { get; set; }
    public int HealthMax { get; set; }

    //public int Mana { get; set; }
    public int ManaMax { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Vitality { get; set; }
    public int Wisdom { get; set; }
    public float Speed { get; set; }
    public int Dexterity { get; set; }
    // Not Experience/ExperienceNextLevel — both fully derive from Level +
    // ExperienceTotal (see Player.ExperienceNextLevel/
    // CumulativeExperienceForLevel), so there's nothing left to persist for
    // them.
    public int ExperienceTotal { get; set; }
    public int HighScore { get; set; }

    // True once this character has actually been played (selected and
    // entered the world) since its last delete — unlike HighScore, this does
    // NOT survive a delete, so Character Select can tell "never played /
    // just wiped" apart from "played, but happens to have zero score" (the
    // latter should still offer Delete; the former shouldn't).
    public bool HasBeenPlayed { get; set; }

    // Permanent once true — see Player.HasReachedLevel20's own doc comment.
    // Unlike HasBeenPlayed, this DOES survive a delete, same treatment as
    // HighScore.
    public bool HasReachedLevel20 { get; set; }
    public int Level { get; set; }
    public Weapon Weapon { get; set; }
    public Armor Armor { get; set; }
    public Ring Ring { get; set; }

    // Only one of these is ever populated, matching whichever class was
    // saved — separate concrete-typed fields rather than one AbilityItem-
    // typed field, avoiding a dependency on JSON polymorphism resolving
    // correctly several inheritance levels below where it's declared.
    public Spell Spell { get; set; }
    public Quiver Quiver { get; set; }
    public Shield Shield { get; set; }
    public Tome Tome { get; set; }
    public int HealthPotionCharges { get; set; }
    public int ManaPotionCharges { get; set; }

    // Permanent stat-potion bonuses (see Player.Potion*Bonus). Equipment
    // bonuses don't need a field here — they're read live from Weapon/Armor/
    // Ring above, each of which already carries its own bonus fields.
    public int PotionAttackBonus { get; set; }
    public int PotionDefenseBonus { get; set; }
    public float PotionSpeedBonus { get; set; }
    public int PotionDexterityBonus { get; set; }
    public int PotionVitalityBonus { get; set; }
    public int PotionWisdomBonus { get; set; }
    public int PotionHealthMaxBonus { get; set; }
    public int PotionManaMaxBonus { get; set; }
}
