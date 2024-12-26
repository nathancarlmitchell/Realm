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
        public Potion(Potions potion)
        {
            MaximumStackableQuantity = 6;
            Consumable = true;

            switch (potion)
            {
                case Potions.Health:
                    this.image = Art.HealthPotion;
                    this.ID = GameState.HealthPotionGuid;
                    this.Name = "Health Potion";
                    break;

                case Potions.Mana:
                    this.image = Art.ManaPotion;
                    this.ID = GameState.ManaPotionGuid;
                    this.Name = "Mana Potion";
                    break;

                case Potions.Attack:
                    this.image = Art.Attack;
                    this.ID = GameState.AttackPotionGuid;
                    this.Name = "Attack Potion";
                    break;

                case Potions.Defense:
                    this.image = Art.Defense;
                    this.ID = GameState.DefensePotionGuid;
                    this.Name = "Defense Potion";
                    break;

                case Potions.Speed:
                    this.image = Art.Speed;
                    this.ID = GameState.SpeedPotionGuid;
                    this.Name = "Speed Potion";
                    break;

                case Potions.Dexterity:
                    this.image = Art.Dexterity;
                    this.ID = GameState.DexterityPotionGuid;
                    this.Name = "Dexterity Potion";
                    break;

                case Potions.Vitality:
                    this.image = Art.Vitalty;
                    this.ID = GameState.VitalityPotionGuid;
                    this.Name = "Vitality Potion";
                    break;

                case Potions.Wisdom:
                    this.image = Art.Wisdom;
                    this.ID = GameState.WisdomPotionGuid;
                    this.Name = "Wisdom Potion";
                    break;

                case Potions.Life:
                    this.image = Art.Life;
                    this.ID = GameState.LifePotionGuid;
                    this.Name = "Life Potion";
                    break;

                case Potions.ManaMax:
                    this.image = Art.Mana;
                    this.ID = GameState.ManaGuid;
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
                    int health = Player.Health;
                    health += amount;
                    if (health > Player.HealthMax)
                    {
                        Player.Health = Player.HealthMax;
                        return;
                    }
                    Player.Health += amount;
                    break;

                case Potions.Mana:
                    int mana = Player.Mana;
                    mana += amount;
                    if (mana > Player.ManaMax)
                    {
                        Player.Mana = Player.ManaMax;
                        return;
                    }
                    Player.Mana += amount;
                    break;

                case Potions.Attack:
                    Player.Attack += 1;
                    break;

                case Potions.Defense:
                    Player.Defense += 1;
                    break;

                case Potions.Speed:
                    Player.Speed += 1;
                    break;

                case Potions.Dexterity:
                    Player.Dexterity += 1;
                    break;

                case Potions.Vitality:
                    Player.Vitality += 1;
                    break;

                case Potions.Wisdom:
                    Player.Wisdom += 1;
                    break;

                case Potions.Life:
                    Player.HealthMax += 5;
                    break;

                case Potions.ManaMax:
                    Player.ManaMax += 5;
                    break;

                default:
                    Debug.WriteLine("Unkown Potion");
                    break;
            }
        }
    }
}
