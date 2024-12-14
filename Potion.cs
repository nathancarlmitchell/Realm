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
    public class Potion : Item
    {
        public Potion()
        {
            this.image = Art.Item;
            this.Position.X = Player.Instance.Position.X + 100;
            this.Position.Y = Player.Instance.Position.Y + 100;

            this.ID = GameState.PotionGuid;
            this.Name = "Potion";

            Debug.WriteLine(ID);

            MaximumStackableQuantity = 6;
        }

        public static void Use()
        {
            Debug.WriteLine("Use()");
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
