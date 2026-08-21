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
        public static Texture2D Stheno { get; private set; }
        public static Texture2D SthenoPet { get; private set; }
        public static Texture2D SthenoPetProjectile { get; private set; }
        public static Texture2D SthenoSwarm { get; private set; }
        public static Texture2D SthenoSwarmProjectile { get; private set; }
        public static Texture2D SthenoBladeProjectile { get; private set; }
        public static Texture2D SwordSlash { get; private set; }
        public static Texture2D RedFire { get; private set; }
        public static Texture2D PurpleMagic { get; private set; }
        public static Texture2D GreenMagic { get; private set; }
        public static Texture2D Snake { get; private set; }
        public static Texture2D BigSnake { get; private set; }
        public static Texture2D Wizard { get; private set; }
        public static Texture2D Archer { get; private set; }
        public static Texture2D Knight { get; private set; }
        public static Texture2D ArcherProjectile { get; private set; }
        public static Texture2D ShieldProjectile { get; private set; }
        public static AnimatedTexture Portal { get; private set; }

        // Dungeon-specific portal animations — same 7-frame, 8fps loop as
        // the generic Portal above, just laid out 5-wide/2-row instead of
        // one long strip (see AnimatedTexture's columns param). Only used
        // for portals leading into a specific boss's dungeon (see
        // Portal.Destination.BossDestination); every other portal keeps
        // the plain swirl above.
        public static AnimatedTexture SpriteWorldPortal { get; private set; }
        public static AnimatedTexture SnakePitPortal { get; private set; }

        // The boss arena's own exit portal (see
        // Portal.Destination.NexusDestination) — a single static frame, not
        // a real animation, so it's loaded as a 1-frame AnimatedTexture
        // rather than a plain Texture2D. That still works cleanly with
        // Portal's existing AnimatedTexture-typed `image` field/DrawFrame()
        // call with no special-casing: frameCount 1 just means UpdateFrame()
        // never has anywhere to advance to.
        public static AnimatedTexture NexusPortal { get; private set; }

        // The Bank portal (see Portal.Destination.BankDestination) — same
        // single-static-frame treatment as NexusPortal above, a chest icon
        // rather than a swirl/doorway.
        public static AnimatedTexture BankPortal { get; private set; }

        // The main Realm portal (see Portal.Destination.RealmDestination) —
        // same single-static-frame treatment, a stone archway.
        public static AnimatedTexture RealmPortal { get; private set; }

        // The Character Select portal (see
        // Portal.Destination.CharacterSelectDestination) — same
        // single-static-frame treatment, a warrior figure.
        public static AnimatedTexture CharacterSelectPortal { get; private set; }

        public static Texture2D Inventory { get; private set; }
        public static Texture2D HealthPotion { get; private set; }
        public static Texture2D ManaPotion { get; private set; }
        public static Texture2D HealthBar { get; private set; }
        public static Texture2D Mute { get; private set; }
        public static Texture2D Unmute { get; private set; }

        // A filled white circle, generated at runtime rather than loaded
        // from disk — same reasoning as HealthBar above (a 1x1 pixel
        // stretched into rectangles/lines), except a solid-color square
        // can't be scaled into a circle, so this needs real per-pixel data.
        // Used for telegraphed-AoE indicators (see GrenadeProjectile) where
        // the drawn shape needs to actually match a circular hitbox radius,
        // tinted/scaled per use rather than baked into fixed art.
        public static Texture2D Circle { get; private set; }

        public static SpriteFont HudFont { get; private set; }
        public static SpriteFont TitleFont { get; private set; }

        // Bold, slightly larger than HudFont — used for floating combat
        // damage numbers (DamageNumber.cs), which need to read clearly at
        // a glance over busy backgrounds; HudFont's regular weight stayed
        // hard to read even after bumping its draw scale up.
        public static SpriteFont DamageFont { get; private set; }

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
        public static Texture2D Slowed { get; private set; }

        public static void Load(ContentManager content)
        {
            // World.
            Background = content.Load<Texture2D>("background");
            Tile = content.Load<Texture2D>("tile");
            Portal = new AnimatedTexture(Vector2.Zero, 0f, 1.0f, 0.5f);
            Portal.Load(content, "portal", 7, 8);

            // Non-looping: this sheet is a one-shot "opening" animation
            // (closed -> forming -> fully open), not a continuous idle
            // loop like the generic swirl — it should play through once
            // and hold on the final open frame instead of replaying
            // "closed" forever.
            SpriteWorldPortal = new AnimatedTexture(Vector2.Zero, 0f, 1.0f, 0.5f);
            SpriteWorldPortal.Load(content, "Sprite World Portal", 7, 16, 5, loop: false);

            SnakePitPortal = new AnimatedTexture(Vector2.Zero, 0f, 1.0f, 0.5f);
            SnakePitPortal.Load(content, "Snake Pit Portal", 7, 8, 5);

            NexusPortal = new AnimatedTexture(Vector2.Zero, 0f, 1.0f, 0.5f);
            NexusPortal.Load(content, "Portal to Nexus", 1, 1);

            BankPortal = new AnimatedTexture(Vector2.Zero, 0f, 1.0f, 0.5f);
            BankPortal.Load(content, "Vault Chest", 1, 1);

            RealmPortal = new AnimatedTexture(Vector2.Zero, 0f, 1.0f, 0.5f);
            RealmPortal.Load(content, "Portal to Realm", 1, 1);

            CharacterSelectPortal = new AnimatedTexture(Vector2.Zero, 0f, 1.0f, 0.5f);
            CharacterSelectPortal.Load(content, "Character Changer", 1, 1);

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

            Circle = GenerateCircleTexture(Game1.Instance.GraphicsDevice, 64);

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
            Stheno = content.Load<Texture2D>("Enemies/Stheno the Snake Queen");
            SthenoPet = content.Load<Texture2D>("Enemies/Stheno Pet");
            SthenoPetProjectile = content.Load<Texture2D>("Projectiles/Stheno Pet");
            SthenoSwarm = content.Load<Texture2D>("Enemies/Stheno Swarm");
            SthenoSwarmProjectile = content.Load<Texture2D>("Projectiles/Stheno Swarm");
            SthenoBladeProjectile = content.Load<Texture2D>("Projectiles/Stheno Blade");
            SwordSlash = content.Load<Texture2D>("Projectiles/sword_slash");
            RedFire = content.Load<Texture2D>("Projectiles/red_fire");
            PurpleMagic = content.Load<Texture2D>("Projectiles/purple_magic");
            GreenMagic = content.Load<Texture2D>("Projectiles/green_magic");
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
            Slowed = content.Load<Texture2D>("StatusEffects/slowed");

            // Fonts.
            HudFont = content.Load<SpriteFont>("Fonts/HudFont");
            TitleFont = content.Load<SpriteFont>("Fonts/TitleFont");
            DamageFont = content.Load<SpriteFont>("Fonts/DamageFont");
        }

        // Hard-edged filled circle, diameter x diameter, opaque white
        // inside the radius and fully transparent outside it — drawn once
        // at startup rather than per-frame, then tinted/scaled at draw time
        // by whoever uses it (see Circle above).
        private static Texture2D GenerateCircleTexture(GraphicsDevice device, int diameter)
        {
            var texture = new Texture2D(device, diameter, diameter);
            var data = new Color[diameter * diameter];
            float radius = diameter / 2f;
            Vector2 center = new(radius, radius);

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    data[y * diameter + x] = distance <= radius ? Color.White : Color.Transparent;
                }
            }

            texture.SetData(data);
            return texture;
        }
    }
}
