using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm.States;

namespace Realm
{
    public static class Overlay
    {
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

        public static void DrawScore(SpriteBatch spriteBatch)
        {
            // Draw Score.
            var color = Color.Black;
            if (Player.ExperienceTotal >= GameState.HighScore)
            {
                color = Color.Yellow;
            }
            spriteBatch.DrawString(
                Art.HudFont,
                "Score: " + Player.ExperienceTotal,
                new Vector2(32, 64),
                color
            );
            spriteBatch.DrawString(
                Art.HudFont,
                "Hi Score: " + GameState.HighScore,
                new Vector2(32, 92),
                color
            );
        }

        public static void DrawStats(SpriteBatch spriteBatch)
        {
            int x = Game1.ScreenWidth - 420;
            int y = 64;
            //int x = (int)Game1.Camera.Pos.X + (Game1.ScreenWidth / 2) - 512;
            //int y = (int)Game1.Camera.Pos.Y + Game1.Viewport.Height / 2 - 128;
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
                "ExperienceNextLevel: " + Player.ExperienceNextLevel,
                new Vector2(x + 128, y),
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
            int x = 32;
            int y = Game1.Viewport.Height - 128;
            //int x = (int)Game1.Camera.Pos.X - Game1.Viewport.Width / 2 + 64;
            //int y = (int)Game1.Camera.Pos.Y + Game1.Viewport.Height / 2 - 128;

            int barScale = 4;
            int barHeight = 40;
            int barOffset = 4;

            Vector2 expBarPos = new(x, y - barHeight - barOffset);
            Vector2 healthBarPos = new(x, y);
            Vector2 manaBarPos = new(x, y + barHeight + barOffset);

            // Normalize experience values.
            int normalisedExp = (Player.Experience * 100 / Player.ExperienceNextLevel * 100) / 100;
            //int max = Player.Experience;
            //int min = 0;
            //int range = (max - min);
            //int normalisedNextLevel = 100 * (max - min) / range;

            //int max2 = Player.ExperienceNextLevel;
            //int min2 = 0;
            //int range2 = (max2 - min2);
            //int normalisedExperience = 100 * (max2 - min2) / range2;

            // Experience bars.
            Rectangle goldRect = new(0, 0, normalisedExp * barScale, barHeight);
            Rectangle blackRectExp = new(0, 0, 100 * barScale, barHeight);

            // Normalize health values.
            int normalisedHealth = (Player.Health * 100 / Player.HealthMax * 100) / 100;

            // Health bars.
            Rectangle greenRect = new(0, 0, normalisedHealth * barScale, barHeight);
            Rectangle redRect = new(0, 0, 100 * barScale, barHeight);

            // Normalize mana values.
            int normalisedMana = (Player.Mana * 100 / Player.ManaMax * 100) / 100;

            // Mana bars.
            Rectangle blueRect = new(0, 0, normalisedMana * barScale, barHeight);
            Rectangle blackRect = new(0, 0, 100 * barScale, barHeight);

            // Black bar.
            spriteBatch.Draw(
                Art.HealthBar,
                expBarPos,
                blackRectExp,
                Color.Black * 0.5f,
                0f,
                Vector2.Zero,
                1f,
                0,
                0
            );

            // Gold bar.
            spriteBatch.Draw(
                Art.HealthBar,
                expBarPos,
                goldRect,
                Color.Goldenrod,
                0f,
                Vector2.Zero,
                1f,
                0,
                0
            );

            // Red bar.
            spriteBatch.Draw(
                Art.HealthBar,
                healthBarPos,
                redRect,
                Color.DarkRed * 0.5f,
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
                Color.Black * 0.5f,
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

            // Experience.
            spriteBatch.DrawString(
                Art.HudFont,
                "Experience: " + Player.Experience + " / " + Player.ExperienceNextLevel,
                new Vector2(x, y - barHeight - barOffset),
                Color.White
            );

            // Health.
            spriteBatch.DrawString(
                Art.HudFont,
                "HP: " + Player.Health + " / " + Player.HealthMax,
                new Vector2(x, y + 0),
                Color.White
            );

            // Mana.
            spriteBatch.DrawString(
                Art.HudFont,
                "Mana: " + Player.Mana + " / " + Player.ManaMax,
                new Vector2(x, y + barHeight + barOffset),
                Color.White
            );
        }

        public static void DrawInventory(SpriteBatch spriteBatch)
        {
            Player.Instance.Inventory.Draw(spriteBatch);
        }

        public static void DrawDebug(SpriteBatch spriteBatch)
        {
            float x = 64;
            float y = 256;
            //float x = Game1.Camera.Pos.X - Game1.Viewport.Width / 2 + 64;
            //float y = Game1.Camera.Pos.Y - Game1.Viewport.Height / 2 + 64;
            Vector2 pos = new Vector2(x, y);

            spriteBatch.DrawString(
                Art.HudFont,
                "EntityManager.Count: " + EntityManager.Count,
                pos,
                Color.White
            );

            pos = new Vector2(x, y + 16);
            spriteBatch.DrawString(
                Art.HudFont,
                "Camera.Pos: " + Game1.Camera.Pos,
                pos,
                Color.White
            );

            pos = new Vector2(x, y + 32);
            spriteBatch.DrawString(
                Art.HudFont,
                "Player.Pos: " + Player.Instance.Position,
                pos,
                Color.White
            );
            pos = new Vector2(x, y + 48);
            spriteBatch.DrawString(
                Art.HudFont,
                "Game1.WorldBounds: " + Game1.WorldBounds,
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
