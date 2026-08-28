using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    // Same/Better/Worse rather than a plain bool — the inventory/bank/
    // loot-bag comparison tooltip (ComparisonLines() below) needs a real
    // three-way result per line, not just "is this an upgrade": a stat
    // line now always shows (Gold) when it matches what's equipped, not
    // just when it's better (Green) or worse (Red, e.g. hovering a T0
    // Robe while a T1 Robe is equipped shows "-1 Defense" in red).
    // Weapon/AbilityItem's own Damage lines use the same three-way scheme
    // now too (see Util.DrawTooltip()'s color resolution) — Mana Cost and
    // header lines (name/tier/description) still only ever use Same/Better,
    // deliberately unchanged.
    //
    // WrongClass overrides all three for a Damage line specifically when
    // CanEquipByCurrentClass is false — the item can't actually be worn by
    // this class at all, so "better/worse/same" than what's equipped is
    // moot; Util.DrawTooltip() renders it Gray regardless of the raw damage
    // numbers.
    public enum TooltipComparison
    {
        Same,
        Better,
        Worse,
        WrongClass,
    }

    public class Equipment : Item
    {
        public int Tier { get; set; }

        // Fixed to the border's 40x40 footprint, not the equipped icon's size —
        // an empty slot (nothing equipped) still needs a valid drop target.
        public Rectangle SlotBounds;
        protected bool hover = false;

        // False for the blank placeholder every equip slot starts as (and
        // returns to once unequipped/dragged out).
        public bool IsEquipped => image != null;

        // Settings > Graphics > "Display Item Tiers" (default on) —
        // "T{Tier}" drawn in the bottom-right corner of this item's own
        // icon (inset 4px from both edges so it doesn't touch the icon's
        // actual border), wherever it's drawn. Public rather than protected: three
        // of its four call sites (InventorySystem.Draw(), BankSystem.Draw(),
        // LootBag.DrawLoot()) aren't Equipment subclasses, so they can't
        // reach a protected member — each of those passes its own
        // already-computed icon bounds for that specific slot (no shared
        // field exists across all four contexts to read instead); the
        // fourth (each of Weapon/Armor/Ring/AbilityItem's own
        // DrawEquipped()) just passes its existing SlotBounds.
        // Pure position math, split out from the actual draw call below so
        // it's independently testable without needing a working
        // SpriteBatch/GraphicsDevice.
        private const float TierLabelEdgeOffset = 4f;

        private static Vector2 ComputeTierLabelPosition(Rectangle iconBounds, Vector2 textSize) =>
            new(
                iconBounds.Right - textSize.X - TierLabelEdgeOffset,
                iconBounds.Bottom - textSize.Y - TierLabelEdgeOffset
            );

        public void DrawTierLabel(SpriteBatch spriteBatch, Rectangle iconBounds)
        {
            if (!Player.Instance.DisplayItemTiersEnabled)
                return;

            string text = "T" + Tier;
            Vector2 textSize = Art.RetroFont.MeasureString(text);
            Vector2 position = ComputeTierLabelPosition(iconBounds, textSize);
            Util.DrawOutlinedText(spriteBatch, Art.RetroFont, text, position, Color.White);
        }

        // True if the player's current class can actually equip this item —
        // used to visually flag wrong-class items sitting in the inventory/
        // bank grids (InventorySystem.Draw()/BankSystem.Draw()). Ring has no
        // class restriction, so the base implementation is always true;
        // Weapon/Armor/AbilityItem each override with the exact same check
        // their own LoadWeapon()/LoadArmor()/Player.CanEquipAbilityItem()
        // already use to decide whether the item can actually be equipped.
        public virtual bool CanEquipByCurrentClass => true;

        // Read live by Player.RecalculateStats() (summed across Weapon/Armor/
        // Ring) to compute derived stats — no accumulator to keep in sync,
        // this item's own fields are the source of truth while it's equipped.
        public int MaxHealthBonus { get; set; }
        public int MaxManaBonus { get; set; }
        public int AttackBonus { get; set; }
        public int DefenseBonus { get; set; }
        public float SpeedBonus { get; set; }
        public int DexterityBonus { get; set; }
        public int VitalityBonus { get; set; }
        public int WisdomBonus { get; set; }

        // % bonus to XP gained while equipped (see Player.
        // EquipmentXpBonusPercent/Enemy.WasShot()'s death branch). 0 for
        // every item except Tome so far — kept here rather than on Tome
        // alone since it's summed live across all four equip slots the same
        // way every other bonus above already is.
        public float XpBonusPercent { get; set; }

        public override void Update()
        {
            hover = SlotBounds.Intersects(Input.MouseBounds);
        }

        // Summary of whichever bonuses are non-zero, one per line, for
        // equipped-item hover tooltips (Armor, Ring) — previously joined
        // with ", " onto a single line, per direct feedback that each stat
        // should stand on its own line vertically instead.
        protected string BonusSummary()
        {
            List<string> parts = [];

            if (MaxHealthBonus != 0)
                parts.Add($"+{MaxHealthBonus} MaxHealth");
            if (MaxManaBonus != 0)
                parts.Add($"+{MaxManaBonus} MaxMana");
            if (AttackBonus != 0)
                parts.Add($"+{AttackBonus} Attack");
            if (DefenseBonus != 0)
                parts.Add($"+{DefenseBonus} Defense");
            if (SpeedBonus != 0)
                parts.Add($"+{SpeedBonus} Speed");
            if (DexterityBonus != 0)
                parts.Add($"+{DexterityBonus} Dexterity");
            if (VitalityBonus != 0)
                parts.Add($"+{VitalityBonus} Vitality");
            if (WisdomBonus != 0)
                parts.Add($"+{WisdomBonus} Wisdom");
            if (XpBonusPercent != 0)
                parts.Add($"+{XpBonusPercent}% XP");

            return parts.Count > 0 ? string.Join(Environment.NewLine, parts) : "No bonuses";
        }

        // Tier/name/description/bonuses, as shown on hover both in the equip
        // slot (DrawEquipped) and — for whichever item a bank/inventory slot
        // holds — BankSystem's hover tooltip. Covers Armor/Ring as-is; Weapon
        // and AbilityItem override this for their own extra stat line.
        public virtual string TooltipText()
        {
            string description = Util.WrapText(Art.RetroFont, Description, 350);
            return $"T{Tier} - {Name}{Environment.NewLine}{description}{Environment.NewLine}{BonusSummary()}";
        }

        // Tier/name/description as individual, uncolored lines — shared by
        // ComparisonLines() below and each subclass's override, so the
        // header doesn't need re-deriving per type.
        protected List<(string Text, TooltipComparison Comparison)> HeaderLines()
        {
            var lines = new List<(string, TooltipComparison)>
            {
                ($"T{Tier} - {Name}", TooltipComparison.Same),
            };
            string description = Util.WrapText(Art.RetroFont, Description, 350);

            // WrapText() inserts a bare "\n" between wrapped lines, not
            // Environment.NewLine ("\r\n" on Windows) — splitting on the
            // latter here would never match, collapsing every wrapped line
            // of a long description into a single list entry and throwing
            // off every line position that comes after it.
            foreach (string line in description.Split('\n'))
                lines.Add((line, TooltipComparison.Same));
            return lines;
        }

        // Same stats as BonusSummary(), but as individual delta-based
        // lines against `equipped` — used by the inventory/bank/loot-bag
        // hover tooltip (InventorySystem.Draw()/BankSystem.Draw()/
        // LootBag.DrawLoot()), where "how does this compare to what I have
        // on" is the actual question being asked. `equipped` is the
        // player's current Weapon/Armor/Ring/AbilityItem for this slot —
        // never null (an empty slot is a real, zero-stat placeholder
        // object, not a null reference), so an unequipped slot naturally
        // compares as "anything beats nothing."
        //
        // A stat shows whenever this item has it OR there's an actual
        // difference from what's equipped — not just "whenever this item
        // has it" like the old absolute-value-only version. Better lines
        // still show this item's own absolute value (e.g. "+5 Defense"),
        // matching the original display; Worse lines show the actual
        // negative delta instead (e.g. "-1 Defense" hovering a T0 Robe
        // while a T1 Robe — DefenseBonus 1 — is equipped), since showing
        // "+0 Defense" for a decrease would be meaningless; Same lines
        // (this item matches the equipped value, both nonzero) show the
        // absolute value too, now visible instead of silently omitted.
        protected List<(string Text, TooltipComparison Comparison)> BonusComparisonLines(
            Equipment equipped
        )
        {
            var lines = new List<(string, TooltipComparison)>();

            void AddInt(int mine, int theirs, string label)
            {
                int delta = mine - theirs;
                if (mine == 0 && delta == 0)
                    return;
                if (delta > 0)
                    lines.Add(($"+{mine} {label}", TooltipComparison.Better));
                else if (delta < 0)
                    lines.Add(($"{delta} {label}", TooltipComparison.Worse));
                else
                    lines.Add(($"+{mine} {label}", TooltipComparison.Same));
            }

            void AddFloat(float mine, float theirs, string label)
            {
                float delta = mine - theirs;
                if (mine == 0 && delta == 0)
                    return;
                if (delta > 0)
                    lines.Add(($"+{mine} {label}", TooltipComparison.Better));
                else if (delta < 0)
                    lines.Add(($"{delta} {label}", TooltipComparison.Worse));
                else
                    lines.Add(($"+{mine} {label}", TooltipComparison.Same));
            }

            AddInt(MaxHealthBonus, equipped.MaxHealthBonus, "MaxHealth");
            AddInt(MaxManaBonus, equipped.MaxManaBonus, "MaxMana");
            AddInt(AttackBonus, equipped.AttackBonus, "Attack");
            AddInt(DefenseBonus, equipped.DefenseBonus, "Defense");
            AddFloat(SpeedBonus, equipped.SpeedBonus, "Speed");
            AddInt(DexterityBonus, equipped.DexterityBonus, "Dexterity");
            AddInt(VitalityBonus, equipped.VitalityBonus, "Vitality");
            AddInt(WisdomBonus, equipped.WisdomBonus, "Wisdom");

            float xpDelta = XpBonusPercent - equipped.XpBonusPercent;
            if (XpBonusPercent != 0 || xpDelta != 0)
            {
                if (xpDelta > 0)
                    lines.Add(($"+{XpBonusPercent}% XP", TooltipComparison.Better));
                else if (xpDelta < 0)
                    lines.Add(($"{xpDelta}% XP", TooltipComparison.Worse));
                else
                    lines.Add(($"+{XpBonusPercent}% XP", TooltipComparison.Same));
            }

            if (lines.Count == 0)
                lines.Add(("No bonuses", TooltipComparison.Same));

            return lines;
        }

        // ComparisonLines() equivalent of TooltipText() — same content, but
        // as individual lines a caller can color per-line rather than one
        // flat string. Covers Armor/Ring as-is; Weapon and AbilityItem
        // override this the same way they override TooltipText().
        public virtual List<(string Text, TooltipComparison Comparison)> ComparisonLines(
            Equipment equipped
        )
        {
            var lines = HeaderLines();
            lines.AddRange(BonusComparisonLines(equipped));
            return lines;
        }
    }
}
