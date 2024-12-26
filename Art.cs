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
        public static Texture2D Border { get; private set; }
        public static Texture2D Projectile { get; private set; }
        public static Texture2D Projectile2 { get; private set; }
        public static Texture2D EnemyProjectile { get; private set; }
        public static Texture2D Tile { get; private set; }
        public static Texture2D Enemy { get; private set; }
        public static Texture2D Enemy2 { get; private set; }
        public static Texture2D EnemySpriteGod { get; private set; }
        public static Texture2D Snake { get; private set; }
        public static Texture2D Wizard { get; private set; }
        public static Texture2D Archer { get; private set; }
        public static Texture2D ArcherProjectile { get; private set; }
        public static AnimatedTexture Portal { get; private set; }
        public static Texture2D Item { get; private set; }
        public static Texture2D Inventory { get; private set; }
        public static Texture2D HealthPotion { get; private set; }
        public static Texture2D ManaPotion { get; private set; }
        public static Texture2D HealthBar { get; private set; }
        public static Texture2D Mute { get; private set; }
        public static Texture2D Unmute { get; private set; }

        public static SpriteFont HudFont { get; private set; }
        public static SpriteFont TitleFont { get; private set; }

        // Weapons.
        public static Texture2D Wand { get; private set; }

        // Stat potions.
        public static Texture2D Attack { get; private set; }
        public static Texture2D Defense { get; private set; }
        public static Texture2D Dexterity { get; private set; }
        public static Texture2D Life { get; private set; }
        public static Texture2D Mana { get; private set; }
        public static Texture2D Speed { get; private set; }
        public static Texture2D Vitalty { get; private set; }
        public static Texture2D Wisdom { get; private set; }

        // Loot bags.
        public static Texture2D LootBag { get; private set; }
        public static Texture2D LootBagBlue { get; private set; }
        public static Texture2D LootBagWhite { get; private set; }

        public static void Load(ContentManager content)
        {
            // World.
            Background = content.Load<Texture2D>("background");
            Tile = content.Load<Texture2D>("tile");
            Portal = new AnimatedTexture(Vector2.Zero, 0f, 1.5f, 0.5f);
            Portal.Load(content, "portal", 7, 8);
            HealthPotion = content.Load<Texture2D>("health_potion");
            ManaPotion = content.Load<Texture2D>("mana_potion");

            // Loot bags.
            LootBag = content.Load<Texture2D>("loot_bag");
            LootBagBlue = content.Load<Texture2D>("Items/Bags/blue");
            LootBagWhite = content.Load<Texture2D>("Items/Bags/white");

            // Controls.
            ButtonTexture = content.Load<Texture2D>("Controls/Button");

            // Overlay.
            Mute = content.Load<Texture2D>("Overlay/mute");
            Unmute = content.Load<Texture2D>("Overlay/unmute");
            Border = content.Load<Texture2D>("Overlay/border");

            HealthBar = new Texture2D(Game1.Instance.GraphicsDevice, 1, 1);
            HealthBar.SetData(new[] { Color.White });

            // Player.
            Wizard = content.Load<Texture2D>("player");
            Projectile = content.Load<Texture2D>("projectile");
            Projectile2 = content.Load<Texture2D>("projectile2");

            Archer = content.Load<Texture2D>("Classes/archer");
            ArcherProjectile = content.Load<Texture2D>("Projectiles/archer");

            Item = content.Load<Texture2D>("item");
            Inventory = content.Load<Texture2D>("inventory");

            // Enemies.
            EnemyProjectile = content.Load<Texture2D>("enemy_projectile");
            Enemy = content.Load<Texture2D>("enemy");
            Enemy2 = content.Load<Texture2D>("enemy2");
            EnemySpriteGod = content.Load<Texture2D>("Enemies/sprite_god");
            Snake = content.Load<Texture2D>("snake");

            // Weapons.
            Wand = content.Load<Texture2D>("Weapons/Wands/wand");

            // Stat potions.
            Attack = content.Load<Texture2D>("Items/Potions/attack");
            Defense = content.Load<Texture2D>("Items/Potions/defense");
            Dexterity = content.Load<Texture2D>("Items/Potions/dexterity");
            Life = content.Load<Texture2D>("Items/Potions/life");
            Mana = content.Load<Texture2D>("Items/Potions/mana");
            Speed = content.Load<Texture2D>("Items/Potions/speed");
            Vitalty = content.Load<Texture2D>("Items/Potions/vitality");
            Wisdom = content.Load<Texture2D>("Items/Potions/wisdom");

            // Fonts.
            HudFont = content.Load<SpriteFont>("Fonts/HudFont");
            TitleFont = content.Load<SpriteFont>("Fonts/TitleFont");
        }
    }
}
