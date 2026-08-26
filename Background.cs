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

        // Applied on top of the cover-scale below to zoom back out a bit —
        // a full 1.0 cover-scale fills the window exactly but crops
        // whichever axis overflows (the full 512px-tall image doesn't fit
        // the 720-tall window at cover-scale's width-driven zoom), so
        // scaling down from that reveals more of the actual artwork at the
        // cost of some letterboxing. Tune this directly if more/less of the
        // image should show.
        private const float ZoomOutFactor = 0.8f;

        // Cover-scaled (like CSS background-size: cover) rather than
        // stretched — backgroundTexture's own aspect ratio (768x512) isn't
        // the window's (1280x720), and stretching to fill exactly would
        // visibly distort the pixel art. Scales up by whichever axis needs
        // it more, then centers, cropping the overflow on the other axis
        // (before ZoomOutFactor pulls back out again).
        //
        // opacity multiplies the draw color's alpha (0 = invisible, 1 =
        // fully opaque) — callers fading the background in over time pass a
        // ramping value here rather than this method tracking any timing
        // itself, since how long/whether to fade is a per-screen decision.
        public static void Draw(SpriteBatch spriteBatch, float opacity = 1f)
        {
            float scale =
                Math.Max(
                    (float)Game1.ScreenWidth / backgroundTexture.Width,
                    (float)Game1.ScreenHeight / backgroundTexture.Height
                ) * ZoomOutFactor;
            int drawWidth = (int)(backgroundTexture.Width * scale);
            int drawHeight = (int)(backgroundTexture.Height * scale);
            int x = (Game1.ScreenWidth - drawWidth) / 2;
            int y = (Game1.ScreenHeight - drawHeight) / 2;

            spriteBatch.Draw(
                backgroundTexture,
                new Rectangle(x, y, drawWidth, drawHeight),
                Color.White * opacity
            );
        }
    }
}
