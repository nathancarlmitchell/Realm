using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Realm.States
{
    public class GameState : State
    {
        public static Player Player { get; set; }

        //public static Camera_old Camera { get; set; }
        Rectangle targetRectangle;

        public GameState(Game1 game, GraphicsDevice graphicsDevice, ContentManager content)
            : base()
        {
            EntityManager.Add(Player.Instance);
            EntityManager.Add(new Item());

            // Define a drawing rectangle based on the number of tiles wide and high, using the texture dimensions.
            targetRectangle = new Rectangle(0, 0, Game1.WorldWidth, Game1.WorldHeight);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(
                SpriteSortMode.FrontToBack,
                BlendState.AlphaBlend,
                SamplerState.LinearWrap,
                DepthStencilState.Default,
                RasterizerState.CullNone,
                null,
                Game1.Camera.GetTransformation()
            );

            // Draw background.
            //Background.Draw(spriteBatch);
            spriteBatch.Draw(Art.Tile, new Vector2(32, 32), targetRectangle, Color.White);

            spriteBatch.End();

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                null,
                null,
                null,
                null,
                null,
                Game1.Camera.GetTransformation()
            );

            // Draw player.
            //Player.Draw(spriteBatch);

            // Draw projectiles.
            EntityManager.Draw(spriteBatch);

            spriteBatch.End();

            spriteBatch.Begin();

            // Draw health and mana.
            Overlay.DrawHealth(spriteBatch);

            // Draw stats.
            Overlay.DrawStats(spriteBatch);

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
            EntityManager.Update();
            EnemySpawner.Update();
        }
    }
}
