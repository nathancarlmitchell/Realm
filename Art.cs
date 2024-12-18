﻿using System;
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
        public static Texture2D Projectile2 { get; private set; }
        public static Texture2D EnemyProjectile { get; private set; }
        public static Texture2D Tile { get; private set; }
        public static Texture2D Enemy { get; private set; }
        public static Texture2D Enemy2 { get; private set; }
        public static Texture2D EnemySpriteGod { get; private set; }
        public static Texture2D Snake { get; private set; }
        public static Texture2D Player { get; private set; }
        public static Texture2D Portal { get; private set; }
        public static Texture2D Item { get; private set; }
        public static Texture2D Inventory { get; private set; }
        public static Texture2D HealthPotion { get; private set; }
        public static Texture2D ManaPotion { get; private set; }
        public static Texture2D HealthBar { get; private set; }
        public static SpriteFont HudFont { get; private set; }
        public static SpriteFont TitleFont { get; private set; }

        // Weapons.
        public static Texture2D Wand { get; private set; }

        public static void Load(ContentManager content)
        {
            ButtonTexture = content.Load<Texture2D>("Controls/Button");
            Background = content.Load<Texture2D>("background");
            Projectile = content.Load<Texture2D>("projectile");
            Projectile2 = content.Load<Texture2D>("projectile2");
            EnemyProjectile = content.Load<Texture2D>("enemy_projectile");
            Tile = content.Load<Texture2D>("tile");
            Enemy = content.Load<Texture2D>("enemy");
            Enemy2 = content.Load<Texture2D>("enemy2");
            EnemySpriteGod = content.Load<Texture2D>("Enemies/sprite_god");
            Snake = content.Load<Texture2D>("snake");
            Player = content.Load<Texture2D>("player");
            Portal = content.Load<Texture2D>("portal");
            Item = content.Load<Texture2D>("item");
            Inventory = content.Load<Texture2D>("inventory");
            HealthPotion = content.Load<Texture2D>("health_potion");
            ManaPotion = content.Load<Texture2D>("mana_potion");

            // Weapons.
            Wand = content.Load<Texture2D>("Weapons/Wands/wand");

            HealthBar = new Texture2D(Game1.Instance.GraphicsDevice, 1, 1);
            HealthBar.SetData(new[] { Color.White });

            //Player = new AnimatedTexture(new Vector2(0, 0), 0, 1f, 0.5f);
            //Player.Load(content, "player", 2, 2);

            HudFont = content.Load<SpriteFont>("Fonts/HudFont");
            TitleFont = content.Load<SpriteFont>("Fonts/TitleFont");
        }
    }
}
