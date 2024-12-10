using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
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

        public Button()
        {
            texture = Art.ButtonTexture;
            font = Art.HudFont;
            scale = Game1.Scale;
            PenColor = Color.Black;
        }

        public Button(Texture2D _texture)
        {
            texture = _texture;
            font = Art.HudFont;
            scale = Game1.Scale;
            PenColor = Color.Black;
        }

        public Button(Texture2D _texture, SpriteFont _font)
        {
            texture = _texture;
            font = _font;
            scale = Game1.Scale;
            PenColor = Color.Black;
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

                spriteBatch.DrawString(font, Text, new Vector2(x, y), PenColor);
            }
        }

        //public void Update(GameTime gameTime, TouchCollection touchCollection
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

            // Check touch input.
            //if (touchCollection.Moved())
            //{
            //    foreach (TouchLocation tl in touchCollection)
            //    {
            //        int x = (int)tl.Position.X;
            //        int y = (int)tl.Position.Y;
            //        var touchRectangle = new Rectangle(x, y, 1, 1);
            //        if (touchRectangle.Intersects(Rectangle))
            //        {
            //            isHovering = true;
            //        }
            //    }
            //}

            //if (touchCollection.Released())
            //{
            //    foreach (TouchLocation tl in touchCollection)
            //    {
            //        int x = (int)tl.Position.X;
            //        int y = (int)tl.Position.Y;
            //        var touchRectangle = new Rectangle(x, y, 1, 1);
            //        if (touchRectangle.Intersects(Rectangle))
            //        {
            //            Pressed();
            //        }
            //    }
            //}
        }
        #endregion
    }
}
