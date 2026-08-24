using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Realm.States;

namespace Realm
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager Graphics;
        private SpriteBatch _spriteBatch;

        private State nextState,
            currentState;

        public static Camera Camera;

        // The window stays the original 1280x720 — the sidebar carves its
        // width out of that instead of adding to it, so the gameplay/camera
        // area is narrower (WindowWidth - SidebarWidth) than it used to be.
        // Everything that renders or reasons about the visible game world
        // (Camera, on-screen checks, overlays drawn on top of gameplay
        // content like the loot bag popup) should use GameplayViewportWidth/
        // Height, not ScreenWidth/Viewport.Width — otherwise it ignores the
        // sidebar and draws underneath it.
        public const int WindowWidth = 1280;
        public const int WindowHeight = 720;
        public const int SidebarWidth = 300;
        public const int GameplayViewportWidth = WindowWidth - SidebarWidth;
        public const int GameplayViewportHeight = WindowHeight;

        // Left edge of the HUD sidebar, in screen space.
        public static int SidebarX => GameplayViewportWidth;

        // Helpful static properties.
        public static Game1 Instance { get; private set; }
        public static Viewport Viewport
        {
            get { return Instance.GraphicsDevice.Viewport; }
        }
        public static Vector2 ScreenSize
        {
            get { return new Vector2(Viewport.Width, Viewport.Height); }
        }
        public static int ScreenWidth
        {
            get { return (int)ScreenSize.X; }
        }
        public static int ScreenHeight
        {
            get { return (int)ScreenSize.Y; }
        }
        public static int CenterWidth
        {
            get { return (int)(ScreenSize.X / 2); }
        }
        public static int CenterHeight
        {
            get { return (int)(ScreenSize.Y / 2); }
        }
        public static int Scale { get; set; }
        public static int WorldWidth { get; set; }
        public static int WorldHeight { get; set; }
        public static Vector2 WorldSize
        {
            get { return new Vector2(WorldWidth, WorldHeight); }
        }
        public static GameTime GameTime;

        public static Rectangle WorldBounds
        {
            get { return GetWorldBounds(1f); }
        }

        // Same box as WorldBounds, scaled around the camera. Pass > 1 to
        // include a margin beyond the visible screen (e.g. for enemy
        // "on screen" attack checks that should trigger slightly early).
        // Uses the fixed gameplay viewport, not the (wider) window, so
        // "on screen" still means the actual visible play area.
        public static Rectangle GetWorldBounds(float scale)
        {
            int halfWidth = (int)((GameplayViewportWidth / 2) * scale);
            int halfHeight = (int)((GameplayViewportHeight / 2) * scale);
            return new Rectangle(
                (int)Camera.Pos.X - halfWidth,
                (int)Camera.Pos.Y - halfHeight,
                2 * halfWidth,
                2 * halfHeight
            );
        }

        public static bool Mute { get; set; }
        public static bool _Debug { get; set; }
        public List<Weapon> Weapons { get; set; }
        public List<Armor> Armors { get; set; }
        public List<Ring> Rings { get; set; }
        public List<Spell> Spells { get; set; }
        public List<Quiver> Quivers { get; set; }
        public List<Shield> Shields { get; set; }

        public Game1()
        {
            Instance = this;
            Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            IsMouseVisible = true;
            _Debug = false;
            Mute = false;
            Window.Title = "Realm";
            Scale = 1;

            Graphics.IsFullScreen = false;

            Graphics.PreferredBackBufferWidth = WindowWidth;
            Graphics.PreferredBackBufferHeight = WindowHeight;

            WorldWidth = 500000;
            WorldHeight = 500000;

            Graphics.ApplyChanges();
            Debug.WriteLine(
                "Screen Size: "
                    + GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width
                    + " x "
                    + GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height
            );

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            Art.Load(Content);
            Sound.Load(Content);

            StartGame();
        }

        private void StartGame()
        {
            currentState = new MenuState(this, Graphics.GraphicsDevice, Content);

            Weapons = Util.LoadWeaponData();
            Weapons.AddRange(Util.LoadBowData());
            Armors = Util.LoadArmorData();
            Rings = Util.LoadRingData();
            Spells = Util.LoadSpellData();
            Quivers = Util.LoadQuiverData();
            Shields = Util.LoadShieldData();

            Util.LoadBankData();
            Util.LoadFameData();
            Util.LoadKeyBindingsData();

            Util.LoadOrCreatePlayer(Util.DetermineLastPlayedClass());

            // Must run after LoadOrCreatePlayer(), not alongside the other
            // Load*Data() calls above — ResetPlayer() (called from inside
            // LoadOrCreatePlayer()) constructs a brand new Player.Instance
            // (Wizard/Archer/Knight), which would silently discard whatever
            // this set if it ran first.
            Util.LoadGameSettingsData();

            EntityManager.Add(Player.Instance);
        }

        public void ChangeState(State state)
        {
            // Loot bags are ephemeral (never saved/loaded — see Util.cs) and
            // meaningless outside the state they were dropped in, so they
            // shouldn't survive any state change. Cleared here rather than
            // per-state-constructor since every transition in the game
            // (see StateManager.cs) funnels through this one method — a
            // state that doesn't already clear ItemSpawner.LootBags itself
            // (only RealmState's constructor currently does) would
            // otherwise leave stale, uninteractable bags rendering forever
            // via the next state's own DrawLoot() loop.
            ItemSpawner.Reset();

            // Same reasoning as ItemSpawner.Reset() above — a portal
            // awaiting confirmation belongs to the state being left, and
            // every transition funnels through here regardless of how it
            // was triggered (walking through a portal, Escape, a key bind,
            // dying), so this is the one place that reliably catches all of
            // them instead of relying on each path to remember to clear it.
            Portal.ClearPendingConfirmation();

            nextState = state;
        }

        protected override void Update(GameTime gameTime)
        {
            if (nextState != null)
            {
                currentState = nextState;
                nextState = null;
            }

            currentState.Update(gameTime);
            currentState.PostUpdate(gameTime);

            base.Update(gameTime);

            // Handles user input.
            Input.Update(currentState);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            currentState.Draw(gameTime, _spriteBatch);

            base.Draw(gameTime);
        }
    }
}
