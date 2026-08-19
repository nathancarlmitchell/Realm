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
        public static Texture2D Limon { get; private set; }
        public static Texture2D LimonProjectile { get; private set; }
        public static Texture2D Snake { get; private set; }
        public static Texture2D BigSnake { get; private set; }
        public static Texture2D Wizard { get; private set; }
        public static Texture2D Archer { get; private set; }
        public static Texture2D Knight { get; private set; }
        public static Texture2D ArcherProjectile { get; private set; }
        public static Texture2D ShieldProjectile { get; private set; }
        public static AnimatedTexture Portal { get; private set; }
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
        public static Texture2D Vitality { get; private set; }
        public static Texture2D Wisdom { get; private set; }

        // Loot bags.
        public static Texture2D LootBag { get; private set; }
        public static Texture2D LootBagPink { get; private set; }
        public static Texture2D LootBagPurple { get; private set; }
        public static Texture2D LootBagBlue { get; private set; }
        public static Texture2D LootBagWhite { get; private set; }
        public static Texture2D LootBagGold { get; private set; }

        // Status effects (debuff indicator icons — see Entity.DrawDebuffIndicators).
        public static Texture2D Paralyzed { get; private set; }
        public static Texture2D Stunned { get; private set; }

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
            LootBagPink = content.Load<Texture2D>("Items/Bags/pink");
            LootBagPurple = content.Load<Texture2D>("Items/Bags/purple");
            LootBagBlue = content.Load<Texture2D>("Items/Bags/blue");
            LootBagWhite = content.Load<Texture2D>("Items/Bags/white");
            LootBagGold = content.Load<Texture2D>("Items/Bags/gold");

            // Controls.
            ButtonTexture = content.Load<Texture2D>("Controls/Button");

            // Overlay.
            Mute = content.Load<Texture2D>("Overlay/mute");
            Unmute = content.Load<Texture2D>("Overlay/unmute");
            Border = content.Load<Texture2D>("Overlay/border");

            HealthBar = new Texture2D(Game1.Instance.GraphicsDevice, 1, 1);
            HealthBar.SetData(new[] { Color.White });

            // Player.
            Wizard = content.Load<Texture2D>("Classes/wizard");
            Projectile = content.Load<Texture2D>("projectile");
            Projectile2 = content.Load<Texture2D>("projectile2");

            Archer = content.Load<Texture2D>("Classes/archer");
            ArcherProjectile = content.Load<Texture2D>("Projectiles/archer");

            Knight = content.Load<Texture2D>("Classes/knight");
            ShieldProjectile = content.Load<Texture2D>("Projectiles/shield");

            Inventory = content.Load<Texture2D>("inventory");

            // Enemies.
            EnemyProjectile = content.Load<Texture2D>("enemy_projectile");
            Enemy = content.Load<Texture2D>("enemy");
            Enemy2 = content.Load<Texture2D>("enemy2");
            EnemySpriteGod = content.Load<Texture2D>("Enemies/sprite_god");
            Limon = content.Load<Texture2D>("Enemies/Limon the Sprite Goddess");
            LimonProjectile = content.Load<Texture2D>("Projectiles/limon1");
            Snake = content.Load<Texture2D>("snake");
            BigSnake = content.Load<Texture2D>("Enemies/big_snake");

            // Weapons.
            Wand = content.Load<Texture2D>("Weapons/Wands/wand");

            // Stat potions.
            Attack = content.Load<Texture2D>("Items/Potions/attack");
            Defense = content.Load<Texture2D>("Items/Potions/defense");
            Dexterity = content.Load<Texture2D>("Items/Potions/dexterity");
            Life = content.Load<Texture2D>("Items/Potions/life");
            Mana = content.Load<Texture2D>("Items/Potions/mana");
            Speed = content.Load<Texture2D>("Items/Potions/speed");
            Vitality = content.Load<Texture2D>("Items/Potions/vitality");
            Wisdom = content.Load<Texture2D>("Items/Potions/wisdom");

            // Status effects.
            Paralyzed = content.Load<Texture2D>("StatusEffects/paralyzed");
            Stunned = content.Load<Texture2D>("StatusEffects/stunned");

            // Fonts.
            HudFont = content.Load<SpriteFont>("Fonts/HudFont");
            TitleFont = content.Load<SpriteFont>("Fonts/TitleFont");
        }
    }
}
