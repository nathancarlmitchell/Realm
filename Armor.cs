using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm.Data;

namespace Realm
{
    public class Armor : Equipment
    {
        public enum ArmorType
        {
            Robe, // 0 — Wizard only
            Leather, // 1 — Archer only
            Heavy, // 2 — Knight only
        }

        public ArmorType Type { get; set; }

        public override bool CanEquipByCurrentClass => Type == Player.Instance.ArmorType;

        // Third in the equipment row: Weapon, then AbilityItem, then Armor
        // (Weapon.cs uses the same x/y origin, offset by two 40px slots).
        static int x = Game1.SidebarX + 20 + 80;
        static int y = 410;

        public Armor(Texture2D image)
        {
            ID = Guid.NewGuid();
            this.image = image;
            SlotBounds = new Rectangle(x, y, 40, 40);
        }

        public Armor()
        {
            SlotBounds = new Rectangle(x, y, 40, 40);
        }

        public static Armor LoadArmor(string armorName)
        {
            Armor armorData = Game1.Instance.Armors.FirstOrDefault(x => (x.Name == armorName));

            // armorData is null if armorName doesn't match anything in
            // Game1.Instance.Armors (the merged Robe/Leather/Heavy catalog —
            // see Game1.StartGame()). A Type mismatch (e.g. an Archer trying
            // to equip a Robe) is also rejected here, same as
            // Weapon.LoadWeapon does for WeaponType.
            if (armorData != null && armorData.Type == Player.Instance.ArmorType)
            {
                Texture2D armorTexture = Game1.Instance.Content.Load<Texture2D>(
                    armorData.ImageName
                );

                Armor armor = new(armorTexture)
                {
                    Name = armorData.Name,
                    Description = armorData.Description,
                    Type = armorData.Type,
                    Tier = armorData.Tier,
                    MaxHealthBonus = armorData.MaxHealthBonus,
                    MaxManaBonus = armorData.MaxManaBonus,
                    AttackBonus = armorData.AttackBonus,
                    DefenseBonus = armorData.DefenseBonus,
                    SpeedBonus = armorData.SpeedBonus,
                    DexterityBonus = armorData.DexterityBonus,
                    VitalityBonus = armorData.VitalityBonus,
                    WisdomBonus = armorData.WisdomBonus,
                    XpBonusPercent = armorData.XpBonusPercent,
                    ImageName = armorData.ImageName,
                };

                Player.Instance.EquipArmor(armor);
                return armor;
            }

            Sound.Play(Sound.Error, 0.4f);
            return null;
        }

        // The current class's Tier 0 armor — shown greyed out in an empty
        // slot as a placeholder, same idea as Weapon.PlaceholderImage.
        private static Texture2D PlaceholderImage =>
            Game1
                .Instance.Armors.FirstOrDefault(a =>
                    a.Type == Player.Instance.ArmorType && a.Tier == 0
                )
                ?.image;

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
    }
}
