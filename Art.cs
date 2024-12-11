using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    static class Art
    {
        public static Texture2D ButtonTexture { get; private set; }
        public static Texture2D Background { get; private set; }
        public static Texture2D Projectile { get; private set; }
        public static Texture2D Enemy { get; private set; }
        public static Texture2D Enemy2 { get; private set; }
        public static Texture2D Player { get; private set; }
        public static Texture2D HealthBar { get; private set; }
        public static SpriteFont HudFont { get; private set; }
        public static SpriteFont TitleFont { get; private set; }

        public static void Load(ContentManager content)
        {
            ButtonTexture = content.Load<Texture2D>("Controls/Button");
            Background = content.Load<Texture2D>("background");
            Projectile = content.Load<Texture2D>("projectile");
            Enemy = content.Load<Texture2D>("enemy");
            Enemy2 = content.Load<Texture2D>("enemy2");
            Player = content.Load<Texture2D>("player");

            HealthBar = new Texture2D(Game1.Instance.GraphicsDevice, 1, 1);
            HealthBar.SetData(new[] { Color.White });

            //Player = new AnimatedTexture(new Vector2(0, 0), 0, 1f, 0.5f);
            //Player.Load(content, "player", 2, 2);

            HudFont = content.Load<SpriteFont>("Fonts/HudFont");
            TitleFont = content.Load<SpriteFont>("Fonts/TitleFont");
        }
    }
}
