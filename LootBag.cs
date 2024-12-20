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
    public class LootBag : Entity
    {
        public List<Item> Items = [];
        private bool isOpen;

        public LootBag()
        {
            image = Art.LootBag;
            isOpen = false;
        }

        public void Add(Item item) { }

        public void Remove(Item item) { }

        public void Clear() { }

        public override void Update()
        {
            if (Player.Instance.Bounds.Intersects(this.Bounds))
            {
                isOpen = true;
                return;
            }
            isOpen = false;
        }

        public void DrawLoot(SpriteBatch spriteBatch)
        {
            if (isOpen)
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    int x = Game1.ScreenWidth / 2 + (i * 64);
                    int y = 200;

                    spriteBatch.Draw(
                        Art.Border,
                        new Vector2(x - Art.Border.Width / 2, y - Art.Border.Height / 2),
                        Color.White
                    );

                    Vector2 pos = new Vector2(x, y);

                    Items[i].Position = pos;

                    if (
                        new Rectangle(
                            (int)pos.X - Items[i].Width / 2,
                            (int)pos.Y - Items[i].Height / 2,
                            64,
                            64
                        ).Intersects(Input.MouseBounds)
                    )
                    {
                        Items[i].Hover = true;
                    }
                    else
                    {
                        Items[i].Hover = false;
                    }

                    Items[i].Draw(spriteBatch);
                    Items[i].DrawLoot(spriteBatch, x, y);
                }
            }
        }
    }
}
