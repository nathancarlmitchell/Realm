using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    public abstract class Item : Entity
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public int MaximumStackableQuantity { get; set; }
        public bool Hover;

        public Item()
        {
            MaximumStackableQuantity = 1;
            Hover = false;
        }

        public Item(Texture2D image)
        {
            this.image = image;
            this.Radius = image.Width / 2;

            MaximumStackableQuantity = 1;
            Hover = false;
        }

        public override void Update()
        {
            int x = (int)Player.Instance.Position.X;
            int y = (int)Player.Instance.Position.Y;

            //this.Position = new Vector2(x, y);

            if (Bounds.Intersects(Input.MouseBounds))
            {
                Hover = true;
                Debug.WriteLine(Bounds);
                return;
            }
            Hover = false;
        }

        public void DrawLoot(SpriteBatch spriteBatch, int x, int y)
        {
            if (Hover)
            {
                string text = Name;

                int textX = (int)(Art.HudFont.MeasureString(text).X / 2);
                int textY = (int)(Art.HudFont.MeasureString(text).Y / 2);

                spriteBatch.DrawString(
                    Art.HudFont,
                    text,
                    new Vector2(x - textX, y - image.Height - textY),
                    Color.Red
                );
            }
        }

        //public override void Draw(SpriteBatch spriteBatch)
        //{
        //    spriteBatch.Draw(Art.Border, new Vector2(x, y), Color.White);
        //    spriteBatch.Draw(Art.Wand, new Vector2(x, y), Color.White);

        //    if (hover)
        //    {
        //        string text =
        //            $"T{Teir} - {Name}{Environment.NewLine}{Description}{Environment.NewLine}Damge: {DamageMin} - {DamageMax}";

        //        int textY = (int)(Art.HudFont.MeasureString(text).Y / 2);

        //        spriteBatch.DrawString(
        //            Art.HudFont,
        //            text,
        //            new Vector2(x, y - image.Height - textY),
        //            Color.Red
        //        );
        //    }
        //}
    }
}
