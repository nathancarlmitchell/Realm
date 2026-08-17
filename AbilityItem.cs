using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    public class AbilityItem : Equipment
    {
        public int ManaCost { get; set; }
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }

        public override bool CanEquipByCurrentClass => Player.Instance.CanEquipAbilityItem(this);

        // Shared by Spell/Quiver — only one is ever equipped at a time
        // (whichever matches the player's class), so both render in the same
        // slot position: continuing the equipment row after Ring.
        protected static int x = Game1.SidebarX + 20 + 120;
        protected static int y = 410;

        public AbilityItem()
        {
            SlotBounds = new Rectangle(x, y, 40, 40);
        }

        // Defined here rather than per-subclass since the render logic is
        // identical for Spell and Quiver — only the equipped data differs.
        public void DrawEquipped(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Art.Border, new Vector2(x, y), Color.White);

            if (!IsEquipped)
                return;

            spriteBatch.Draw(this.image, new Vector2(x, y), Color.White);

            if (hover)
            {
                string text = TooltipText();

                int textY = (int)(Art.HudFont.MeasureString(text).Y / 2);

                Util.DrawTooltip(
                    spriteBatch,
                    Art.HudFont,
                    text,
                    new Vector2(x, y - image.Height - textY),
                    Color.Red
                );
            }
        }

        public override string TooltipText()
        {
            string description = Util.WrapText(Art.HudFont, Description, 350);
            return $"T{Tier} - {Name}{Environment.NewLine}{description}{Environment.NewLine}{BonusSummary()}{AbilitySummary()}";
        }

        private string AbilitySummary()
        {
            List<string> parts = [$"Damage: {MinDamage} - {MaxDamage}"];

            if (ManaCost != 0)
                parts.Add($"{ManaCost} Mana Cost");

            return parts.Count > 0 ? Environment.NewLine + string.Join(", ", parts) : "";
        }
    }
}
