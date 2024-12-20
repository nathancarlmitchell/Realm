using System;
using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Realm
{
    public class Weapon : Item
    {
        public string Description { get; set; }
        public int Teir { get; set; }

        public int DamageMin;
        public int DamageMax;
        public int FireRate;
        public float ProjectileMagnitude;
        public int ProjectileDuration;
        public Texture2D ProjectileImage;

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

        //public override void Update()
        //{
        //    hover = false;

        //    if (bounds.Intersects(Input.MouseBounds))
        //    {
        //        // Mouse over weapon.
        //        hover = true;
        //    }
        //}

        // This method needs to be replaced by drawing from the inventory.
        //public override void Draw(SpriteBatch spriteBatch)
        //{
        //    spriteBatch.Draw(Art.Border, new Vector2(x, y), Color.White);
        //    spriteBatch.Draw(Art.Wand, new Vector2(x, y), Color.White);

        //    if (hover)
        //    {
        //        string text =
        //            $"T{Teir} - {Name}{Environment.NewLine}{Description}{Environment.NewLine}Damge: {DamageMin} - {DamageMax}";

        //        int textY = (int)(Art.HudFont.MeasureString(text).Y / 2);

        //        spriteBatch.DrawString(
        //            Art.HudFont,
        //            text,
        //            new Vector2(x, y - image.Height - textY),
        //            Color.Red
        //        );
        //    }
        //}
    }
}
