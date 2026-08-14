using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    public class Ring : Equipment
    {
        // Continues the equipment row: Weapon, then Armor 40px to the right,
        // then Ring another 40px past that.
        static int x = Game1.SidebarX + 20 + 80;
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
