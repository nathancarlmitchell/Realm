using System;

namespace Realm.Data;

public class PlayerData
{
    public Guid ID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

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
    public int Experience { get; set; }
    public int ExperienceNextLevel { get; set; }
    public int ExperienceTotal { get; set; }
    public int Level { get; set; }
    public Weapon Weapon { get; set; }
}
