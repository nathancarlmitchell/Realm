using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Realm
{
    public static class Background
    {
        private static Texture2D backgroundTexture;

        static Background()
        {
            backgroundTexture = Art.Background;
        }

        // Cover-scaled (like CSS background-size: cover) rather than
        // stretched — backgroundTexture's own aspect ratio (768x512) isn't
        // the window's (1280x720), and stretching to fill exactly would
        // visibly distort the pixel art. Scales up by whichever axis needs
        // it more, then centers, cropping the overflow on the other axis.
        public static void Draw(SpriteBatch spriteBatch)
        {
            float scale = Math.Max(
                (float)Game1.ScreenWidth / backgroundTexture.Width,
                (float)Game1.ScreenHeight / backgroundTexture.Height
            );
            int drawWidth = (int)(backgroundTexture.Width * scale);
            int drawHeight = (int)(backgroundTexture.Height * scale);
            int x = (Game1.ScreenWidth - drawWidth) / 2;
            int y = (Game1.ScreenHeight - drawHeight) / 2;

            spriteBatch.Draw(
                backgroundTexture,
                new Rectangle(x, y, drawWidth, drawHeight),
                Color.White
            );
        }
    }
}
