using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Realm;

namespace Realm.Controls
{
    public class Button
    {
        #region Fields
        private MouseState mouse,
            previousMouse;
        private SpriteFont font;
        private bool isHovering;
        private Texture2D texture;
        private int scale;

        #endregion

        #region Properties
        public event EventHandler Click;
        public bool Clicked { get; private set; }
        public Color PenColor { get; set; }
        public Vector2 Position { get; set; }
        public Rectangle Rectangle
        {
            get
            {
                return new Rectangle(
                    (int)Position.X,
                    (int)Position.Y,
                    texture.Width * scale,
                    texture.Height * scale
                );
            }
        }

        public string Text { get; set; }

        #endregion

        #region Methods

        // PenColor defaults to White (not Black) — Button.Draw() now
        // outlines button text in black (Util.DrawOutlinedText), and a
        // black fill on a black outline renders as one undifferentiated
        // blob with none of the letterforms' own detail visible, not just
        // low-contrast. Callers that explicitly set PenColor afterward
        // (e.g. the Erase All Data / confirm buttons' Red/DarkRed) are
        // unaffected — this only changes what "didn't set one" means.
        //
        // font uses Art.RetroFont — the same font/size Settings' own
        // Back/Reset buttons already use via the explicit-font constructor
        // below — so every button in the game matches.
        public Button()
        {
            texture = Art.ButtonTexture;
            font = Art.RetroFont;
            scale = Game1.Scale;
            PenColor = Color.White;
        }

        public Button(Texture2D _texture)
        {
            texture = _texture;
            font = Art.RetroFont;
            scale = Game1.Scale;
            PenColor = Color.White;
        }

        public Button(Texture2D _texture, SpriteFont _font)
        {
            texture = _texture;
            font = _font;
            scale = Game1.Scale;
            PenColor = Color.White;
        }

        public void Pressed()
        {
            Click?.Invoke(this, new EventArgs());
            Sound.Play(Sound.Button, 0.25f);
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            var color = Color.White;

            if (isHovering)
            {
                color = Color.Gray;
            }

            spriteBatch.Draw(texture, Rectangle, color);

            if (!string.IsNullOrEmpty(Text))
            {
                var x = (Rectangle.X + (Rectangle.Width / 2)) - (font.MeasureString(Text).X / 2);
                var y = (Rectangle.Y + (Rectangle.Height / 2)) - (font.MeasureString(Text).Y / 2);

                Util.DrawOutlinedText(spriteBatch, font, Text, new Vector2(x, y), PenColor);
            }
        }

        public void Update(GameTime gameTime)
        {
            previousMouse = mouse;
            mouse = Mouse.GetState();

            var mouseRectangle = new Rectangle(mouse.X, mouse.Y, 1, 1);

            isHovering = false;

            if (mouseRectangle.Intersects(Rectangle))
            {
                isHovering = true;

                if (
                    mouse.LeftButton == ButtonState.Released
                    && previousMouse.LeftButton == ButtonState.Pressed
                )
                {
                    Pressed();
                }
            }
        }
        #endregion
    }
}
