using System;
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

        public static State GameState,
            MenuState;

        public static Camera Camera;

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

        public static Rectangle WorldBounds
        {
            get
            {
                return new Rectangle(
                    (int)Camera.Pos.X - CenterWidth,
                    (int)Camera.Pos.Y - CenterHeight,
                    (int)Camera.Pos.X + CenterWidth,
                    (int)Camera.Pos.Y + CenterHeight
                );
            }
        }
        public static bool Mute { get; set; }
        public static bool _Debug { get; set; }

        public Game1()
        {
            Instance = this;
            Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            IsMouseVisible = true;
            _Debug = true;
            Mute = true;
            Window.Title = "Realm";
            Scale = 1;

            Graphics.IsFullScreen = false;

            Graphics.PreferredBackBufferWidth = 1280;
            Graphics.PreferredBackBufferHeight = 720;

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
            //GameState = new GameState(this, Graphics.GraphicsDevice, Content);
            currentState = new MenuState(this, Graphics.GraphicsDevice, Content);
            EntityManager.Add(new Player());
        }

        public void ChangeState(State state)
        {
            nextState = state;
        }

        protected override void Update(GameTime gameTime)
        {
            if (nextState != null)
            {
                //if (nextState is GameState && GameState != null)
                //{
                //    currentState = GameState;
                //    Debug.WriteLine("restoring GameState");
                //}
                //else if (nextState is MenuState && MenuState != null)
                //{
                //    currentState = MenuState;
                //    Debug.WriteLine("restoring MenuState");
                //}
                //else if (nextState is MenuState)
                //{
                //    MenuState = nextState;
                //    currentState = nextState;
                //    Debug.WriteLine("nextState is MenuState");
                //}
                //else
                //{
                //    Debug.WriteLine("Next State.");
                //    currentState = nextState;
                //}

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
