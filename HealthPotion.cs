using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Realm.States;

namespace Realm
{
    public class HealthPotion : Item
    {
        public HealthPotion()
        {
            this.image = Art.HealthPotion;
            this.ID = GameState.HealthPotionGuid;
            this.Name = "HealthPotion";

            MaximumStackableQuantity = 6;
        }

        public static void Use()
        {
            int ammount = 25;
            int health = Player.Health;

            health += ammount;

            if (health > Player.HealthMax)
            {
                Player.Health = Player.HealthMax;
                return;
            }

            Player.Health += ammount;
        }
    }
}
