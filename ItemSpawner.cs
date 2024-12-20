using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Realm
{
    public static class ItemSpawner
    {
        private static readonly Random rand = new();
        public static List<LootBag> LootBags = new List<LootBag>();

        public static void Spawn(Vector2 pos)
        {
            List<Item> items = new List<Item>();

            items.Add(new Weapon(Art.Wand, Art.Projectile2));
            if (rand.Next(10) == 0)
            {
                if (rand.Next(3) == 0)
                    items.Add(new HealthPotion());
                else
                    items.Add(new ManaPotion());
            }

            if (items.Count > 0)
            {
                LootBag bag = new LootBag() { Position = pos, Items = items };
                EntityManager.Add(bag);
                LootBags.Add(bag);
            }
        }

        public static void Update()
        {
            // Remove items from bag
            // Remove bag if no items left
        }
    }
}
