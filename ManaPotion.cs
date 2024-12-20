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
    public class ManaPotion : Item
    {
        public ManaPotion()
        {
            this.image = Art.ManaPotion;
            //this.Position.X = Player.Instance.Position.X + 100;
            //this.Position.Y = Player.Instance.Position.Y + 100;

            this.ID = GameState.ManaPotionGuid;
            this.Name = "ManaPotion";

            MaximumStackableQuantity = 6;
        }

        public static void Use()
        {
            int ammount = 25;
            int mana = Player.Mana;

            mana += ammount;

            if (mana > Player.ManaMax)
            {
                Player.Mana = Player.ManaMax;
                return;
            }

            Player.Mana += ammount;
        }
    }
}
