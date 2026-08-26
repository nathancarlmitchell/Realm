using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Realm.Controls;

namespace Realm.States
{
    public class MenuState : State
    {
        private readonly List<Button> buttons;
        private readonly Menu menu;

        public MenuState(Game1 game, GraphicsDevice graphicsDevice, ContentManager content)
            : base()
        {
            Game1.Instance.IsMouseVisible = true;

            var newGameButton = new Button() { Text = "Nexus" };
            newGameButton.Click += NewGameButton_Click;

            var classButton = new Button() { Text = "Character Select" };
            classButton.Click += ClassButton_Click;

            var settingsButton = new Button() { Text = "Settings" };
            settingsButton.Click += SettingsButton_Click;

            var quitGameButton = new Button() { Text = "Quit" };
            quitGameButton.Click += QuitGameButton_Click;

            buttons = [newGameButton, classButton, settingsButton, quitGameButton];
            menu = new Menu(buttons);
        }

        private void QuitGameButton_Click(object sender, EventArgs e)
        {
            StateManager.ExitGame();
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            StateManager.OpenSettings(this);
        }

        private void NewGameButton_Click(object sender, EventArgs e)
        {
            StateManager.EnterNexus();
        }

        private void ClassButton_Click(object sender, EventArgs e)
        {
            StateManager.SelectClass();
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            // Draw background.
            Background.Draw(spriteBatch);

            // Draw title.
            Overlay.DrawTitle(spriteBatch);

            // Draw menu.
            menu.Draw(gameTime, spriteBatch);

            spriteBatch.End();
        }

        public override void PostUpdate(GameTime gameTime) { }

        public override void Update(GameTime gameTime)
        {
            foreach (var button in buttons)
            {
                button.Update(gameTime);
            }
        }
    }
}
