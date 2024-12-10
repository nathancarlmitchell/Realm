using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    public class Player : Entity
    {
        public int id;
        public string name;
        public string description;

        public static float Speed;

        public AnimatedTexture Texture;

        public Player()
        {
            id = 0;
            name = "Player";
            description = string.Empty;

            Speed = 8;
            Position = Game1.ScreenSize / 2;

            Texture = Art.Player;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Draw player texture.
            Texture.DrawFrame(spriteBatch, new Vector2(Position.X, Position.Y));
        }

        public void Update(float elapsed, GameTime gameTime)
        {
            // Update player animation.
            Texture.UpdateFrame(elapsed);
        }

        public override void Update()
        {
            Velocity = Speed * Input.GetMovementDirection();
            Position += Velocity;
            //Position = Vector2.Clamp(Position, Size / 2, Game1.ScreenSize - Size / 2);
        }
    }
}
