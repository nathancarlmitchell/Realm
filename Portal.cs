using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm.States;

namespace Realm
{
    public class Portal : Item
    {
        public Portal()
        {
            this.image = Art.Portal;
            this.Radius = image.Width / 2;
            this.Position.X = Player.Instance.Position.X + 100;
            this.Position.Y = Player.Instance.Position.Y + 100;

            //this.ID = GameState.Portal;
            this.Name = "Portal";

            Debug.WriteLine(ID);

            MaximumStackableQuantity = 6;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            spriteBatch.DrawString(
                Art.HudFont,
                "Enter",
                new Vector2(
                    this.Position.X - (this.image.Width / 4),
                    this.Position.Y + (this.image.Height / 2)
                ),
                Color.Black
            );
        }
    }
}
