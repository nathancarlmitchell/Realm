using System;
using System.Collections.Generic;
using System.Linq;
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
        // slot position: second in the row, right after Weapon.
        protected static int x = Game1.SidebarX + 20 + 40;
        protected static int y = 410;

        public AbilityItem()
        {
            SlotBounds = new Rectangle(x, y, 40, 40);
        }

        // Tier 0 ability item matching the current class (Spell/Quiver/
        // Shield/Tome/Cloak) — shown greyed out in an empty slot as a placeholder,
        // same idea as Weapon.PlaceholderImage. Unlike Weapon/Armor there's
        // no shared "Type" enum to filter on (each class's ability item is
        // its own C# subclass), so this checks each catalog's Tier 0 entry
        // against Player.CanEquipAbilityItem — the same per-class match
        // already used everywhere else an ability item's class-fit matters.
        private static Texture2D PlaceholderImage =>
            Game1
                .Instance.Spells.Cast<AbilityItem>()
                .Concat(Game1.Instance.Quivers)
                .Concat(Game1.Instance.Shields)
                .Concat(Game1.Instance.Tomes)
                .Concat(Game1.Instance.Cloaks)
                .FirstOrDefault(item => item.Tier == 0 && Player.Instance.CanEquipAbilityItem(item))
                ?.image;

        // Defined here rather than per-subclass since the render logic is
        // identical for Spell and Quiver — only the equipped data differs.
        public void DrawEquipped(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Art.Border, new Vector2(x, y), Color.White);

            if (!IsEquipped)
            {
                if (PlaceholderImage != null)
                    spriteBatch.Draw(PlaceholderImage, new Vector2(x, y), Color.Gray * 0.5f);
                return;
            }

            spriteBatch.Draw(this.image, new Vector2(x, y), Color.White);
            DrawTierLabel(spriteBatch, SlotBounds);
        }

        // Drawn in its own pass, after every equip slot's border/icon (see
        // Overlay.DrawEquipment()) — otherwise a later-drawn slot's icon can
        // paint over this one's tooltip wherever the two overlap, since
        // SpriteBatch preserves draw-call order and the four slots aren't
        // drawn in the same order they're laid out on screen. Same fix as
        // LootBag's equivalent bug.
        public void DrawTooltip(SpriteBatch spriteBatch)
        {
            if (!IsEquipped || !hover)
                return;

            string text = TooltipText();

            int textY = (int)(Art.RetroFont.MeasureString(text).Y / 2);

            Util.DrawCategorizedTooltip(
                spriteBatch,
                Art.RetroFont,
                text,
                new Vector2(x, y - image.Height - textY)
            );
        }

        public override string TooltipText()
        {
            string description = Util.WrapText(Art.RetroFont, Description, 350);
            return $"{TierLabel} - {Name}{Environment.NewLine}{description}{Environment.NewLine}{BonusSummary()}{AbilitySummary()}";
        }

        private string AbilitySummary()
        {
            // Cloak (Rogue) has no direct damage roll of its own — its
            // ability is Invisibility, not a projectile burst — so
            // MinDamage/MaxDamage stay 0/0 for every tier. Showing
            // "Damage: 0 - 0" there would be misleading, so the line is
            // skipped rather than always shown like every other AbilityItem
            // subtype (Spell/Quiver/Shield/Tome), none of which hit this
            // case.
            List<string> parts = [];
            if (MinDamage != 0 || MaxDamage != 0)
                parts.Add($"Damage: {MinDamage} - {MaxDamage}");

            if (ManaCost != 0)
                parts.Add($"{ManaCost} Mana Cost");

            // Each part on its own line (not comma-joined) — Damage and Mana
            // Cost get different tooltip colors (see Util.
            // DrawCategorizedTooltip), which only works if they're separate
            // lines rather than sharing one.
            return parts.Count > 0 ? Environment.NewLine + string.Join(Environment.NewLine, parts) : "";
        }

        public override List<(string Text, TooltipComparison Comparison)> ComparisonLines(
            Equipment equipped
        )
        {
            var equippedItem = (AbilityItem)equipped;

            var lines = HeaderLines();
            lines.AddRange(BonusComparisonLines(equipped));

            // Same "Cloak has no damage roll" reasoning as AbilitySummary()
            // above — skip the comparison line entirely rather than show a
            // meaningless "Damage: 0 - 0 (Same)" for every Cloak tier.
            if (MinDamage != 0 || MaxDamage != 0 || equippedItem.MinDamage != 0 || equippedItem.MaxDamage != 0)
            {
                float mineDamage = (MinDamage + MaxDamage) / 2f;
                float theirDamage = (equippedItem.MinDamage + equippedItem.MaxDamage) / 2f;
                lines.Add(
                    (
                        $"Damage: {MinDamage} - {MaxDamage}",
                        !CanEquipByCurrentClass ? TooltipComparison.WrongClass
                            : mineDamage > theirDamage ? TooltipComparison.Better
                            : mineDamage < theirDamage ? TooltipComparison.Worse
                            : TooltipComparison.Same
                    )
                );
            }

            if (ManaCost != 0)
            {
                // Lower is better here, unlike every other stat on this
                // tooltip — only count it as an improvement when there's an
                // actual equipped ability item to be cheaper than, not just
                // an empty slot (nothing equipped has no real mana cost to
                // beat).
                bool cheaper = equippedItem.IsEquipped && ManaCost < equippedItem.ManaCost;
                lines.Add(
                    (
                        $"{ManaCost} Mana Cost",
                        cheaper ? TooltipComparison.Better : TooltipComparison.Same
                    )
                );
            }

            return lines;
        }
    }
}
