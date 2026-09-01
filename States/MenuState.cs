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

        // Background fades in from 0 up to this cap rather than snapping
        // straight to a static opacity — a plain fade over
        // BackgroundFadeDurationSeconds. Tracked here (not inside
        // Background itself) since MenuState is sometimes reused across a
        // trip to Settings and back (StateManager.OpenSettings passes this
        // same instance as the return state) — the fade should only ever
        // play once per instance, not replay every time this screen is
        // shown again.
        private const float BackgroundMaxOpacity = 0.5f;
        private const float BackgroundFadeDurationSeconds = 2f;
        private float backgroundFadeTimer;

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
            StateManager.CharacterSlots();
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            // Draw background.
            float backgroundOpacity =
                BackgroundMaxOpacity
                * Math.Min(backgroundFadeTimer / BackgroundFadeDurationSeconds, 1f);
            Background.Draw(spriteBatch, backgroundOpacity);

            // Draw title.
            Overlay.DrawTitle(spriteBatch);

            // Draw menu.
            menu.Draw(gameTime, spriteBatch);

            spriteBatch.End();
        }

        public override void PostUpdate(GameTime gameTime) { }

        public override void Update(GameTime gameTime)
        {
            backgroundFadeTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            foreach (var button in buttons)
            {
                button.Update(gameTime);
            }
        }
    }
}
