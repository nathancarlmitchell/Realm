using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Realm.States
{
    public class RealmState : State
    {
        Rectangle targetRectangle;

        public static Guid HealthPotionGuid = Guid.NewGuid();
        public static Guid ManaPotionGuid = Guid.NewGuid();
        public static int HighScore { get; set; }
        public static int Score { get; set; }
        private List<Portal> portalList;

        public RealmState(Game1 game, GraphicsDevice graphicsDevice, ContentManager content)
            : base()
        {
            Debug.WriteLine("New RealmState created.");
            Game1.Camera = new Camera(Game1.Viewport, Game1.WorldWidth, Game1.WorldHeight, 1f);

            EntityManager.Reset();

            var portalPos = new Vector2(
                Player.Instance.Position.X - 25,
                Player.Instance.Position.Y - 100
            );

            portalList = [new Portal(), new Portal(portalPos, Portal.Destination.CharacterSelect)];

            // Define a drawing rectangle based on the number of tiles wide and high, using the texture dimensions.
            targetRectangle = new Rectangle(0, 0, Game1.WorldWidth, Game1.WorldHeight);
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

            // Draw portal.
            foreach (Portal portal in portalList)
            {
                portal.Draw(spriteBatch, gameTime);
            }

            // Draw player.
            EntityManager.Draw(spriteBatch);

            spriteBatch.End();

            spriteBatch.Begin();

            // Draw health and mana.
            Overlay.DrawHealth(spriteBatch);

            // Draw stats.
            Overlay.DrawStats(spriteBatch);

            // Draw inventory.
            //Overlay.DrawInventory(spriteBatch);

            // Draw score.
            Overlay.DrawScore(spriteBatch);

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

            foreach (Portal portal in portalList)
            {
                portal.Update(gameTime);
            }
        }
    }
}
