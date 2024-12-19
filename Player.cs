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

        public static Guid ID;
        public static string Name;
        public static string Description;

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

        private static int baseDex = 17;

        public static int Experience;
        public static int ExperienceNextLevel;
        public static int ExperienceTotal;

        public static int Level;

        public static int ProjectileDuration;
        public static float ProjectileMagnitude;

        public Weapon Weapon;

        public Texture2D Texture;

        public Player()
        {
            ID = Guid.NewGuid();
            Name = "Player";
            Description = string.Empty;
            image = Art.Player;

            Inventory = new InventorySystem();

            instance = this;

            Health = 100;
            HealthMax = 100;

            Mana = 100;
            ManaMax = 100;

            Attack = 17;
            Defense = 0;
            Vitality = 5;
            Wisdom = 23;
            Speed = 17;
            Dexterity = 17;

            Experience = 0;
            ExperienceNextLevel = 50;
            ExperienceTotal = 0;

            Level = 1;

            ProjectileDuration = 32;
            ProjectileMagnitude = 12f;

            Weapon = new Weapon(Art.Wand, Art.Projectile2);

            Position = new Vector2(Game1.WorldWidth / 2, Game1.WorldHeight / 2);
            Radius = image.Width / 2f;
        }

        public static void UseAbility()
        {
            int abilityCost = 25;

            int damage = Instance.rand.Next(10, 15);

            if (Mana >= abilityCost)
            {
                Mana -= abilityCost;

                // Spell bomb.
                for (int i = 0; i < 35; i++)
                {
                    Vector2 vel = Extensions.FromPolar(i * 10, ProjectileMagnitude);
                    EntityManager.Add(
                        new Projectile(Input.GetMousePosition(), vel) { Damage = damage }
                    );
                }
            }
            else
            {
                Sound.Play(Sound.NoMana, 0.4f);
            }
        }

        private void Shoot()
        {
            Weapon.Shoot();
        }

        public static void LevelUp()
        {
            Attack = 17 + (Level * 2);
            Defense = 0 + (int)(Level * 0.5);
            Vitality = 5 + (Level * 1);
            Wisdom = 23 + (Level * 1);
            Speed = 17 + (Level * 1);
            Dexterity = 17 + (Level * 2);

            HealthMax = 100 + (Level * 25);
            ManaMax = 100 + (Level * 10);

            Health = HealthMax;
            Mana = ManaMax;

            Level++;

            Experience = 0;
            ExperienceNextLevel = 50 + ((Level * 2) * 50);

            Sound.Play(Sound.LevelUp, 0.4f);
        }

        public static void Hit(int damage = 25)
        {
            int damageModified = damage - Defense;
            if (damageModified <= damage / 10)
            {
                damageModified = damage / 10;
            }

            Health = Health - damageModified;
            if (Health <= 0)
            {
                Kill();
            }
            else
            {
                Sound.Play(Sound.WizardHit, 0.45f);
            }
        }

        public static void Kill()
        {
            EnemySpawner.Reset();
            EntityManager.Reset();
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
        private int healthCooldownCount = 160;

        private int manaCooldown = 0;
        private int manaCooldownCount = 320;

        private int projectileCooldown = 0;
        private readonly int projectileCooldownCount = 240;

        public override void Update()
        {
            // Update position.
            Velocity = (int)((Speed / 75) * 5.6 + 2) * Input.GetMovementDirection();
            Position += Velocity;

            // Update camera position.
            Game1.Camera.Pos += Velocity;

            // Check for level up
            if (Level < 20 && Experience >= ExperienceNextLevel)
            {
                LevelUp();
            }

            // Regenerate Health.
            healthCooldown += 1 + (int)(0.24 * Vitality);
            if (healthCooldown >= healthCooldownCount)
            {
                healthCooldown = 0;
                if (Health < HealthMax)
                    Health++;
            }

            // Regenerate mana.
            manaCooldown += 1 + (int)(0.12 * Wisdom);
            if (manaCooldown >= manaCooldownCount)
            {
                manaCooldown = 0;
                if (Mana < ManaMax)
                    Mana++;
            }

            // Shoot
            // This may be moved to new Weapon class.
            projectileCooldown += ((Dexterity * 100) / 150 * 100) / 100;
            if (
                Input.mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed
                && projectileCooldown >= projectileCooldownCount
            )
            {
                projectileCooldown = 0;
                Shoot();
            }

            // Update weapon.
            this.Weapon.Update();
        }
    }
}
