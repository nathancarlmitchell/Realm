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

        public static Camera2D Camera;

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
        public static int Width { get; set; }
        public static bool Mute { get; set; }
        public static bool _Debug { get; set; }

        public Game1()
        {
            Instance = this;
            Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _Debug = true;
        }

        protected override void Initialize()
        {
            IsMouseVisible = true;
            Mute = false;
            Window.Title = "Realm";
            Scale = 1;
            Width = ScreenWidth;

            Graphics.IsFullScreen = false;

            Graphics.PreferredBackBufferWidth = 1280;
            Graphics.PreferredBackBufferHeight = 720;

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
            Camera = new Camera2D(Viewport, 2000, 2000, 1f);

            Art.Load(Content);
            Sound.Load(Content);

            StartGame();
            // TODO: use this.Content to load your game content here
        }

        private void StartGame()
        {
            new GameState(this, Graphics.GraphicsDevice, Content);
            currentState = new MenuState(this, Graphics.GraphicsDevice, Content);
        }

        public void ChangeState(State state)
        {
            nextState = state;
        }

        private KeyboardState keyboard;

        protected override void Update(GameTime gameTime)
        {
            // Move the camera when the arrow keys are pressed
            //Vector2 movement = Vector2.Zero;
            //Viewport vp = Viewport;
            //keyboard = Keyboard.GetState();

            //if (keyboard.IsKeyDown(Keys.A))
            //    movement.X--;
            //if (keyboard.IsKeyDown(Keys.D))
            //    movement.X++;
            //if (keyboard.IsKeyDown(Keys.W))
            //    movement.Y--;
            //if (keyboard.IsKeyDown(Keys.S))
            //    movement.Y++;

            //Camera.Pos += movement * Player.Speed;

            if (nextState != null)
            {
                if (nextState is GameState && GameState != null)
                {
                    currentState = GameState;
                    Debug.WriteLine("restoring GameState");
                }
                else if (nextState is MenuState && MenuState != null)
                {
                    currentState = MenuState;
                    Debug.WriteLine("restoring MenuState");
                }
                else if (nextState is MenuState)
                {
                    MenuState = nextState;
                    currentState = nextState;
                    Debug.WriteLine("nextState is MenuState");
                }
                else
                {
                    Debug.WriteLine("Next State.");
                    currentState = nextState;
                }

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
            GraphicsDevice.Clear(Color.WhiteSmoke);

            currentState.Draw(gameTime, _spriteBatch);

            base.Draw(gameTime);
        }
    }
}
