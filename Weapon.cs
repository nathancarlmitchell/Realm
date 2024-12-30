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
        public Rectangle WeaponSlotBounds;

        static int x = Game1.ScreenWidth - 420;
        static int y = 512;
        bool hover = false;

        public Weapon(Texture2D image, Texture2D projectileImage)
        {
            ID = Guid.NewGuid();

            this.image = image;
            ProjectileImage = projectileImage;

            WeaponSlotBounds = new Rectangle(x, y, image.Width, image.Height);
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

                Player.Instance.Weapon = weapon;
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
            dragItem = false;
            //mousePressed = false;

            if (WeaponSlotBounds.Intersects(Input.MouseBounds))
            {
                // Mouse over weapon.
                hover = true;
            }

            // Mouse pressed.
            if (Input.MousePressed())
            {
                if (!mousePressed && hover)
                {
                    dragItem = true;
                }
                mousePressed = true;
                //return;
            }

            // Mouse released.
            if (Input.MouseReleased())
            {
                mousePressed = false;
                dragItem = false;
                // DO SOMETHING ELSE.
                //Player.Instance.Inventory.SwapItem = true;
            }
        }

        private bool mousePressed = false;
        private bool dragItem = false;

        //This method needs to be replaced by drawing from the inventory.
        public void DrawEquipped(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Art.Border, new Vector2(x, y), Color.White);
            spriteBatch.Draw(this.image, new Vector2(x, y), Color.White);

            if (hover)
            {
                string description = Util.WrapText(Art.HudFont, Description, 350);
                string text =
                    $"T{Teir} - {Name}{Environment.NewLine}{description}{Environment.NewLine}Damge: {DamageMin} - {DamageMax}";

                int textY = (int)(Art.HudFont.MeasureString(text).Y / 2);

                spriteBatch.DrawString(
                    Art.HudFont,
                    text,
                    new Vector2(x, y - image.Height - textY),
                    Color.Red
                );
            }

            if (dragItem)
            {
                spriteBatch.Draw(this.image, Input.MousePosition, Color.White * 0.5f);
            }
        }
    }
}
