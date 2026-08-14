using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Realm.States;

namespace Realm
{
    public enum Potions
    {
        Health,
        Mana,
        Attack,
        Defense,
        Speed,
        Dexterity,
        Vitality,
        Wisdom,
        Life,
        ManaMax,
    }

    public class Potion : Item
    {
        // Required by System.Text.Json to deserialize a saved Potion (it's a
        // registered polymorphic Item subtype) — all fields are populated via
        // property setters after construction, same as Weapon's parameterless ctor.
        public Potion() { }

        public Potion(Potions potion)
        {
            MaximumStackableQuantity = 6;
            Consumable = true;

            switch (potion)
            {
                case Potions.Health:
                    this.image = Art.HealthPotion;
                    this.ImageName = "health_potion";
                    this.ID = RealmState.HealthPotionGuid;
                    this.Name = "Health Potion";
                    break;

                case Potions.Mana:
                    this.image = Art.ManaPotion;
                    this.ImageName = "mana_potion";
                    this.ID = RealmState.ManaPotionGuid;
                    this.Name = "Mana Potion";
                    break;

                case Potions.Attack:
                    this.image = Art.Attack;
                    this.ImageName = "Items/Potions/attack";
                    this.ID = RealmState.AttackPotionGuid;
                    this.Name = "Attack Potion";
                    break;

                case Potions.Defense:
                    this.image = Art.Defense;
                    this.ImageName = "Items/Potions/defense";
                    this.ID = RealmState.DefensePotionGuid;
                    this.Name = "Defense Potion";
                    break;

                case Potions.Speed:
                    this.image = Art.Speed;
                    this.ImageName = "Items/Potions/speed";
                    this.ID = RealmState.SpeedPotionGuid;
                    this.Name = "Speed Potion";
                    break;

                case Potions.Dexterity:
                    this.image = Art.Dexterity;
                    this.ImageName = "Items/Potions/dexterity";
                    this.ID = RealmState.DexterityPotionGuid;
                    this.Name = "Dexterity Potion";
                    break;

                case Potions.Vitality:
                    this.image = Art.Vitality;
                    this.ImageName = "Items/Potions/vitality";
                    this.ID = RealmState.VitalityPotionGuid;
                    this.Name = "Vitality Potion";
                    break;

                case Potions.Wisdom:
                    this.image = Art.Wisdom;
                    this.ImageName = "Items/Potions/wisdom";
                    this.ID = RealmState.WisdomPotionGuid;
                    this.Name = "Wisdom Potion";
                    break;

                case Potions.Life:
                    this.image = Art.Life;
                    this.ImageName = "Items/Potions/life";
                    this.ID = RealmState.LifePotionGuid;
                    this.Name = "Life Potion";
                    break;

                case Potions.ManaMax:
                    this.image = Art.Mana;
                    this.ImageName = "Items/Potions/mana";
                    this.ID = RealmState.ManaGuid;
                    this.Name = "ManaMax Potion";
                    break;

                default:
                    Debug.WriteLine("Unkown Potion");
                    break;
            }
        }

        public static void Use(Potions potion)
        {
            int amount = 25;

            switch (potion)
            {
                case Potions.Health:
                    int health = Player.Instance.Health;
                    health += amount;
                    if (health > Player.Instance.HealthMax)
                    {
                        Player.Instance.Health = Player.Instance.HealthMax;
                        return;
                    }
                    Player.Instance.Health += amount;
                    break;

                case Potions.Mana:
                    int mana = Player.Instance.Mana;
                    mana += amount;
                    if (mana > Player.Instance.ManaMax)
                    {
                        Player.Instance.Mana = Player.Instance.ManaMax;
                        return;
                    }
                    Player.Instance.Mana += amount;
                    break;

                case Potions.Attack:
                    Player.Instance.PotionAttackBonus += 1;
                    Player.Instance.RecalculateStats();
                    break;

                case Potions.Defense:
                    Player.Instance.PotionDefenseBonus += 1;
                    Player.Instance.RecalculateStats();
                    break;

                case Potions.Speed:
                    Player.Instance.PotionSpeedBonus += 1;
                    Player.Instance.RecalculateStats();
                    break;

                case Potions.Dexterity:
                    Player.Instance.PotionDexterityBonus += 1;
                    Player.Instance.RecalculateStats();
                    break;

                case Potions.Vitality:
                    Player.Instance.PotionVitalityBonus += 1;
                    Player.Instance.RecalculateStats();
                    break;

                case Potions.Wisdom:
                    Player.Instance.PotionWisdomBonus += 1;
                    Player.Instance.RecalculateStats();
                    break;

                case Potions.Life:
                    Player.Instance.PotionHealthMaxBonus += 5;
                    Player.Instance.RecalculateStats();
                    break;

                case Potions.ManaMax:
                    Player.Instance.PotionManaMaxBonus += 5;
                    Player.Instance.RecalculateStats();
                    break;

                default:
                    Debug.WriteLine("Unkown Potion");
                    break;
            }
        }
    }
}
