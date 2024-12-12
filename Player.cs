using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm.States;

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

        public static int Health;
        public static int HealthMax;

        public static int Mana;
        public static int ManaMax;

        public static int Attack;
        public static int Defense;
        public static int Vitality;
        public static int Wisdom;
        public static float Speed;
        public static int Dexterity;

        public static int Experience;
        public static int ExperienceNextLevel;
        public static int ExperienceTotal;

        public static int Level;

        public static int ProjectileDuration;
        public static float ProjectileMagnitude;

        public Texture2D Texture;

        public Player()
        {
            id = 0;
            name = "Player";
            description = string.Empty;
            image = Art.Player;

            Health = 100;
            HealthMax = 100;

            Mana = 100;
            ManaMax = 100;

            Attack = 1;
            Defense = 1;
            Vitality = 1;
            Wisdom = 1;
            Speed = 2;
            Dexterity = 1;

            Experience = 0;
            ExperienceNextLevel = 10;
            ExperienceTotal = 0;

            Level = 1;

            ProjectileDuration = 24;
            ProjectileMagnitude = 12f;

            Position = new Vector2(Game1.WorldWidth / 2, Game1.WorldHeight / 2);
            Radius = image.Width / 2f;
        }

        public static void UseAbility()
        {
            int abilityCost = 25;

            if (Mana >= abilityCost)
            {
                Mana -= abilityCost;

                // Spell bomb.
                for (int i = 0; i < 35; i++)
                {
                    Vector2 vel = Extensions.FromPolar(i * 10, ProjectileMagnitude);
                    EntityManager.Add(new Projectile(Input.GetMousePosistion(), vel));
                }
            }
        }

        private int projectileCooldownRemaining = 0;
        private readonly int projectileCooldown = 50;

        private void Shoot()
        {
            var aim = Input.GetMouseAimDirection();
            if (aim.LengthSquared() > 0 && projectileCooldownRemaining <= 0)
            {
                projectileCooldownRemaining = projectileCooldown - (Dexterity * 1);
                float aimAngle = aim.ToAngle();
                Quaternion aimQuat = Quaternion.CreateFromYawPitchRoll(0, 0, aimAngle);
                float randomSpread = rand.NextFloat(-0.04f, 0.04f) + rand.NextFloat(-0.04f, 0.04f);
                //Vector2 vel = Extensions.FromPolar(aimAngle + randomSpread, 11f);
                Vector2 vel = Extensions.FromPolar(aimAngle + randomSpread, ProjectileMagnitude);
                //Vector2 offset = Vector2.Transform(new Vector2(25, -8), aimQuat);
                EntityManager.Add(new Projectile(Position, vel));
                //offset = Vector2.Transform(new Vector2(25, 8), aimQuat);
                //EntityManager.Add(new Projectile(Position + offset, vel));
                //Sound.Shot.Play(0.2f, rand.NextFloat(-0.2f, 0.2f), 0);
            }
            if (projectileCooldownRemaining > 0)
                projectileCooldownRemaining--;
        }

        public static void LevelUp()
        {
            Level++;

            Attack++;
            Defense++;

            Vitality += Level / 2;
            Wisdom += Level / 2;

            if (Level % 3 == 0)
            {
                Speed++;
            }

            Dexterity += Level / 2;

            Experience = 0;
            ExperienceNextLevel = 10 * Level * Level;
        }

        public static void Hit(int damage = 25)
        {
            Health = Health - damage;
            if (Health <= 0)
            {
                Kill();
            }
        }

        public static void Kill()
        {
            EnemySpawner.Reset();
            EntityManager.Reset();
            GameState.Player = new Player();
            Player.Instance.Position = Game1.WorldSize / 2;
            Camera.Reset();
            Game1.Instance.ChangeState(
                new GameOverState(
                    Game1.Instance,
                    Game1.Instance.GraphicsDevice,
                    Game1.Instance.Content
                )
            );
        }

        private int healthCooldownRemaining = 0;
        private readonly int healthCooldown = 500;

        private int manaCooldownRemaining = 0;
        private readonly int manaCooldown = 250;

        public override void Update()
        {
            // Update position.
            Velocity = Speed * Input.GetMovementDirection();
            Position += Velocity;

            // Update camera position.
            Game1.Camera.Pos += Velocity;

            //Position = Vector2.Clamp(Position, Size / 2, Game1.ScreenSize - Size / 2);

            // Check for level up
            if (Experience >= ExperienceNextLevel)
            {
                Debug.WriteLine("Level up.");
                LevelUp();
            }

            // Regenerate Health.
            if (healthCooldownRemaining <= 0)
            {
                healthCooldownRemaining = healthCooldown - (Vitality * 1);
                if (Health < HealthMax)
                    Health++;
            }
            if (healthCooldownRemaining > 0)
                healthCooldownRemaining--;

            // Regenerate mana.
            if (manaCooldownRemaining <= 0)
            {
                manaCooldownRemaining = manaCooldown - (Wisdom * 1);
                if (Mana < ManaMax)
                    Mana++;
            }
            if (manaCooldownRemaining > 0)
                manaCooldownRemaining--;

            // Shoot
            // This may be moved to new Weapon class.
            if (Input.mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
            {
                Shoot();
            }
        }
    }
}
