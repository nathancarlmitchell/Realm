using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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

        public static float Opacity;

        public InventorySystem Inventory { get; set; }

        public static int Health;
        public static int HealthMax;

        public static int Mana;
        public static int ManaMax;

        public static int Attack;
        public static int Defense;
        public static float Speed;
        public static int Dexterity;
        public static int Vitality;
        public static int Wisdom;

        public static int baseHealth = 100;
        public static int baseMana = 100;
        public static int baseAttack = 17;
        public static int baseDefense = 0;
        public static float baseSpeed = 17;
        public static int baseDexterity = 17;
        public static int BaseVitality = 5;
        public static int baseWisdom = 23;

        public static int MaxHealth;
        public static int MaxMana;
        public static int MaxAttack;
        public static int MaxDefense;
        public static float MaxSpeed;
        public static int MaxDexterity;
        public static int MaxVitality;
        public static int MaxWisdom;

        public static int Experience;
        public static int ExperienceNextLevel;
        public static int ExperienceTotal;

        public static int Level;

        public Weapon Weapon;

        public Texture2D Texture;

        public enum Class
        {
            Wizard, // 0
            Archer, // 1
        }

        public Class PlayerClass { get; set; }

        public enum PlayerWeaponType
        {
            Wand, // 0
            Bow, // 1
        }

        public PlayerWeaponType WeaponType { get; set; }

        public Player()
        {
            ID = Guid.NewGuid();

            Opacity = 1f;

            Inventory = new InventorySystem();

            instance = this;

            Experience = 0;
            ExperienceNextLevel = 50;
            ExperienceTotal = 0;

            Level = 1;

            Position = new Vector2(Game1.WorldWidth / 2, Game1.WorldHeight / 2);

            Weapon = new Weapon();

            // Radius is not accurate,
            // Archer is smaller.
            Radius = 64 / 2f;
        }

        public virtual void LevelUp()
        {
            Health = HealthMax;
            Mana = ManaMax;

            Level++;

            Experience = 0;
            ExperienceNextLevel = 50 + ((Level * 2) * 50);

            Sound.Play(Sound.LevelUp, 0.4f);
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
                    Vector2 vel = Extensions.FromPolar(i * 10, Instance.Weapon.ProjectileMagnitude);
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

        public static void Hit(int damage = 25)
        {
            int damageModified = damage - Defense;
            if (damageModified <= damage / 10)
            {
                damageModified = damage / 10;
            }

            //Health = Health - damageModified;
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
            StateManager.GameOver();
        }

        private int healthCooldown = 0;
        private int healthCooldownCount = 160;

        private int manaCooldown = 0;
        private int manaCooldownCount = 320;

        private int projectileCooldown = 0;
        private readonly int projectileCooldownCount = 240;

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(
                image,
                Position,
                null,
                color * Opacity,
                Orientation,
                Size / 2f,
                1f,
                0,
                0
            );
        }

        public override void Update()
        {
            // Update position.
            Velocity = (int)((Speed / 75) * 5.6 + 2) * Input.GetMovementDirection();
            Position += Velocity;

            // Update camera position.
            Game1.Camera.Pos += Velocity;

            // Check for level up.
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
                Input.mouse.LeftButton == ButtonState.Pressed
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
