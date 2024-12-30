using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static Realm.InventorySystem;

namespace Realm
{
    public static class ItemSpawner
    {
        private static readonly Random rand = new();

        // Move to GameState?
        public static List<LootBag> LootBags = [];

        public static void Reset()
        {
            LootBags = [];
        }

        public static void Spawn(Vector2 pos)
        {
            List<Item> items = [];
            Texture2D bagTexture = Art.LootBag;

            // Drop weapon.
            if (rand.Next(15) == 0)
            {
                // Drop the next highest teir.
                if (Game1.Instance.Weapons.Exists(x => (x.Teir == Player.Instance.Weapon.Teir + 1)))
                {
                    bagTexture = Art.LootBagPink;
                    //int randomWeapon = rand.Next(Game1.Instance.Weapons.Count);
                    Weapon nextWeapon = Game1.Instance.Weapons.FirstOrDefault(x =>
                        (x.Teir == Player.Instance.Weapon.Teir + 1)
                    );
                    items.Add(nextWeapon);
                }
            }

            // Drop stat potion.
            if (rand.Next(15) == 0)
            {
                bagTexture = Art.LootBagBlue;
                int next = rand.Next(8);
                Potions potion = Potions.Health;
                switch (next)
                {
                    case 0:
                        potion = Potions.Attack;
                        break;
                    case 1:
                        potion = Potions.Defense;
                        break;
                    case 2:
                        potion = Potions.Dexterity;
                        break;
                    case 3:
                        potion = Potions.Life;
                        break;
                    case 4:
                        potion = Potions.ManaMax;
                        break;
                    case 5:
                        potion = Potions.Speed;
                        break;
                    case 6:
                        potion = Potions.Vitality;
                        break;
                    case 7:
                        potion = Potions.Wisdom;
                        break;
                }
                items.Add(new Potion(potion));
            }

            if (rand.Next(10) == 0)
            {
                if (rand.Next(2) == 0)
                    items.Add(new Potion(Potions.Mana));
                else
                    items.Add(new Potion(Potions.Health));
            }

            if (items.Count > 0)
            {
                LootBag bag = new()
                {
                    Position = pos,
                    Items = items,
                    image = bagTexture,
                };

                EntityManager.Add(bag);
                LootBags.Add(bag);

                Sound.Play(Sound.LootAppears, 0.4f);
            }
        }

        //public static void Update()
        //{
        //    // Remove items from bag
        //    // Remove bag if no items left
        //}
    }
}
