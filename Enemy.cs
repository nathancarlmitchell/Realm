using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm;

namespace Realm
{
    class Enemy : Entity
    {
        static Random rand = new Random();
        private int timeUntilStart = 60;
        public bool IsActive
        {
            get { return timeUntilStart <= 0; }
        }
        public int PointValue { get; private set; }
        private List<IEnumerator<int>> behaviours = new List<IEnumerator<int>>();

        public Enemy(Texture2D image, Vector2 position)
        {
            this.image = image;
            Position = position;
            Radius = image.Width / 2f;
            color = Color.Transparent;
            PointValue = 1;
        }

        public override void Update()
        {
            if (timeUntilStart <= 0)
            {
                ApplyBehaviours();
            }
            else
            {
                timeUntilStart--;
                color = Microsoft.Xna.Framework.Color.White * (1 - timeUntilStart / 60f);
            }
            Position += Velocity;
            Position = Vector2.Clamp(Position, Size / 2, Game1.ScreenSize - Size / 2);
            Velocity *= 0.8f;
        }

        public void WasShot()
        {
            IsExpired = true;
            //PlayerStatus.AddPoints(PointValue);
            //PlayerStatus.IncreaseMultiplier();
            //Sound.Explosion.Play(0.5f, rand.NextFloat(-0.2f, 0.2f), 0);
        }

        IEnumerable<int> FollowPlayer(float acceleration = 1f)
        {
            while (true)
            {
                Velocity += (Player.Instance.Position - Position).ScaleTo(acceleration);
                if (Velocity != Vector2.Zero)
                    Orientation = Velocity.ToAngle();
                yield return 0;
            }
        }

        private void AddBehaviour(IEnumerable<int> behaviour)
        {
            behaviours.Add(behaviour.GetEnumerator());
        }

        private void ApplyBehaviours()
        {
            for (int i = 0; i < behaviours.Count; i++)
            {
                if (!behaviours[i].MoveNext())
                    behaviours.RemoveAt(i--);
            }
        }

        public static Enemy CreateSeeker(Vector2 position)
        {
            var enemy = new Enemy(Art.Enemy, position);
            enemy.AddBehaviour(enemy.FollowPlayer());
            return enemy;
        }

        IEnumerable<int> MoveRandomly()
        {
            float direction = rand.NextFloat(0, MathHelper.TwoPi);
            while (true)
            {
                direction += rand.NextFloat(-0.1f, 0.1f);
                direction = MathHelper.WrapAngle(direction);
                for (int i = 0; i < 6; i++)
                {
                    Velocity += Extensions.FromPolar(direction, 0.4f);
                    Orientation -= 0.05f;
                    var bounds = Game1.Viewport.Bounds;
                    bounds.Inflate(-image.Width, -image.Height);
                    // if the enemy is outside the bounds, make it move away from the edge
                    if (!bounds.Contains(Position.ToPoint()))
                        direction =
                            (Game1.ScreenSize / 2 - Position).ToAngle()
                            + rand.NextFloat(-MathHelper.PiOver2, MathHelper.PiOver2);
                    yield return 0;
                }
            }
        }

        public static Enemy CreateWanderer(Vector2 position)
        {
            var enemy = new Enemy(Art.Enemy, position);
            enemy.AddBehaviour(enemy.MoveRandomly());
            return enemy;
        }

        public void HandleCollision(Enemy other)
        {
            var d = Position - other.Position;
            Velocity += 10 * d / (d.LengthSquared() + 1);
        }
    }
}
