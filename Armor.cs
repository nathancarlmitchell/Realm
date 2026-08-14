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

        // Directly to the right of the weapon slot (Weapon.cs uses the same x/y
        // origin, offset by its 40px border width).
        static int x = Game1.SidebarX + 20 + 40;
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

            // armorData is null if armorName doesn't match anything in ArmorData.json.
            // A Type mismatch (e.g. an Archer trying to equip a Robe) is also
            // rejected here, same as Weapon.LoadWeapon does for WeaponType.
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
                    ImageName = armorData.ImageName,
                };

                Player.Instance.EquipArmor(armor);
                return armor;
            }

            Sound.Play(Sound.Error, 0.4f);
            return null;
        }

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
    }
}
