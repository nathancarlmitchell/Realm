using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

        public static void DrawStats(SpriteBatch spriteBatch)
        {
            int x = (int)Game1.Camera.Pos.X + (Game1.ScreenWidth / 2) - 512;
            int y = (int)Game1.Camera.Pos.Y + Game1.Viewport.Height / 2 - 128;
            Vector2 pos = new Vector2(x, y);
            Color color = Color.Red;

            spriteBatch.DrawString(
                Art.HudFont,
                "Level: " + Player.Level,
                new Vector2(x, y + 0),
                color
            );
            spriteBatch.DrawString(
                Art.HudFont,
                "Experience: " + Player.Experience,
                new Vector2(x, y + 16),
                color
            );
            spriteBatch.DrawString(
                Art.HudFont,
                "ExperienceTotal: " + Player.ExperienceTotal,
                new Vector2(x + 128, y + 16),
                color
            );
            spriteBatch.DrawString(
                Art.HudFont,
                "Attack: " + Player.Attack,
                new Vector2(x, y + 32),
                color
            );
            spriteBatch.DrawString(
                Art.HudFont,
                "Defence: " + Player.Defense,
                new Vector2(x, y + 48),
                color
            );
            spriteBatch.DrawString(
                Art.HudFont,
                "Vitality: " + Player.Vitality,
                new Vector2(x, y + 64),
                color
            );
            spriteBatch.DrawString(
                Art.HudFont,
                "Wisdom: " + Player.Wisdom,
                new Vector2(x, y + 80),
                color
            );
            spriteBatch.DrawString(
                Art.HudFont,
                "Speed: " + Player.Speed,
                new Vector2(x, y + 96),
                color
            );
            spriteBatch.DrawString(
                Art.HudFont,
                "Dexterity: " + Player.Dexterity,
                new Vector2(x, y + 112),
                color
            );
        }

        public static void DrawHealth(SpriteBatch spriteBatch)
        {
            int x = (int)Game1.Camera.Pos.X - Game1.Viewport.Width / 2 + 64;
            int y = (int)Game1.Camera.Pos.Y + Game1.Viewport.Height / 2 - 128;

            int barScale = 4;
            int barHeight = 40;
            int barOffset = 4;

            Vector2 healthBarPos = new(x, y);
            Vector2 manaBarPos = new(x, y + barHeight + barOffset);

            // Health bars.
            Rectangle greenRect = new(0, 0, Player.Health * barScale, barHeight);
            Rectangle redRect = new(0, 0, Player.HealthMax * barScale, barHeight);

            // Mana bars.
            Rectangle blueRect = new(0, 0, Player.Mana * barScale, barHeight);
            Rectangle blackRect = new(0, 0, Player.ManaMax * barScale, barHeight);

            // Red bar.
            spriteBatch.Draw(
                Art.HealthBar,
                healthBarPos,
                redRect,
                Color.DarkRed,
                0f,
                Vector2.Zero,
                1f,
                0,
                0
            );

            // Green bar.
            spriteBatch.Draw(
                Art.HealthBar,
                healthBarPos,
                greenRect,
                Color.DarkGreen,
                0f,
                Vector2.Zero,
                1f,
                0,
                0
            );

            // Black bar.
            spriteBatch.Draw(
                Art.HealthBar,
                manaBarPos,
                blackRect,
                Color.Black,
                0f,
                Vector2.Zero,
                1f,
                0,
                0
            );

            // Blue bar.
            spriteBatch.Draw(
                Art.HealthBar,
                manaBarPos,
                blueRect,
                Color.DarkBlue,
                0f,
                Vector2.Zero,
                1f,
                0,
                0
            );
        }

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

            float x = Game1.Camera.Pos.X - Game1.Viewport.Width / 2 + 64;
            float y = Game1.Camera.Pos.Y + Game1.Viewport.Height / 2 - 64;
            Vector2 pos = new Vector2(x, y);

            spriteBatch.DrawString(
                Art.HudFont,
                "EntityManager.Count: " + EntityManager.Count,
                pos,
                Color.White
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
