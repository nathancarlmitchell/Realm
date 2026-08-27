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

        // Despawn countdown, fixed at spawn and never touched by Add()/
        // Remove()/Clear() below — adding or removing items doesn't reset
        // it. Lazily initialized on the first Update() call rather than in
        // the constructor: every real LootBag is constructed via an object
        // initializer (`new LootBag { image = bagTexture, ... }` in
        // ItemSpawner.cs), which only assigns `image` *after* the
        // constructor body already ran — too early there to know which
        // color this bag actually is.
        private int? lifespanTicksRemaining = null;

        // Orange/Red/White run twice as long as every other color — the
        // rarer/better bags (Red and White sit at the top of the tier
        // ladder — see ItemSpawner.cs's BagRankFor*/entry 146; Orange isn't
        // wired to any drop yet but gets the same treatment now in case it
        // is later) are worth a longer window to notice and grab.
        private const int NormalLifespanTicks = 60 * 60; // 60s @ 60 ticks/sec
        private const int LongLifespanTicks = 120 * 60; // 120s

        private static int LifespanTicksFor(Texture2D texture) =>
            texture == Art.LootBagOrange || texture == Art.LootBagRed || texture == Art.LootBagWhite
                ? LongLifespanTicks
                : NormalLifespanTicks;

        // Blink window before despawn — starts slow, ramps up to a fast
        // blink right before the bag actually disappears, rather than one
        // fixed blink rate for the whole warning window.
        private const int BlinkWarningTicks = 10 * 60; // last 10s
        private const float SlowBlinkHalfPeriodTicks = 20f;
        private const float FastBlinkHalfPeriodTicks = 4f;
        private float blinkPhase = 0f;

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

            lifespanTicksRemaining ??= LifespanTicksFor(image);
            lifespanTicksRemaining--;

            if (lifespanTicksRemaining <= 0)
            {
                // Same explicit removal the pickup-emptied-the-bag path
                // below already does — an expired entity never gets
                // Update() called on it again, and nothing in
                // EntityManager prunes ItemSpawner.LootBags on its own, so
                // skipping this would leave a ghost bag that still renders
                // via DrawLoot() (called directly off that list, not
                // through EntityManager) even after despawning.
                IsExpired = true;
                ItemSpawner.LootBags.Remove(this);
                return;
            }

            if (lifespanTicksRemaining <= BlinkWarningTicks)
            {
                // 0 at the start of the warning window, 1 right at despawn
                // — drives the blink period from slow to fast.
                float progress = 1f - (lifespanTicksRemaining.Value / (float)BlinkWarningTicks);
                float halfPeriod = MathHelper.Lerp(SlowBlinkHalfPeriodTicks, FastBlinkHalfPeriodTicks, progress);
                blinkPhase += 1f / halfPeriod;
                color = ((int)blinkPhase % 2) == 0 ? Color.White : Color.Transparent;
            }
            else
            {
                color = Color.White;
            }

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
                int hoveredIndex = -1;
                int hoveredX = 0,
                    hoveredY = 0;

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

                    if (Items[i].Hover)
                    {
                        hoveredIndex = i;
                        hoveredX = x;
                        hoveredY = y;
                    }
                }

                // Full Tier/name/description/bonuses for equipment — same as
                // the inventory/bank hover tooltip, including which stat
                // lines would beat what's currently equipped — plain items
                // (e.g. potions) just show their name. Drawn in its own pass
                // after every item's border/icon above, rather than inline
                // during that loop — a tooltip is often wider/taller than
                // the 64px item spacing, so drawing it mid-loop meant a later
                // item's border/icon (drawn afterward, same call order
                // SpriteBatch preserves) could paint right over it wherever
                // the two overlapped on screen.
                if (hoveredIndex >= 0)
                {
                    Item hovered = Items[hoveredIndex];

                    if (hovered is Equipment equipment)
                    {
                        Equipment equippedCounterpart = equipment switch
                        {
                            Weapon => Player.Instance.Weapon,
                            Armor => Player.Instance.Armor,
                            Ring => Player.Instance.Ring,
                            AbilityItem => Player.Instance.AbilityItem,
                            _ => null,
                        };

                        List<(string Text, TooltipComparison Comparison)> lines = equippedCounterpart != null
                            ? equipment.ComparisonLines(equippedCounterpart)
                            : new List<(string Text, TooltipComparison Comparison)>
                            {
                                (equipment.TooltipText(), TooltipComparison.Same),
                            };

                        float width = 0f;
                        foreach (var line in lines)
                            width = Math.Max(width, Art.RetroFont.MeasureString(line.Text).X);
                        int textX = (int)(width / 2);
                        int textY = (int)(lines.Count * Art.RetroFont.LineSpacing / 2);

                        Util.DrawTooltip(
                            spriteBatch,
                            Art.RetroFont,
                            lines,
                            new Vector2(hoveredX - textX, hoveredY - hovered.image.Height - textY)
                        );
                    }
                    else
                    {
                        string text = hovered.Name;
                        int textX = (int)(Art.RetroFont.MeasureString(text).X / 2);
                        int textY = (int)(Art.RetroFont.MeasureString(text).Y / 2);

                        Util.DrawTooltip(
                            spriteBatch,
                            Art.RetroFont,
                            text,
                            new Vector2(hoveredX - textX, hoveredY - hovered.image.Height - textY),
                            Color.White
                        );
                    }
                }
            }
        }
    }
}
