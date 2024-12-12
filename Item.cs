using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Realm
{
    public class Item : Entity
    {
        public Item()
        {
            this.image = Art.Item;
            this.Radius = image.Width / 2;
            this.Position.X = Player.Instance.Position.X + 100;
            this.Position.Y = Player.Instance.Position.Y + 100;
        }

        public void Pickup()
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

        public override void Update()
        {
            //throw new NotImplementedException();
        }
    }
}
