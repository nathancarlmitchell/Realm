using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;

namespace Realm
{
    public class Player : Entity
    {
        private static Player instance;
        public static Player Instance
        {
            get
            {
                if (instance == null)
                    instance = new Player();
                return instance;
            }
        }

        private readonly Random rand = new();

        public int id;
        public string name;
        public string description;

        //public static Vector2 Posistion;

        public static float Speed;
        public static int Dexterity;
        public static int ProjectileDuration;
        public static float ProjectileMagnitude;

        public Texture2D Texture;

        public Player()
        {
            id = 0;
            name = "Player";
            description = string.Empty;

            Speed = 4;
            Dexterity = 1;

            ProjectileDuration = 24;
            ProjectileMagnitude = 12f;

            Position = Game1.ScreenSize / 2;

            image = Art.Player;
        }

        //public override void Draw(SpriteBatch spriteBatch)
        //{
        //    // Draw player texture.
        //    Player.Instance.Draw(spriteBatch);
        //    //Texture.DrawFrame(spriteBatch, new Vector2(Position.X - 32, Position.Y - 32));
        //}

        public void Update(float elapsed, GameTime gameTime)
        {
            // Update player animation.
            //Texture.UpdateFrame(elapsed);
        }

        private int cooldownRemaining = 0;
        private int cooldownFrames = 24;

        public override void Update()
        {
            Velocity = Speed * Input.GetMovementDirection();
            Position += Velocity;
            //Position = Vector2.Clamp(Position, Size / 2, Game1.ScreenSize - Size / 2);

            // Shoot
            if (Input.MouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
            {
                var aim = Input.GetMouseAimDirection();
                if (aim.LengthSquared() > 0 && cooldownRemaining <= 0)
                {
                    cooldownRemaining = cooldownFrames;
                    float aimAngle = aim.ToAngle();
                    Quaternion aimQuat = Quaternion.CreateFromYawPitchRoll(0, 0, aimAngle);
                    float randomSpread =
                        rand.NextFloat(-0.04f, 0.04f) + rand.NextFloat(-0.04f, 0.04f);
                    //Vector2 vel = Extensions.FromPolar(aimAngle + randomSpread, 11f);
                    Vector2 vel = Extensions.FromPolar(
                        aimAngle + randomSpread,
                        ProjectileMagnitude
                    );
                    //Vector2 offset = Vector2.Transform(new Vector2(25, -8), aimQuat);
                    EntityManager.Add(new Projectile(Position, vel));
                    //offset = Vector2.Transform(new Vector2(25, 8), aimQuat);
                    //EntityManager.Add(new Projectile(Position + offset, vel));
                    //Sound.Shot.Play(0.2f, rand.NextFloat(-0.2f, 0.2f), 0);
                }
                if (cooldownRemaining > 0)
                    cooldownRemaining = cooldownRemaining - Dexterity;
            }
        }
    }
}
