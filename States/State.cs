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

        // Whether a circle of the given radius at worldPosition sits on
        // walkable ground in this state. True by default — the open Realm
        // and the Nexus have no walls or hard edges within reach. Overridden
        // by DungeonState (tile grid) and BossRealmState (bounded arena).
        // First use: Rogue.UseAbility()'s Cloak of the Planewalker teleport,
        // which refuses to drop the player onto a wall or past the map edge.
        public virtual bool IsWalkable(Vector2 worldPosition, float radius) => true;

        #endregion
    }
}
