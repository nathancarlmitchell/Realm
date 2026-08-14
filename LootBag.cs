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
            }
        }

        public void DrawLoot(SpriteBatch spriteBatch)
        {
            // Draw contents of loot bag if player is touching it, and it's the
            // closest such bag — otherwise multiple open bags would draw their
            // item portraits on top of each other. Also defers to the bank
            // panel if that's currently open — Portal.Update() already decided
            // the bank wins whenever it's at least as close as the nearest bag,
            // so a true result here means this bag lost that comparison.
            if (isOpen && ItemSpawner.NearestOpenBag() == this && !BankSystem.IsOpen)
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    // Centered over the play area, not the wider window
                    // (which includes the HUD sidebar) — this popup overlays
                    // gameplay content, so it should stay visually centered
                    // on what the player is actually looking at.
                    int x = Game1.GameplayViewportWidth / 2 + (i * 64);
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
                            bool pickedUp = false;
                            bool usedInPlace = false;

                            if (Player.Instance.Inventory.CanAddItem(Items[i]))
                            {
                                pickedUp = Player.Instance.Inventory.AddItem(Items[i], 1);
                            }
                            else if (Items[i].Consumable)
                            {
                                // No room to pick it up (stack/inventory full) — drink
                                // it where it lies instead of just rejecting the click.
                                usedInPlace = Player.Instance.Inventory.UsePotionEffect(
                                    Items[i].Name
                                );
                                Sound.Play(usedInPlace ? Sound.UsePotion : Sound.Error, 0.4f);
                            }
                            else
                            {
                                Sound.Play(Sound.Error, 0.4f);
                            }

                            if (pickedUp || usedInPlace)
                            {
                                Items[i].IsExpired = true;
                                Items.RemoveAt(i);

                                // Despawn loot bag if it is empty. Remove it from
                                // ItemSpawner.LootBags right here rather than
                                // waiting for a future Update() — an expired
                                // entity never gets Update() called on it again,
                                // so that removal would never actually happen,
                                // leaving a ghost bag that can silently absorb
                                // anything dropped at this spot afterward.
                                if (Items.Count <= 0)
                                {
                                    this.isOpen = false;
                                    this.IsExpired = true;
                                    ItemSpawner.LootBags.Remove(this);
                                }
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
