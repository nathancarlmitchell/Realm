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

        public static void Draw(SpriteBatch spriteBatch)
        {
            // Draw background.
            spriteBatch.Draw(backgroundTexture, new Vector2(0, 0), Color.White);
        }
    }
}
