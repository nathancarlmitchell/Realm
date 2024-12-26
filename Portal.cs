using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm.Controls;
using Realm.States;

namespace Realm
{
    public class Portal
    {
        public enum Destination
        {
            None,
            Realm,
            CharacterSelect,
        }

        private AnimatedTexture image;
        private Vector2 position;
        private Rectangle bounds
        {
            get { return new Rectangle((int)position.X + 64, (int)position.Y + 64, 32, 32); }
        }
        private bool intersects = false;
        private Button enterButton;
        public Destination dest;

        public Portal()
        {
            image = Art.Portal;
            position.X = Player.Instance.Position.X + 100;
            position.Y = Player.Instance.Position.Y + 100;
            dest = Destination.Realm;
        }

        public Portal(Vector2 position, Destination dest)
        {
            image = Art.Portal;
            this.position = position;
            this.dest = dest;
        }

        public void EnterPortal()
        {
            switch (dest)
            {
                case Destination.Realm:
                    StateManager.EnterPortal();
                    break;

                case Destination.CharacterSelect:
                    StateManager.SelectClass();
                    break;
            }
        }

        public void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            image.UpdateFrame(elapsed);

            if (Player.Instance.Bounds.Intersects(bounds))
            {
                EnterPortal();
            }

            //intersects = false;
            //if (Player.Instance.Bounds.Intersects(bounds))
            //{
            //    intersects = true;
            //    Player.Opacity -= 0.025f;
            //}
            //else
            //{
            //    Player.Opacity += 0.025f;
            //    if (Player.Opacity > 1)
            //    {
            //        Player.Opacity = 1;
            //    }
            //}

            //if (Player.Opacity <= -0.5)
            //{
            //    StateManager.EnterPortal();
            //    Player.Opacity = 1f;
            //}

            //image.Opacity = Player.Opacity;
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            image.DrawFrame(spriteBatch, position);
        }
    }
}
