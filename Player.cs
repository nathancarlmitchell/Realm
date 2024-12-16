using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
            set { instance = value; }
        }

        private readonly Random rand = new();

        public int id;
        public string name;
        public string description;

        //private static InventorySystem inventory;
        //public static InventorySystem Inventory
        //{
        //    get
        //    {
        //        if (inventory == null)
        //            inventory = new InventorySystem();
        //        return inventory;
        //    }
        //}

        public InventorySystem Inventory { get; set; }

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

            Inventory = new InventorySystem();

            instance = this;

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
            ExperienceNextLevel = 25;
            ExperienceTotal = 0;

            Level = 1;

            ProjectileDuration = 32;
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

        private void Shoot()
        {
            var aim = Input.GetMouseAimDirection();
            if (aim.LengthSquared() > 0)
            {
                float aimAngle = aim.ToAngle();
                Quaternion aimQuat = Quaternion.CreateFromYawPitchRoll(0, 0, aimAngle);
                float randomSpread = rand.NextFloat(-0.04f, 0.04f) + rand.NextFloat(-0.04f, 0.04f);
                //Vector2 vel = Extensions.FromPolar(aimAngle + randomSpread, 11f);
                Vector2 vel = Extensions.FromPolar(aimAngle + randomSpread, ProjectileMagnitude);
                //Vector2 offset = Vector2.Transform(new Vector2(25, -8), aimQuat);
                EntityManager.Add(new Projectile(Position, vel) { image = Art.Projectile2 });
                //offset = Vector2.Transform(new Vector2(25, 8), aimQuat);
                //EntityManager.Add(new Projectile(Position + offset, vel));
                //Sound.Shot.Play(0.2f, rand.NextFloat(-0.2f, 0.2f), 0);
            }
        }

        public static void LevelUp()
        {
            Level++;

            Attack++;
            Defense++;

            Vitality += Level / 2;
            Wisdom += Level;

            if (Level % 3 == 0)
            {
                Speed++;
            }

            Dexterity += Level / 2;

            HealthMax += 5;
            ManaMax += 5;

            Health = HealthMax;
            Mana = ManaMax;

            Experience = 0;
            ExperienceNextLevel = 25 * Level * Level;

            Sound.Play(Sound.LevelUp, 0.3f);
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
            //Player.Instance = new Player();
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

        private int healthCooldown = 0;
        private int healthCooldownCount = 500;

        private int manaCooldown = 0;
        private int manaCooldownCount = 500;

        private int projectileCooldown = 0;
        private readonly int projectileCooldownCount = 100;

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
            healthCooldown += Vitality;
            if (healthCooldown >= healthCooldownCount)
            {
                healthCooldown = 0;
                if (Health < HealthMax)
                    Health++;
            }
            //if (healthCooldownRemaining >= 0)
            //    healthCooldown -= (Vitality * 1);

            // Regenerate mana.
            manaCooldown += Wisdom;
            if (manaCooldown >= manaCooldownCount)
            {
                manaCooldown = 0;
                if (Mana < ManaMax)
                    Mana++;
            }

            // Shoot
            // This may be moved to new Weapon class.
            projectileCooldown += Dexterity;
            if (
                Input.mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed
                && projectileCooldown >= projectileCooldownCount
            )
            {
                projectileCooldown = 0;
                Shoot();
            }
        }
    }
}
