using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Realm
{
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

        // One-line summary of whichever bonuses are non-zero, for equipped-item
        // hover tooltips (Armor, Ring).
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

            return parts.Count > 0 ? string.Join(", ", parts) : "No bonuses";
        }

        // Tier/name/description/bonuses, as shown on hover both in the equip
        // slot (DrawEquipped) and — for whichever item a bank/inventory slot
        // holds — BankSystem's hover tooltip. Covers Armor/Ring as-is; Weapon
        // and AbilityItem override this for their own extra stat line.
        public virtual string TooltipText()
        {
            string description = Util.WrapText(Art.HudFont, Description, 350);
            return $"T{Tier} - {Name}{Environment.NewLine}{description}{Environment.NewLine}{BonusSummary()}";
        }

        // Tier/name/description as individual, uncolored lines — shared by
        // ComparisonLines() below and each subclass's override, so the
        // header doesn't need re-deriving per type.
        protected List<(string Text, bool Better)> HeaderLines()
        {
            var lines = new List<(string, bool)> { ($"T{Tier} - {Name}", false) };
            string description = Util.WrapText(Art.HudFont, Description, 350);

            // WrapText() inserts a bare "\n" between wrapped lines, not
            // Environment.NewLine ("\r\n" on Windows) — splitting on the
            // latter here would never match, collapsing every wrapped line
            // of a long description into a single list entry and throwing
            // off every line position that comes after it.
            foreach (string line in description.Split('\n'))
                lines.Add((line, false));
            return lines;
        }

        // Same content as BonusSummary(), but as individual lines, each
        // flagged for whether THIS item's value beats the equivalent stat on
        // `equipped` — used to highlight upgrades in the inventory/bank
        // hover tooltip (InventorySystem.Draw()/BankSystem.Draw()), where
        // "is this better than what I have on" is the actual question being
        // asked. `equipped` is the player's current Weapon/Armor/Ring/
        // AbilityItem for this slot — never null (an empty slot is a real,
        // zero-stat placeholder object, not a null reference), so an unequipped
        // slot naturally compares as "anything beats nothing."
        protected List<(string Text, bool Better)> BonusComparisonLines(Equipment equipped)
        {
            var lines = new List<(string, bool)>();

            void AddInt(int mine, int theirs, string label)
            {
                if (mine != 0)
                    lines.Add(($"+{mine} {label}", mine > theirs));
            }

            void AddFloat(float mine, float theirs, string label)
            {
                if (mine != 0)
                    lines.Add(($"+{mine} {label}", mine > theirs));
            }

            AddInt(MaxHealthBonus, equipped.MaxHealthBonus, "MaxHealth");
            AddInt(MaxManaBonus, equipped.MaxManaBonus, "MaxMana");
            AddInt(AttackBonus, equipped.AttackBonus, "Attack");
            AddInt(DefenseBonus, equipped.DefenseBonus, "Defense");
            AddFloat(SpeedBonus, equipped.SpeedBonus, "Speed");
            AddInt(DexterityBonus, equipped.DexterityBonus, "Dexterity");
            AddInt(VitalityBonus, equipped.VitalityBonus, "Vitality");
            AddInt(WisdomBonus, equipped.WisdomBonus, "Wisdom");
            if (XpBonusPercent != 0)
                lines.Add(($"+{XpBonusPercent}% XP", XpBonusPercent > equipped.XpBonusPercent));

            if (lines.Count == 0)
                lines.Add(("No bonuses", false));

            return lines;
        }

        // ComparisonLines() equivalent of TooltipText() — same content, but
        // as individual lines a caller can color per-line rather than one
        // flat string. Covers Armor/Ring as-is; Weapon and AbilityItem
        // override this the same way they override TooltipText().
        public virtual List<(string Text, bool Better)> ComparisonLines(Equipment equipped)
        {
            var lines = HeaderLines();
            lines.AddRange(BonusComparisonLines(equipped));
            return lines;
        }
    }
}
