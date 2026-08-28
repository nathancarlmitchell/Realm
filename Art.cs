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
        public static Texture2D Pirate { get; private set; }
        public static Texture2D BeachedBuccaneer { get; private set; }
        public static Texture2D Bandit { get; private set; }
        public static Texture2D BanditLeader { get; private set; }
        public static Texture2D ScorpionQueen { get; private set; }
        public static Texture2D LittleScorpion { get; private set; }
        public static Texture2D SandsmanKing { get; private set; }
        public static Texture2D SandsmanArcher { get; private set; }
        public static Texture2D SandsmanSorcerer { get; private set; }
        public static Texture2D GreenArrow { get; private set; }
        public static Texture2D PurpleMysticShot { get; private set; }
        public static Texture2D DarkBlueMagic { get; private set; }
        public static Texture2D GiantCrab { get; private set; }
        public static Texture2D Beam { get; private set; }
        public static Texture2D BlueBolt { get; private set; }
        public static Texture2D LittleBlueJelly { get; private set; }
        public static Texture2D LittleGreenJelly { get; private set; }
        public static Texture2D LittlePinkJelly { get; private set; }
        public static Texture2D Piratess { get; private set; }
        public static Texture2D SandDevil { get; private set; }
        public static Texture2D BeachBeacon { get; private set; }
        public static Texture2D BlueMissile { get; private set; }
        public static Texture2D DarkGraySpinner { get; private set; }
        public static Texture2D WhiteBolt { get; private set; }
        public static Texture2D Wizard { get; private set; }
        public static Texture2D Archer { get; private set; }
        public static Texture2D Knight { get; private set; }
        public static Texture2D Priest { get; private set; }
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
        public static Texture2D FameIcon { get; private set; }
        public static Texture2D CombatBadge { get; private set; }
        public static Texture2D IndicatorArrow { get; private set; }

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

        // A dedicated font for States/SettingsState.cs — same Arial
        // family as HudFont, just a size up (14pt vs 12pt) for a menu
        // screen read at a normal, unhurried pace, rather than reusing the
        // small HUD-scale font that's tuned for compact in-game overlays.
        public static SpriteFont SettingsFont { get; private set; }

        // Jersey10 (SIL Open Font License), a bundled pixel-style TrueType
        // file rather than an installed system font family like every font
        // above. Originally added just for the sidebar bars, now the base
        // font for the in-game HUD, tooltips, damage numbers/XP drops,
        // Character Select, Settings, and (via RetroFontLarge below) the
        // title screen and boss-announcement banner — only
        // TauntBubble.cs's enemy speech bubbles still use HudFont. Chosen
        // via a side-by-side render comparison against four other free
        // pixel/retro fonts; DamageFont (previously used for damage
        // numbers) had zero remaining consumers once this replaced it and
        // was deleted outright, while SettingsFont (previously Settings'
        // own font) was left loaded but unused rather than removed, since
        // deleting it wasn't itself requested. No longer used by buttons —
        // see RetroFontButton below.
        public static SpriteFont RetroFont { get; private set; }

        // Same Jersey10 file as RetroFont, baked at a much larger native
        // point size (84pt vs 14pt) instead of relying on RetroFont's own
        // small glyph bitmap stretched up via SpriteBatch's scale parameter
        // — stretching a small rasterized font blurs badly under the
        // default linear sampler, since there's no such thing as scaling a
        // bitmap up losslessly. Used by Overlay.DrawTitle() ("Realm") and
        // BossRealmState's boss-announcement banner, both of which only
        // ever draw at native size or SMALLER (shrunk to fit a long boss
        // name) — downscaling a large bitmap loses fine detail gracefully,
        // unlike upscaling a small one.
        public static SpriteFont RetroFontLarge { get; private set; }

        // Same bundled Jersey10 file as RetroFont/RetroFontLarge, baked at
        // its own 110pt native size — sits between RetroFont's small 14pt
        // HUD size and RetroFontLarge's full 140pt title size. Used by
        // GameOverState's "Score:"/"Fame Earned:" text, which needs
        // something bigger than HUD text but smaller than the actual
        // title, drawn at native size for the same crisp-bitmap reasoning
        // as RetroFontLarge's own comment. Not used by any button — see
        // RetroFontButton below for those.
        public static SpriteFont RetroFontMedium { get; private set; }

        // Micro5 (SIL Open Font License), a separate bundled TrueType file
        // used only by Controls/Button.cs — every button in the game,
        // including Settings' own Back/Reset. A dedicated RetroFontButton
        // briefly existed and was retired in favor of RetroFont (to match
        // Settings' own buttons), then the user asked for a fresh
        // button-specific font again; chosen via a side-by-side render
        // comparison of the actual menu buttons against four other
        // candidates (DotGothic16, RubikPixels, Jacquard12, RubikGlitch)
        // plus Press Start 2P at the same 18pt — the others either
        // overflowed "Character Select"/"Reset to Defaults" at a usable
        // size or rendered illegibly; only this and Jacquard12 fit
        // cleanly, and this won.
        public static SpriteFont RetroFontButton { get; private set; }

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
        public static Texture2D LootBagCyan { get; private set; }
        public static Texture2D LootBagRed { get; private set; }
        public static Texture2D LootBagOrange { get; private set; }

        // Status effects (debuff indicator icons — see Entity.DrawDebuffIndicators).
        public static Texture2D Paralyzed { get; private set; }
        public static Texture2D Stunned { get; private set; }
        public static Texture2D Slowed { get; private set; }
        public static Texture2D Healing { get; private set; }
        public static Texture2D Unstable { get; private set; }

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
            LootBag = content.Load<Texture2D>("Items/Bags/brown");
            LootBagPink = content.Load<Texture2D>("Items/Bags/pink");
            LootBagPurple = content.Load<Texture2D>("Items/Bags/purple");
            LootBagBlue = content.Load<Texture2D>("Items/Bags/blue");
            LootBagWhite = content.Load<Texture2D>("Items/Bags/white");
            LootBagGold = content.Load<Texture2D>("Items/Bags/gold");
            LootBagCyan = content.Load<Texture2D>("Items/Bags/cyan");
            LootBagRed = content.Load<Texture2D>("Items/Bags/red");
            LootBagOrange = content.Load<Texture2D>("Items/Bags/orange");

            // Controls.
            ButtonTexture = content.Load<Texture2D>("Controls/Button");

            // Overlay.
            Mute = content.Load<Texture2D>("Overlay/mute");
            Unmute = content.Load<Texture2D>("Overlay/unmute");
            Border = content.Load<Texture2D>("Overlay/border");
            FameIcon = content.Load<Texture2D>("Overlay/Fame Icon");
            CombatBadge = content.Load<Texture2D>("Overlay/Combat Badge");
            IndicatorArrow = content.Load<Texture2D>("Overlay/Indicator Arrow");

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

            Priest = content.Load<Texture2D>("Classes/priest");

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
            Pirate = content.Load<Texture2D>("Biomes/Beach/Pirate");
            BeachedBuccaneer = content.Load<Texture2D>("Biomes/Beach/Beached Buccaneer");
            Bandit = content.Load<Texture2D>("Biomes/Beach/Bandit");
            BanditLeader = content.Load<Texture2D>("Biomes/Beach/Bandit Leader");
            ScorpionQueen = content.Load<Texture2D>("Biomes/Beach/Scorpion Queen");
            LittleScorpion = content.Load<Texture2D>("Biomes/Beach/Little Scorpion");
            SandsmanKing = content.Load<Texture2D>("Biomes/Beach/Sandsman King");
            SandsmanArcher = content.Load<Texture2D>("Biomes/Beach/Sandsman Archer");
            SandsmanSorcerer = content.Load<Texture2D>("Biomes/Beach/Sandsman Sorcerer");
            GreenArrow = content.Load<Texture2D>("Projectiles/Green Arrow");
            PurpleMysticShot = content.Load<Texture2D>("Projectiles/Purple Mystic Shot");
            DarkBlueMagic = content.Load<Texture2D>("Projectiles/Dark Blue Magic");
            GiantCrab = content.Load<Texture2D>("Biomes/Beach/Giant Crab");
            Beam = content.Load<Texture2D>("Projectiles/Beam");
            BlueBolt = content.Load<Texture2D>("Projectiles/Blue Bolt");
            LittleBlueJelly = content.Load<Texture2D>("Biomes/Beach/Little Blue Jelly");
            LittleGreenJelly = content.Load<Texture2D>("Biomes/Beach/Little Green Jelly");
            LittlePinkJelly = content.Load<Texture2D>("Biomes/Beach/Little Pink Jelly");
            Piratess = content.Load<Texture2D>("Biomes/Beach/Piratess");
            SandDevil = content.Load<Texture2D>("Biomes/Beach/Sand Devil");
            BeachBeacon = content.Load<Texture2D>("Biomes/Beach/Beach Beacon");
            BlueMissile = content.Load<Texture2D>("Projectiles/Blue Missile");
            DarkGraySpinner = content.Load<Texture2D>("Projectiles/Dark Gray Spinner");
            WhiteBolt = content.Load<Texture2D>("Projectiles/white_bolt");

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
            Healing = content.Load<Texture2D>("StatusEffects/healing");
            Unstable = content.Load<Texture2D>("StatusEffects/unstable");

            // Fonts.
            HudFont = content.Load<SpriteFont>("Fonts/HudFont");
            TitleFont = content.Load<SpriteFont>("Fonts/TitleFont");
            SettingsFont = content.Load<SpriteFont>("Fonts/SettingsFont");
            RetroFont = content.Load<SpriteFont>("Fonts/RetroFont");
            RetroFontLarge = content.Load<SpriteFont>("Fonts/RetroFontLarge");
            RetroFontMedium = content.Load<SpriteFont>("Fonts/RetroFontMedium");
            RetroFontButton = content.Load<SpriteFont>("Fonts/RetroFontButton");
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
