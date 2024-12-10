using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Realm.States
{
    public abstract class State
    {
        #region Fields
        protected ContentManager content;
        protected GraphicsDevice graphicsDevice;
        protected Game1 game;

        private static int centerWidth;
        private static int centerHeight;
        private static int screenWidth;
        private static int screenHeight;
        private static int controlWidthCenter;

        #endregion

        #region Methods
        public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);

        public abstract void PostUpdate(GameTime gameTime);

        public State()
        {
            game = Game1.Instance;
            graphicsDevice = Game1.Instance.GraphicsDevice;
            content = Game1.Instance.Content;

            centerWidth = Game1.CenterWidth;
            centerHeight = Game1.CenterHeight;

            screenWidth = Game1.ScreenWidth;
            screenHeight = Game1.ScreenHeight;

            controlWidthCenter = centerWidth - ((Art.ButtonTexture.Width / 2) * Game1.Scale);
        }

        public static int ScreenHeight
        {
            get { return screenHeight; }
        }

        public static int ScreenWidth
        {
            get { return screenWidth; }
        }

        public static int CenterHeight
        {
            get { return centerHeight; }
        }

        public static int CenterWidth
        {
            get { return centerWidth; }
        }
        public static int ControlWidthCenter
        {
            get { return centerWidth; }
        }

        public abstract void Update(GameTime gameTime);

        #endregion
    }
}
