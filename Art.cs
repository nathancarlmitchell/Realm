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
        public static Texture2D CubeGod { get; private set; }
        public static Texture2D CubeOverseer { get; private set; }
        public static Texture2D CubeDefender { get; private set; }
        public static Texture2D CubeBlaster { get; private set; }
        public static Texture2D BlueMagic { get; private set; }
        public static Texture2D OrangeMagic { get; private set; }
        public static Texture2D FireBolt { get; private set; }
        public static Texture2D GreenStar { get; private set; }
        public static Texture2D CyanMagic { get; private set; }
        public static Texture2D YellowMagic { get; private set; }
        public static Texture2D Blade { get; private set; }
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
        public static Texture2D DarknessBolt { get; private set; }
        public static Texture2D MajestyBolt { get; private set; }
        public static Texture2D PurpleBolt { get; private set; }
        public static Texture2D SplendorBolt { get; private set; }
        public static Texture2D PinkBolt { get; private set; }
        public static Texture2D RedBolt { get; private set; }
        public static Texture2D Wizard { get; private set; }
        public static Texture2D Archer { get; private set; }
        public static Texture2D Knight { get; private set; }
        public static Texture2D Priest { get; private set; }
        public static Texture2D Rogue { get; private set; }
        public static Texture2D ArcherProjectile { get; private set; }
        public static Texture2D ShieldProjectile { get; private set; }

        // Pirate Cave's own enemy roster (Data/DungeonType_PirateCave.json)
        // — real, dedicated art supplied for every one of the wiki's named
        // enemies, unlike the tinted-reskin approach used for enemies with
        // no dedicated art (Slime/Brute). Loaded from Content/Dungeons/
        // Pirate Cave/{Name}.png.
        public static Texture2D CavePirateCabinBoy { get; private set; }
        public static Texture2D CavePirateHunchback { get; private set; }
        public static Texture2D CavePirateMacaw { get; private set; }
        public static Texture2D CavePirateMoll { get; private set; }
        public static Texture2D CavePirateMonkey { get; private set; }
        public static Texture2D CavePirateParrot { get; private set; }
        public static Texture2D CavePirateBrawler { get; private set; }
        public static Texture2D CavePirateSailor { get; private set; }
        public static Texture2D CavePirateVeteran { get; private set; }
        public static Texture2D PirateLieutenant { get; private set; }
        public static Texture2D PirateCommander { get; private set; }
        public static Texture2D PirateCaptain { get; private set; }
        public static Texture2D PirateAdmiral { get; private set; }
        public static Texture2D DreadstumpThePirateKing { get; private set; }

        // Pirate Cave's own projectile art (also user-supplied), used in
        // place of the generic Art.EnemyProjectile default wherever a
        // matching shot exists.
        public static Texture2D GoldShot { get; private set; }
        public static Texture2D PirateCannonBullet { get; private set; }
        public static Texture2D PirateKingSword { get; private set; }
        public static Texture2D PirateShot { get; private set; }

        // Loaded from "Priate Sword.png" — a typo in the supplied filename,
        // kept as-is on disk (renaming a user-supplied asset isn't this
        // property's job) but named correctly here in code.
        public static Texture2D PirateSword { get; private set; }

        // Snake Pit's own real enemy roster (Data/DungeonType_SnakePit.json)
        // — real, dedicated art supplied for the 7 regular enemies plus the
        // Treasure Room's own mini-boss, same "one texture per named wiki
        // enemy" treatment as Pirate Cave above. Loaded from Content/
        // Dungeons/Snake Pit/{Name}.png. No dedicated Snakepit Dart Thrower
        // art exists (its own wiki page was merged into the Guard's — see
        // docs/DEVLOG.md) — it reuses Art.HealthBar tinted, same
        // placeholder-art precedent Cube God's "cube system" already set.
        public static Texture2D PitSnake { get; private set; }
        public static Texture2D PitViper { get; private set; }
        public static Texture2D GreaterPitSnake { get; private set; }
        public static Texture2D GreaterPitViper { get; private set; }
        public static Texture2D BrownPython { get; private set; }
        public static Texture2D YellowPython { get; private set; }
        public static Texture2D FirePython { get; private set; }
        public static Texture2D SnakepitGuard { get; private set; }

        // Snake Pit's own basic-shot projectile art (also user-supplied) —
        // used by the 5 regular enemies whose own attack is a plain bite/
        // shot (Pit Snake/Viper, Brown/Yellow/Fire Python) in place of the
        // generic Art.EnemyProjectile default, same role Pirate Cave's own
        // GoldShot/PirateShot/etc. play there. Not used by Greater Pit
        // Snake/Viper (their own attacks are named "Snake Balls"/bombs on
        // the wiki, a different visual, so they keep the generic default)
        // or Snakepit Guard (also Snake Balls/Spinners, not a bite).
        public static Texture2D SnakeBite { get; private set; }

        // Sprite World (realmeye.com/wiki/sprite-world) — real, dedicated
        // art supplied for the full enemy roster (Content/Dungeons/Sprite
        // World/{Name}.png). Craig the Intern has no combat stats on the
        // wiki (a non-hostile NPC cameo) and isn't wired to any enemy class
        // this pass, so his own supplied art is deliberately left unloaded
        // here rather than sitting unused as a dead Art.cs property.
        public static Texture2D NativeDarknessSprite { get; private set; }
        public static Texture2D NativeFireSprite { get; private set; }
        public static Texture2D NativeIceSprite { get; private set; }
        public static Texture2D NativeMagicSprite { get; private set; }
        public static Texture2D NativeNatureSprite { get; private set; }
        public static Texture2D NativeGreaterDarknessSprite { get; private set; }
        public static Texture2D NativeGreaterFireSprite { get; private set; }
        public static Texture2D NativeGreaterIceSprite { get; private set; }
        public static Texture2D NativeGreaterMagicSprite { get; private set; }
        public static Texture2D NativeGreaterNatureSprite { get; private set; }
        public static Texture2D NativeSpriteGodDarkness { get; private set; }
        public static Texture2D NativeSpriteGodFire { get; private set; }
        public static Texture2D NativeSpriteGodIce { get; private set; }
        public static Texture2D NativeSpriteGodMagic { get; private set; }
        public static Texture2D NativeSpriteGodNature { get; private set; }

        // Sprite World's own per-element projectile art (Content/
        // Projectiles/{Color} Sprite {Shape}.png, also user-supplied) — the
        // wiki's own element-to-color mapping (Darkness=black, Fire=orange,
        // Nature=green bolts) plus every remaining color having its own
        // 40x40 boomerang-shaped "Twirl" variant (matching every element's
        // own boomerang attack on Native Sprite God's attack table, not
        // just Magic's) fixes Ice=cyan, Magic=blue. No dedicated "Blue
        // Sprite Magic" (plain 30x15 bolt) was supplied for Magic — only
        // its Twirl — so the regular Native Magic Sprite's own plain shot
        // reuses SpriteMagicTwirl below too, rather than sitting without
        // any real art. Bolt = each element's plain shot (regular Native
        // Sprites); GreaterShape = that element's own distinct shape
        // (Greater Sprites' signature attack); Twirl = every element's own
        // boomerang, used by Native Sprite God specifically (its attack
        // table shows a boomerang variant on every form, not just Magic's).
        public static Texture2D SpriteDarknessBolt { get; private set; }
        public static Texture2D SpriteDarknessGreaterShape { get; private set; } // "Beam"
        public static Texture2D SpriteDarknessTwirl { get; private set; }
        public static Texture2D SpriteFireBolt { get; private set; }
        public static Texture2D SpriteFireGreaterShape { get; private set; } // "Line"
        public static Texture2D SpriteFireTwirl { get; private set; }
        public static Texture2D SpriteIceBolt { get; private set; }
        public static Texture2D SpriteIceTwirl { get; private set; } // also Ice's GreaterShape
        public static Texture2D SpriteNatureBolt { get; private set; }
        public static Texture2D SpriteNatureGreaterShape { get; private set; } // "Bolt"
        public static Texture2D SpriteNatureTwirl { get; private set; }
        public static Texture2D SpriteMagicTwirl { get; private set; } // also Magic's Bolt/GreaterShape

        // Limon the Sprite Goddess's own art (realmeye.com/wiki/sprite-
        // world-guide's "Boss" section) — supplied as a distinct follow-up
        // batch, not shared with the regular Native Sprite roster above.
        //
        // LimonForm{Element}: her phase 2 transform sprites (Content/
        // Dungeons/Sprite World/Limon the Sprite Goddess {Element}.png),
        // swapped in for the fight's duration in that form (see
        // LimonTheSpriteGoddess.TickPendingTransition()). LimonFormFire is
        // pixel-identical to the base Limon texture (her default
        // appearance is already fire-colored) — kept as its own named
        // property for symmetry with the other 4 forms rather than
        // special-cased away, even though swapping to it is a visual
        // no-op.
        public static Texture2D LimonFormMagic { get; private set; }
        public static Texture2D LimonFormIce { get; private set; }
        public static Texture2D LimonFormNature { get; private set; }
        public static Texture2D LimonFormDarkness { get; private set; }
        public static Texture2D LimonFormFire { get; private set; }

        // LimonSignatureBolt (Purple — matches the small pink/magenta
        // dashes on the original placeholder LimonProjectile above,
        // Limon's own established color) replaces that placeholder in
        // every one of her non-elemental attacks (phase 1, phase 3).
        public static Texture2D LimonSignatureBolt { get; private set; }

        // Phase 2's own per-form attacks — one Bolt/Beam/Line pairing per
        // form, matching (but distinct from, larger/boss-tier) the regular
        // Native Sprites' own per-element art above. No dedicated Darkness
        // pair was supplied this batch — that form keeps reusing
        // SpriteDarknessBolt/GreaterShape from the regular roster instead.
        public static Texture2D LimonMagicBolt { get; private set; }
        public static Texture2D LimonMagicBeam { get; private set; }
        public static Texture2D LimonIceBolt { get; private set; }
        public static Texture2D LimonIceBeam { get; private set; }
        public static Texture2D LimonNatureBeam { get; private set; }
        public static Texture2D LimonNatureLine { get; private set; }
        public static Texture2D LimonFireBolt { get; private set; }

        // Phase 3's own distinct "rainbow" flourishes — realmeye.com/wiki/
        // sprite-world-guide's own "ring of fire bolts," "rainbow blast,"
        // and (Darkness form, phase 2) "many rainbow stars." RainbowBlast
        // is a real 4-frame animation (Content/Projectiles/Rainbow Sprite
        // Blast.png, 160x40 = 4x40x40 frames in one row) — the armor-
        // piercing "rainbow blast" is the one shot in this fight dramatic
        // enough to earn a genuinely new AnimatedEnemyProjectile (see that
        // class's own doc comment) rather than a plain static sprite.
        public static Texture2D RainbowLine { get; private set; }
        public static Texture2D RainbowStar { get; private set; }
        public static AnimatedTexture RainbowBlast { get; private set; }

        // "Use of an Electric pet or other abilities to inflict Paralyzed
        // Limon will cause her to fire a radial blast of shots similar to
        // the Staff of Extreme Prejudice" — a real, if easy-to-miss, wiki
        // mechanic (LimonTheSpriteGoddess.ParalyzePunishment()).
        public static Texture2D PrejudicePulse { get; private set; }

        // The Sprite World dungeon's own tile atlas (Data/TileSet_
        // SpriteWorld.json's ImageName) — DungeonState loads it dynamically
        // per-instance via ContentManager instead (it already has a real
        // TileSetData/ContentManager on hand), so this static copy exists
        // only for LimonTheSpriteGoddess.DrawArenaFloor(), which has
        // neither: it needs the same atlas (specifically the 4 directional
        // Conveyor tiles) to paint her boss-arena conveyor zones with real
        // art instead of leaving them invisible.
        public static Texture2D SpriteWorldTileSet { get; private set; }

        public static AnimatedTexture Portal { get; private set; }

        // Dungeon-specific portal animations — same 7-frame, 8fps loop as
        // the generic Portal above, just laid out 5-wide/2-row instead of
        // one long strip (see AnimatedTexture's columns param). Only used
        // for portals leading into a specific boss's dungeon (see
        // Portal.Destination.BossDestination); every other portal keeps
        // the plain swirl above.
        public static AnimatedTexture SpriteWorldPortal { get; private set; }
        public static AnimatedTexture SnakePitPortal { get; private set; }

        // Cube God's own dungeon portal — a single static frame (like
        // NexusPortal/BankPortal/RealmPortal below) rather than an
        // animated strip like SpriteWorldPortal/SnakePitPortal above, since
        // that's the art actually supplied for it.
        public static AnimatedTexture ThirdDimensionPortal { get; private set; }

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

        // Pirate Cave's own entry portal (see Portal.Destination.
        // PirateCaveDungeon) — same single-static-frame treatment as
        // ThirdDimensionPortal above; real art supplied for it (unlike
        // Snake Pit's own dungeon portal, still the generic swirl).
        public static AnimatedTexture PirateCavePortal { get; private set; }

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
        public static Texture2D LethalStrike { get; private set; }
        public static Texture2D Dazed { get; private set; }
        public static Texture2D Bleeding { get; private set; }
        public static Texture2D Speedy { get; private set; }
        public static Texture2D Silenced { get; private set; }
        public static Texture2D GreenBolt { get; private set; }
        public static Texture2D BlackMagic { get; private set; }

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

            ThirdDimensionPortal = new AnimatedTexture(Vector2.Zero, 0f, 1.0f, 0.5f);
            ThirdDimensionPortal.Load(content, "Portal to The Third Dimension", 1, 1);

            NexusPortal = new AnimatedTexture(Vector2.Zero, 0f, 1.0f, 0.5f);
            NexusPortal.Load(content, "Portal to Nexus", 1, 1);

            BankPortal = new AnimatedTexture(Vector2.Zero, 0f, 1.0f, 0.5f);
            BankPortal.Load(content, "Vault Chest", 1, 1);

            RealmPortal = new AnimatedTexture(Vector2.Zero, 0f, 1.0f, 0.5f);
            RealmPortal.Load(content, "Portal to Realm", 1, 1);

            CharacterSelectPortal = new AnimatedTexture(Vector2.Zero, 0f, 1.0f, 0.5f);
            CharacterSelectPortal.Load(content, "Character Changer", 1, 1);

            PirateCavePortal = new AnimatedTexture(Vector2.Zero, 0f, 1.0f, 0.5f);
            PirateCavePortal.Load(content, "Dungeons/Pirate Cave/Portal", 1, 1);

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
            Rogue = content.Load<Texture2D>("Classes/Rogue");

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
            CubeGod = content.Load<Texture2D>("Enemies/Cube God/Cube God");
            CubeOverseer = content.Load<Texture2D>("Enemies/Cube God/Cube Overseer");
            CubeDefender = content.Load<Texture2D>("Enemies/Cube God/Cube Defender");
            CubeBlaster = content.Load<Texture2D>("Enemies/Cube God/Cube Blaster");
            BlueMagic = content.Load<Texture2D>("Projectiles/blue_magic");
            OrangeMagic = content.Load<Texture2D>("Projectiles/Orange Magic");
            FireBolt = content.Load<Texture2D>("Projectiles/Fire Bolt");
            GreenStar = content.Load<Texture2D>("Projectiles/Green Star");
            CyanMagic = content.Load<Texture2D>("Projectiles/Cyan Magic");
            YellowMagic = content.Load<Texture2D>("Projectiles/Yellow Magic");
            Blade = content.Load<Texture2D>("Projectiles/Blade");
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
            DarknessBolt = content.Load<Texture2D>("Projectiles/Darkness Bolt");
            MajestyBolt = content.Load<Texture2D>("Projectiles/Majesty Bolt");
            PurpleBolt = content.Load<Texture2D>("Projectiles/Purple Bolt");
            SplendorBolt = content.Load<Texture2D>("Projectiles/Splendor Bolt");
            PinkBolt = content.Load<Texture2D>("Projectiles/pink_bolt");
            RedBolt = content.Load<Texture2D>("Projectiles/Red Bolt");

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
            LethalStrike = content.Load<Texture2D>("StatusEffects/leathal strike");
            Dazed = content.Load<Texture2D>("StatusEffects/dazed");
            Bleeding = content.Load<Texture2D>("StatusEffects/bleeding");
            Speedy = content.Load<Texture2D>("StatusEffects/speedy");
            Silenced = content.Load<Texture2D>("StatusEffects/silenced");
            GreenBolt = content.Load<Texture2D>("Projectiles/Green Bolt");
            BlackMagic = content.Load<Texture2D>("Projectiles/Black Magic");

            // Pirate Cave (Data/DungeonType_PirateCave.json) — every enemy's
            // own real art, supplied directly rather than a tinted reskin.
            CavePirateCabinBoy = content.Load<Texture2D>("Dungeons/Pirate Cave/Cave Pirate Cabin Boy");
            CavePirateHunchback = content.Load<Texture2D>("Dungeons/Pirate Cave/Cave Pirate Hunchback");
            CavePirateMacaw = content.Load<Texture2D>("Dungeons/Pirate Cave/Cave Pirate Macaw");
            CavePirateMoll = content.Load<Texture2D>("Dungeons/Pirate Cave/Cave Pirate Moll");
            CavePirateMonkey = content.Load<Texture2D>("Dungeons/Pirate Cave/Cave Pirate Monkey");
            CavePirateParrot = content.Load<Texture2D>("Dungeons/Pirate Cave/Cave Pirate Parrot");
            CavePirateBrawler = content.Load<Texture2D>("Dungeons/Pirate Cave/Cave Pirate Brawler");
            CavePirateSailor = content.Load<Texture2D>("Dungeons/Pirate Cave/Cave Pirate Sailor");
            CavePirateVeteran = content.Load<Texture2D>("Dungeons/Pirate Cave/Cave Pirate Veteran");
            PirateLieutenant = content.Load<Texture2D>("Dungeons/Pirate Cave/Pirate Lieutenant");
            PirateCommander = content.Load<Texture2D>("Dungeons/Pirate Cave/Pirate Commander");
            PirateCaptain = content.Load<Texture2D>("Dungeons/Pirate Cave/Pirate Captain");
            PirateAdmiral = content.Load<Texture2D>("Dungeons/Pirate Cave/Pirate Admiral");
            DreadstumpThePirateKing = content.Load<Texture2D>(
                "Dungeons/Pirate Cave/Dreadstump the Pirate King"
            );
            GoldShot = content.Load<Texture2D>("Projectiles/Gold Shot");
            PirateCannonBullet = content.Load<Texture2D>("Projectiles/Pirate Cannon Bullet");
            PirateKingSword = content.Load<Texture2D>("Projectiles/Pirate King Sword");
            PirateShot = content.Load<Texture2D>("Projectiles/Pirate Shot");
            PirateSword = content.Load<Texture2D>("Projectiles/Priate Sword");

            PitSnake = content.Load<Texture2D>("Dungeons/Snake Pit/Pit Snake");
            PitViper = content.Load<Texture2D>("Dungeons/Snake Pit/Pit Viper");
            GreaterPitSnake = content.Load<Texture2D>("Dungeons/Snake Pit/Greater Pit Snake");
            GreaterPitViper = content.Load<Texture2D>("Dungeons/Snake Pit/Greater Pit Viper");
            BrownPython = content.Load<Texture2D>("Dungeons/Snake Pit/Brown Python");
            YellowPython = content.Load<Texture2D>("Dungeons/Snake Pit/Yellow Python");
            FirePython = content.Load<Texture2D>("Dungeons/Snake Pit/Fire Python");
            SnakepitGuard = content.Load<Texture2D>("Dungeons/Snake Pit/Snakepit Guard");
            SnakeBite = content.Load<Texture2D>("Projectiles/Snake Bite");

            // Sprite World (realmeye.com/wiki/sprite-world).
            NativeDarknessSprite = content.Load<Texture2D>(
                "Dungeons/Sprite World/Native Darkness Sprite"
            );
            NativeFireSprite = content.Load<Texture2D>("Dungeons/Sprite World/Native Fire Sprite");
            NativeIceSprite = content.Load<Texture2D>("Dungeons/Sprite World/Native Ice Sprite");
            NativeMagicSprite = content.Load<Texture2D>("Dungeons/Sprite World/Native Magic Sprite");
            NativeNatureSprite = content.Load<Texture2D>(
                "Dungeons/Sprite World/Native Nature Sprite"
            );
            NativeGreaterDarknessSprite = content.Load<Texture2D>(
                "Dungeons/Sprite World/Native Greater Darkness Sprite"
            );
            NativeGreaterFireSprite = content.Load<Texture2D>(
                "Dungeons/Sprite World/Native Greater Fire Sprite"
            );
            NativeGreaterIceSprite = content.Load<Texture2D>(
                "Dungeons/Sprite World/Native Greater Ice Sprite"
            );
            NativeGreaterMagicSprite = content.Load<Texture2D>(
                "Dungeons/Sprite World/Native Greater Magic Sprite"
            );
            NativeGreaterNatureSprite = content.Load<Texture2D>(
                "Dungeons/Sprite World/Native Greater Nature Sprite"
            );
            NativeSpriteGodDarkness = content.Load<Texture2D>(
                "Dungeons/Sprite World/Native Sprite God Darkness"
            );
            NativeSpriteGodFire = content.Load<Texture2D>(
                "Dungeons/Sprite World/Native Sprite God Fire"
            );
            NativeSpriteGodIce = content.Load<Texture2D>("Dungeons/Sprite World/Native Sprite God Ice");
            NativeSpriteGodMagic = content.Load<Texture2D>(
                "Dungeons/Sprite World/Native Sprite God Magic"
            );
            NativeSpriteGodNature = content.Load<Texture2D>(
                "Dungeons/Sprite World/Native Sprite God Nature"
            );

            SpriteDarknessBolt = content.Load<Texture2D>("Projectiles/Black Sprite Magic");
            SpriteDarknessGreaterShape = content.Load<Texture2D>("Projectiles/Black Sprite Beam");
            SpriteDarknessTwirl = content.Load<Texture2D>("Projectiles/Black Sprite Twirl");
            SpriteFireBolt = content.Load<Texture2D>("Projectiles/Orange Sprite Magic");
            SpriteFireGreaterShape = content.Load<Texture2D>("Projectiles/Orange Sprite Line");
            SpriteFireTwirl = content.Load<Texture2D>("Projectiles/Orange Sprite Twirl");
            SpriteIceBolt = content.Load<Texture2D>("Projectiles/Cyan Sprite Magic");
            SpriteIceTwirl = content.Load<Texture2D>("Projectiles/Cyan Sprite Twirl");
            SpriteNatureBolt = content.Load<Texture2D>("Projectiles/Green Sprite Magic");
            SpriteNatureGreaterShape = content.Load<Texture2D>("Projectiles/Green Sprite Bolt");
            SpriteNatureTwirl = content.Load<Texture2D>("Projectiles/Green Sprite Twirl");
            SpriteMagicTwirl = content.Load<Texture2D>("Projectiles/Blue Sprite Twirl");

            LimonFormMagic = content.Load<Texture2D>("Dungeons/Sprite World/Limon the Sprite Goddess Magic");
            LimonFormIce = content.Load<Texture2D>("Dungeons/Sprite World/Limon the Sprite Goddess Ice");
            LimonFormNature = content.Load<Texture2D>(
                "Dungeons/Sprite World/Limon the Sprite Goddess Nature"
            );
            LimonFormDarkness = content.Load<Texture2D>(
                "Dungeons/Sprite World/Limon the Sprite Goddess Darkness"
            );
            LimonFormFire = content.Load<Texture2D>("Dungeons/Sprite World/Limon the Sprite Goddess Fire");
            LimonSignatureBolt = content.Load<Texture2D>("Projectiles/Purple Sprite Bolt");
            LimonMagicBolt = content.Load<Texture2D>("Projectiles/Blue Sprite Bolt");
            LimonMagicBeam = content.Load<Texture2D>("Projectiles/Blue Sprite Beam");
            LimonIceBolt = content.Load<Texture2D>("Projectiles/Cyan Sprite Bolt");
            LimonIceBeam = content.Load<Texture2D>("Projectiles/Cyan Sprite Beam");
            LimonNatureBeam = content.Load<Texture2D>("Projectiles/Green Sprite Beam");
            LimonNatureLine = content.Load<Texture2D>("Projectiles/Green Sprite Line");
            LimonFireBolt = content.Load<Texture2D>("Projectiles/Orange Sprite Bolt");
            RainbowLine = content.Load<Texture2D>("Projectiles/Rainbow Sprite Line");
            RainbowStar = content.Load<Texture2D>("Projectiles/Rainbow Sprite Star");
            PrejudicePulse = content.Load<Texture2D>("Projectiles/Prejudice Pulse");
            SpriteWorldTileSet = content.Load<Texture2D>("Dungeons/Sprite World/TileSet");

            // 4 frames, one row (160x40 = 4x40x40) — a quick, one-shot
            // blast rather than a looping idle animation, matching how it's
            // actually used (LimonTheSpriteGoddess's own armor-piercing
            // shot, alive for well under a second).
            RainbowBlast = new AnimatedTexture(Vector2.Zero, 0f, 1.0f, 0.5f);
            RainbowBlast.Load(content, "Projectiles/Rainbow Sprite Blast", 4, 12, 4, loop: false);
            RainbowBlast.Origin = new Vector2(
                RainbowBlast.FrameWidth / 2f,
                RainbowBlast.FrameHeight / 2f
            );

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
