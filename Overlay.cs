using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm;
using Realm.States;
using Realm.States;

namespace Realm
{
    public static class Overlay
    {
        //private static readonly SpriteBatch spriteBatch = Game1.Instance.SpriteBatch;

        public static void DrawTitle(SpriteBatch spriteBatch)
        {
            // Draw title.
            SpriteFont font = Art.TitleFont;
            string text = "Realm";

            int x = (int)((Game1.ScreenWidth / 2) - (font.MeasureString(text).X / 2));
            int y = 128 / Game1.Scale;

            // Draw text shadow.
            spriteBatch.DrawString(
                font,
                text,
                new Vector2(x - 4, y + 4),
                Color.DarkOrange * (1.0f - 0.25f)
            );

            // Draw text.
            spriteBatch.DrawString(font, text, new Vector2(x, y), Color.DarkMagenta);
        }

        //public static void DrawHUD(bool drawScore = true)
        //{
        //    // Draw Score.
        //    if (drawScore)
        //    {
        //        var color = Color.Black;
        //        if (GameState.Score >= GameState.HighScore)
        //        {
        //            color = Color.Yellow;
        //        }
        //        spriteBatch.DrawString(
        //            Art.HudFont,
        //            "Score: " + GameState.Score,
        //            new Vector2(32, 64),
        //            color
        //        );
        //        spriteBatch.DrawString(
        //            Art.HudFont,
        //            "Hi Score: " + GameState.HighScore,
        //            new Vector2(32, 92),
        //            color
        //        );
        //    }

        //    // Draw coins.
        //    spriteBatch.DrawString(
        //        Art.HudFont,
        //        " x " + GameState.Coins,
        //        new Vector2(GameState.CoinHUD.X + 16, GameState.CoinHUD.Y - 8),
        //        Color.Black
        //    );
        //    GameState.CoinHUD.CoinTexture.DrawFrame(
        //        spriteBatch,
        //        new Vector2(GameState.CoinHUD.X, GameState.CoinHUD.Y)
        //    );
        //}

        //public static void DrawMobileHUD()
        //{
        //    // Draw boost button on mobile.
        //    if (OperatingSystem.IsAndroid())
        //    {
        //        spriteBatch.Draw(
        //            Art.BoostTexture,
        //            new Vector2(0, Game1.ScreenHeight - (Art.BoostTexture.Height * 2)),
        //            Color.White * 0.5f
        //        );
        //        spriteBatch.Draw(
        //            Art.BoostTexture,
        //            new Vector2(0, Game1.ScreenHeight - (Art.BoostTexture.Height)),
        //            Color.White * 0.5f
        //        );
        //    }
        //}

        public static void DrawDebug(SpriteBatch spriteBatch)
        {
            //spriteBatch.DrawString(
            //    Art.DebugFont,
            //    "Coin Length: " + GameState.coinArray.Count,
            //    new Vector2(64, Game1.ScreenHeight - 96),
            //    Color.Black
            //);
            //spriteBatch.DrawString(
            //    Art.DebugFont,
            //    "Array Length: " + wallArray.Count,
            //    new Vector2(64, Game1.ScreenHeight - 80),
            //    Color.Black
            //);

            spriteBatch.DrawString(
                Art.HudFont,
                "Projectiles: " + EntityManager.Count,
                new Vector2(
                    Game1.Camera.Pos.X - Game1.Viewport.Width / 2 + 64,
                    Game1.Camera.Pos.Y + Game1.Viewport.Height / 2 - 64
                ),
                Color.Black
            );
        }

        //private static float muteCooldown = 1.0f;

        //public static void ToggleAudio()
        //{
        //    muteCooldown = 1.0f;
        //}

        //public static void DrawAudio()
        //{
        //    if (muteCooldown > 0.0f)
        //    {
        //        muteCooldown -= 0.005f;
        //        spriteBatch.Begin();
        //        if (Game1.Mute)
        //        {
        //            spriteBatch.Draw(
        //                Art.MuteTexture,
        //                new Vector2(0, Game1.ScreenHeight - (Art.MuteTexture.Height)),
        //                Color.White * muteCooldown
        //            );
        //        }
        //        else
        //        {
        //            spriteBatch.Draw(
        //                Art.UnmuteTexture,
        //                new Vector2(0, Game1.ScreenHeight - (Art.UnmuteTexture.Height)),
        //                Color.White * muteCooldown
        //            );
        //        }
        //        spriteBatch.End();
        //    }
        //}
    }
}
