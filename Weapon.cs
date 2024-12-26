using System;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm.Data;

namespace Realm
{
    public class Weapon : Item
    {
        public enum WeaponType
        {
            Wand, // 0
            Bow, // 1
        }

        public WeaponType Type { get; set; }

        public int Teir { get; set; }

        public int DamageMin { get; set; }
        public int DamageMax { get; set; }
        public int FireRate { get; set; }
        public float ProjectileMagnitude { get; set; }
        public int ProjectileDuration { get; set; }
        public Texture2D ProjectileImage;
        public string ProjectileImageName { get; set; }

        private readonly Random rand = new();
        private Rectangle bounds;

        static int x = Game1.ScreenWidth - 420;
        static int y = 512;
        bool hover = false;

        public Weapon(Texture2D image, Texture2D projectileImage)
        {
            ID = Guid.NewGuid();
            Name = "Fire Wand";
            Description = "A wizard's most important tool.";
            Teir = 0;

            this.image = image;
            ProjectileImage = projectileImage;

            ProjectileDuration = 32;
            ProjectileMagnitude = 12f;

            DamageMin = 30;
            DamageMax = 55;

            bounds = new Rectangle(x, y, image.Width, image.Height);
        }

        public static Weapon LoadWeapon(string weaponName)
        {
            Weapon weaponData = Game1.Instance.Weapons.FirstOrDefault(x => (x.Name == weaponName));

            if (weaponData.Type.ToString() == Player.Instance.WeaponType.ToString())
            {
                Texture2D weaponTexture = Game1.Instance.Content.Load<Texture2D>(
                    weaponData.ImageName
                );

                Texture2D projectileTexture = Game1.Instance.Content.Load<Texture2D>(
                    weaponData.ProjectileImageName
                );

                Weapon weapon = new(weaponTexture, projectileTexture)
                {
                    Type = weaponData.Type,
                    Name = weaponData.Name,
                    Description = weaponData.Description,
                    Teir = weaponData.Teir,
                    DamageMin = weaponData.DamageMin,
                    DamageMax = weaponData.DamageMax,
                    ProjectileMagnitude = weaponData.ProjectileMagnitude,
                    ProjectileDuration = weaponData.ProjectileDuration,
                    ProjectileImageName = weaponData.ProjectileImageName,
                };

                return weapon;
            }

            Sound.Play(Sound.Error, 0.4f);
            return null;
        }

        public Weapon()
        {
            // Initialized in Player.cs.
        }

        public void Shoot()
        {
            double damgeModifier = (0.5 + Player.Attack / 50);
            double damage = rand.Next(DamageMin, DamageMax) * damgeModifier;

            var aim = Input.GetMouseAimDirection();

            if (aim.LengthSquared() > 0)
            {
                float aimAngle = aim.ToAngle();
                float randomSpread = rand.NextFloat(-0.04f, 0.04f) + rand.NextFloat(-0.04f, 0.04f);

                Vector2 vel = Extensions.FromPolar(
                    aimAngle + randomSpread,
                    this.ProjectileMagnitude
                );

                EntityManager.Add(
                    new Projectile(Player.Instance.Position, vel)
                    {
                        image = this.ProjectileImage,
                        Damage = (int)damage,
                    }
                );

                Sound.Play(Sound.MagicShoot, 0.3f);
            }
        }

        public override void Update()
        {
            hover = false;

            if (bounds.Intersects(Input.MouseBounds))
            {
                // Mouse over weapon.
                hover = true;
            }
        }

        //This method needs to be replaced by drawing from the inventory.
        public void DrawEquipped(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Art.Border, new Vector2(x, y), Color.White);
            spriteBatch.Draw(this.image, new Vector2(x, y), Color.White);

            if (hover)
            {
                string text =
                    $"T{Teir} - {Name}{Environment.NewLine}{Description}{Environment.NewLine}Damge: {DamageMin} - {DamageMax}";

                int textY = (int)(Art.HudFont.MeasureString(text).Y / 2);

                spriteBatch.DrawString(
                    Art.HudFont,
                    text,
                    new Vector2(x, y - image.Height - textY),
                    Color.Red
                );
            }
        }
    }
}
