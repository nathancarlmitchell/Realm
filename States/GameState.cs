using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Realm.States
{
    public class GameState : State
    {
        public static Player Player { get; set; }
        public static Camera Camera { get; set; }

        public GameState(Game1 game, GraphicsDevice graphicsDevice, ContentManager content)
            : base()
        {
            //Camera = new Camera(Game1.Viewport);

            Player = new Player()
            {
                //Position.X = Game1.ScreenWidth / 2,
                //Y = Game1.ScreenHeight / 2,
                //Height = 64,
                //Width = 64,
                Texture = Art.Player,
            };
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                null,
                null,
                null,
                null,
                null,
                Game1.Camera.GetTransformation()
            );

            // Draw background.
            Background.Draw(spriteBatch);

            // Draw player.
            Player.Draw(spriteBatch);

            // Draw projectiles.
            EntityManager.Draw(spriteBatch);

            if (Game1._Debug)
            {
                Overlay.DrawDebug(spriteBatch);
            }

            spriteBatch.End();
        }

        public override void PostUpdate(GameTime gameTime)
        {
            //throw new NotImplementedException();
        }

        public override void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Player.Update(elapsed, gameTime);
            Player.Update();
            EntityManager.Update();
            EnemySpawner.Update();

            //Camera.UpdateCamera(Game1.Viewport);
        }
    }
}
