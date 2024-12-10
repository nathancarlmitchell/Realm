using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm;

namespace Realm.Controls
{
    public class Menu
    {
        private List<Button> buttons;

        public Menu(List<Button> _buttons)
        {
            buttons = _buttons;
            int scale = Game1.Scale;

            int centerHeight = (Game1.ScreenHeight / 2) + (buttons.Count * 20);
            int centerWidth = (Game1.Width / 2) - ((Art.ButtonTexture.Width * scale) / 2);

            for (int i = 0; i < buttons.Count; i++)
            {
                int totalComponents = buttons.Count;
                int centerComponent = totalComponents / 2;
                int difference = i - centerComponent;

                int buttonHeight = centerHeight + difference * 50 * scale;

                // Y position of single buttons
                if (buttons.Count == 1)
                {
                    buttonHeight = Game1.ScreenHeight - 64 * 4;
                }

                buttons[i].Position = new Vector2(centerWidth, buttonHeight);
            }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                buttons[i].Draw(gameTime, spriteBatch);
            }
        }
    }
}
