using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Realm.Controls;

namespace Realm.States
{
    public class GameOverState : State
    {
        private readonly List<Button> butttons;
        private readonly Menu menu;
        private readonly SpriteFont titleFont;

        private int score;
        private int fameEarned;

        public GameOverState(Game1 game, GraphicsDevice graphicsDevice, ContentManager content)
            : base()
        {
            Game1.Instance.IsMouseVisible = true;

            Sound.SongInstance.Stop();
            Sound.Play(Sound.PlayerDeath, 0.4f);

            titleFont = Art.RetroFontMedium;

            score = Player.Instance.ExperienceTotal;

            // Fame earned on death is Base Fame (the automatic XP-to-fame
            // conversion accrued throughout this life) plus Bonus Fame
            // (achievements — none exist yet, so this is currently always
            // 0), not a straight 1:1 copy of Score. Salvaged into the
            // account-level Fame total before both are wiped by the reset
            // below.
            fameEarned = Player.Instance.BaseFame + Player.Instance.BonusFame;
            FameSystem.AddFame(fameEarned);

            var newGameButton = new Button() { Text = "New Game" };
            newGameButton.Click += NewGameButton_Click;

            var mainMenuButton = new Button() { Text = "Main Menu" };
            mainMenuButton.Click += MainMenuButton_Click;

            var quitGameButton = new Button() { Text = "Quit" };
            quitGameButton.Click += QuitGameButton_Click;

            butttons = [newGameButton, mainMenuButton, quitGameButton];
            menu = new Menu(butttons);

            // The character that just died resets to its base stats — its High Score
            // (and permanent star-rating flag, HasReachedLevel20) are kept as a
            // permanent record, but nothing else carries over (including
            // HasBeenPlayed), so a death leaves the save looking the same as an
            // explicit Delete: back to defaults, with Character Select correctly
            // hiding the Delete link again until this class is played some more.
            Player.Class diedClass = Player.PlayerClass;
            int highScore = Player.Instance.HighScore;
            bool hasReachedLevel20 = Player.Instance.HasReachedLevel20;

            EntityManager.RemovePlayer();

            Util.ResetPlayer(diedClass);
            Player.Instance.HighScore = highScore;
            Player.Instance.HasReachedLevel20 = hasReachedLevel20;

            EntityManager.Add(Player.Instance);

            Util.SavePlayerData();
            Util.SaveInventoryData();
            Util.SaveBankData();
            Util.SaveFameData();
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            int x;
            int y = 128;

            Color color = Color.AliceBlue;

            string fameText = "Fame Earned: " + fameEarned;
            int fameY = y;
            int fameX = (int)(CenterWidth - (titleFont.MeasureString(fameText).X / 2));

            spriteBatch.DrawString(
                titleFont,
                fameText,
                new Vector2(fameX - 4, fameY + 4),
                Color.Black * 0.5f
            );
            spriteBatch.DrawString(titleFont, fameText, new Vector2(fameX, fameY), color);

            menu.Draw(gameTime, spriteBatch);

            spriteBatch.End();
        }

        private void NewGameButton_Click(object sender, EventArgs e)
        {
            // Not a direct NewGame() — the class that just died was reset to
            // HasBeenPlayed = false in the constructor above, same as an explicit
            // Delete, so if that was the only character ever played, nothing
            // qualifies as "played" anymore and this needs to defer to Character
            // Select instead of silently restarting the same class.
            StateManager.EnterNexus();
        }

        private void MainMenuButton_Click(object sender, EventArgs e)
        {
            StateManager.MainMenu();
        }

        private void QuitGameButton_Click(object sender, EventArgs e)
        {
            StateManager.ExitGame();
        }

        public override void PostUpdate(GameTime gameTime) { }

        public override void Update(GameTime gameTime)
        {
            foreach (var button in butttons)
            {
                button.Update(gameTime);
            }
        }
    }
}
