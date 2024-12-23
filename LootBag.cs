using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    public class LootBag : Entity
    {
        public List<Item> Items = [];
        private bool isOpen;
        private bool clicked;

        public LootBag()
        {
            image = Art.LootBag;
            isOpen = false;
            clicked = false;
        }

        public void Add(Item item)
        {
            Items.Add(item);
        }

        public void Remove(Item item) { }

        public void Clear() { }

        public override void Update()
        {
            isOpen = false;
            clicked = false;

            // Check if player is touching bag.
            if (Player.Instance.Bounds.Intersects(this.Bounds))
            {
                isOpen = true;
                if (Input.GetMouseClick())
                {
                    clicked = true;
                }
                //return;
            }
        }

        public void DrawLoot(SpriteBatch spriteBatch)
        {
            // Draw contents of loot bag if player is touching.
            if (isOpen)
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    int x = Game1.ScreenWidth / 2 + (i * 64);
                    int y = 200;

                    // Draw item border.
                    spriteBatch.Draw(
                        Art.Border,
                        new Vector2(x - Art.Border.Width / 2, y - Art.Border.Height / 2),
                        Color.White
                    );

                    Items[i].Position = new Vector2(x, y);

                    // Check if mouse is over item.
                    if (
                        new Rectangle(
                            x - Items[i].Width / 2,
                            y - Items[i].Height / 2,
                            64,
                            64
                        ).Intersects(Input.MouseBounds)
                    )
                    {
                        Items[i].Hover = true;
                        if (clicked)
                        {
                            // Place clicked item in inventory if it can stack,
                            // or free slots in inventory.
                            if (
                                Player.Instance.Inventory.HasRoom()
                                || Player.Instance.Inventory.CanStack(Items[i], 1)
                            )
                            {
                                // Add item to inventory and remove from bag.
                                Player.Instance.Inventory.AddItem(Items[i], 1);
                                Items[i].IsExpired = true;
                                Items.RemoveAt(i);

                                // Despawn loot bag if it is empty.
                                if (Items.Count <= 0)
                                {
                                    this.IsExpired = true;
                                }
                            }
                            else
                            {
                                // Error, no room in inventory.
                                Sound.Play(Sound.Error, 0.4f);
                            }

                            clicked = false;
                            return;
                        }
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
