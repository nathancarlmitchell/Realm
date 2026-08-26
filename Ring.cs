using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    public class Ring : Equipment
    {
        // Last in the equipment row: Weapon, AbilityItem, Armor, then Ring.
        static int x = Game1.SidebarX + 20 + 120;
        static int y = 410;

        public Ring(Texture2D image)
        {
            ID = Guid.NewGuid();
            this.image = image;
            SlotBounds = new Rectangle(x, y, 40, 40);
        }

        public Ring()
        {
            SlotBounds = new Rectangle(x, y, 40, 40);
        }

        public static Ring LoadRing(string ringName)
        {
            Ring ringData = Game1.Instance.Rings.FirstOrDefault(x => (x.Name == ringName));

            // No class restriction — any Ring can be equipped by any class.
            if (ringData != null)
            {
                Texture2D ringTexture = Game1.Instance.Content.Load<Texture2D>(
                    ringData.ImageName
                );

                Ring ring = new(ringTexture)
                {
                    Name = ringData.Name,
                    Description = ringData.Description,
                    Tier = ringData.Tier,
                    MaxHealthBonus = ringData.MaxHealthBonus,
                    MaxManaBonus = ringData.MaxManaBonus,
                    AttackBonus = ringData.AttackBonus,
                    DefenseBonus = ringData.DefenseBonus,
                    SpeedBonus = ringData.SpeedBonus,
                    DexterityBonus = ringData.DexterityBonus,
                    VitalityBonus = ringData.VitalityBonus,
                    WisdomBonus = ringData.WisdomBonus,
                    ImageName = ringData.ImageName,
                };

                Player.Instance.EquipRing(ring);
                return ring;
            }

            Sound.Play(Sound.Error, 0.4f);
            return null;
        }

        // Tier 0 ring — shown greyed out in an empty slot as a placeholder,
        // same idea as Weapon.PlaceholderImage. No class restriction on
        // Ring, so this doesn't need to filter by the player's class.
        private static Texture2D PlaceholderImage =>
            Game1.Instance.Rings.FirstOrDefault(r => r.Tier == 0)?.image;

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
