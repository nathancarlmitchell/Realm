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
    }
}
