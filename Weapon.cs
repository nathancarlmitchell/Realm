using System;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Realm.Data;

namespace Realm
{
    public class Weapon : Equipment
    {
        public enum WeaponType
        {
            Wand, // 0
            Bow, // 1
            Sword, // 2
        }

        public WeaponType Type { get; set; }

        public int DamageMin { get; set; }
        public int DamageMax { get; set; }
        public int FireRate { get; set; }
        public float ProjectileMagnitude { get; set; }
        public int ProjectileDuration { get; set; }
        public Texture2D ProjectileImage;
        public string ProjectileImageName { get; set; }

        private readonly Random rand = new();

        // Equipment row, top-left corner of the sidebar's slot area. Armor/
        // Ring/AbilityItem each pick up the row by hardcoding this same y and
        // their own +40px-apart x offset — keep those four in sync if this
        // moves.
        static int x = Game1.SidebarX + 20;
        static int y = 410;

        public Weapon(Texture2D image, Texture2D projectileImage)
        {
            ID = Guid.NewGuid();

            this.image = image;
            ProjectileImage = projectileImage;

            SlotBounds = new Rectangle(x, y, 40, 40);
        }

        public static Weapon LoadWeapon(string weaponName)
        {
            Weapon weaponData = Game1.Instance.Weapons.FirstOrDefault(x => (x.Name == weaponName));

            // weaponData is null if weaponName doesn't match anything in WeaponData.json —
            // e.g. a save referencing a weapon that's since been renamed or removed.
            if (weaponData != null && weaponData.Type == Player.Instance.WeaponType)
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
                    Tier = weaponData.Tier,
                    DamageMin = weaponData.DamageMin,
                    DamageMax = weaponData.DamageMax,
                    ProjectileMagnitude = weaponData.ProjectileMagnitude,
                    ProjectileDuration = weaponData.ProjectileDuration,
                    ImageName = weaponData.ImageName,
                    ProjectileImageName = weaponData.ProjectileImageName,
                };

                Player.Instance.EquipWeapon(weapon);
                return weapon;
            }

            Sound.Play(Sound.Error, 0.4f);
            return null;
        }

        public Weapon()
        {
            SlotBounds = new Rectangle(x, y, 40, 40);
        }

        public virtual void Shoot()
        {
            double damgeModifier = (0.5 + Player.Instance.Attack / 50);
            double damage = rand.Next(DamageMin, DamageMax) * damgeModifier;

            var aim = Input.GetMouseAimDirection();

            if (aim.LengthSquared() > 0)
            {
                float aimAngle = aim.ToAngle();
                float randomSpread = rand.NextFloat(-0.04f, 0.04f) + rand.NextFloat(-0.04f, 0.04f);

                // Spawn from the edge of the player in the aim direction,
                // rather than dead center — reads as coming from a held
                // weapon instead of the player's chest.
                Vector2 spawnPosition =
                    Player.Instance.Position
                    + Extensions.FromPolar(aimAngle, Player.Instance.Radius);

                // Wand bolts pass through enemies (still only damaging each
                // one once, via EntityManager's HitBy tracking); every other
                // weapon type's shot expires on the first thing it hits.
                bool expiresOnHit = this.Type != WeaponType.Wand;

                Vector2 vel = Extensions.FromPolar(
                    aimAngle + randomSpread,
                    this.ProjectileMagnitude
                );

                EntityManager.Add(
                    new Projectile(spawnPosition, vel)
                    {
                        image = this.ProjectileImage,
                        Damage = (int)damage,
                        ExpiresOnHit = expiresOnHit,
                    }
                );

                if (this.Type == WeaponType.Bow)
                {
                    vel = Extensions.FromPolar(
                        aimAngle + randomSpread - 0.35f,
                        this.ProjectileMagnitude
                    );

                    EntityManager.Add(
                        new Projectile(spawnPosition, vel)
                        {
                            image = this.ProjectileImage,
                            Damage = (int)damage,
                            ExpiresOnHit = expiresOnHit,
                        }
                    );

                    vel = Extensions.FromPolar(
                        aimAngle + randomSpread + 0.35f,
                        this.ProjectileMagnitude
                    );

                    EntityManager.Add(
                        new Projectile(spawnPosition, vel)
                        {
                            image = this.ProjectileImage,
                            Damage = (int)damage,
                            ExpiresOnHit = expiresOnHit,
                        }
                    );
                }

                Sound.Play(Sound.MagicShoot, 0.3f);
            }
        }

        //This method needs to be replaced by drawing from the inventory.
        public void DrawEquipped(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Art.Border, new Vector2(x, y), Color.White);

            if (!IsEquipped)
                return;

            spriteBatch.Draw(this.image, new Vector2(x, y), Color.White);

            if (hover)
            {
                string text = TooltipText();

                int textY = (int)(Art.HudFont.MeasureString(text).Y / 2);

                Util.DrawTooltip(
                    spriteBatch,
                    Art.HudFont,
                    text,
                    new Vector2(x, y - image.Height - textY),
                    Color.Red
                );
            }
        }

        public override string TooltipText()
        {
            string description = Util.WrapText(Art.HudFont, Description, 350);
            return $"T{Tier} - {Name}{Environment.NewLine}{description}{Environment.NewLine}Damage: {DamageMin} - {DamageMax}";
        }
    }
}
