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

            this.ID = Guid.NewGuid();
            this.Name = "Potion";

            MaximumStackableQuantity = 6;
        }

        public void Pickup()
        {
            Debug.WriteLine("Pickup()");
            int ammount = 25;
            int health = Player.Health;

            health += ammount;

            if (health > Player.HealthMax)
            {
                Player.Health = Player.HealthMax;
                return;
            }

            Player.Health += ammount;

            //InventorySystem.AddItem(new Potion(), 1);
            //InventorySystem.Instance.AddItem(this, 1);
            GameState.AddItem();
        }
    }
}
