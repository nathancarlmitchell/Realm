using System;
using System.Collections.Generic;
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
            Staff, // 3
        }

        public WeaponType Type { get; set; }

        public override bool CanEquipByCurrentClass => Type == Player.Instance.WeaponType;

        public int DamageMin { get; set; }
        public int DamageMax { get; set; }
        public int FireRate { get; set; }
        public float ProjectileMagnitude { get; set; }
        public int ProjectileDuration { get; set; }
        public Texture2D ProjectileImage;
        public string ProjectileImageName { get; set; }

        // Staff-only — see SineWaveProjectile.cs. 0 for every other
        // weapon type, which correctly produces no perpendicular offset
        // at all (a straight line), so nothing else needs to check Type
        // before reading these.
        public float Amplitude { get; set; }
        public float Frequency { get; set; }

        // Bow-only — see Data/BowData.cs. DamageMin/DamageMax above and
        // ProjectileImage/ProjectileImageName below are reused as the Main
        // shot's damage/art; these are the Side shots' own independent
        // damage and art. 0/null for every other weapon type, which never
        // reads them.
        public int SideDamageMin { get; set; }
        public int SideDamageMax { get; set; }
        public Texture2D SideProjectileImage;
        public string SideProjectileImageName { get; set; }

        // Degrees each Bow side shot is angled away from the aim line. 0
        // for every other weapon type, which never reads it.
        public float ArcGapDegrees { get; set; }

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

                // Bow-only — every other weapon type's SideProjectileImageName
                // is null, and Content.Load can't take a null asset name.
                Texture2D sideProjectileTexture =
                    weaponData.SideProjectileImageName != null
                        ? Game1.Instance.Content.Load<Texture2D>(
                            weaponData.SideProjectileImageName
                        )
                        : null;

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
                    Amplitude = weaponData.Amplitude,
                    Frequency = weaponData.Frequency,
                    SideDamageMin = weaponData.SideDamageMin,
                    SideDamageMax = weaponData.SideDamageMax,
                    SideProjectileImage = sideProjectileTexture,
                    SideProjectileImageName = weaponData.SideProjectileImageName,
                    ArcGapDegrees = weaponData.ArcGapDegrees,
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

                // Wand and Bow shots both pierce (pass through enemies,
                // still only damaging each one once, via EntityManager's
                // HitBy tracking — "Piercing Shots hit multiple targets"
                // for every Bow shot, Main and Side alike); every other
                // weapon type's shot expires on the first thing it hits —
                // Staff included ("Staff shots do not pass through
                // targets").
                bool expiresOnHit = this.Type != WeaponType.Wand && this.Type != WeaponType.Bow;

                if (this.Type == WeaponType.Staff)
                {
                    // Two shots, 0-degree arc gap — both fire along the
                    // exact same angle (unlike Bow's Main/Side spread),
                    // distinguished only by sine-wave phase: one leads,
                    // the other trails a half-cycle behind, so they weave
                    // opposite each other around the aim line rather than
                    // moving in lockstep. See SineWaveProjectile.cs.
                    EntityManager.Add(
                        new SineWaveProjectile(
                            spawnPosition,
                            aimAngle + randomSpread,
                            this.ProjectileMagnitude,
                            this.Amplitude,
                            this.Frequency,
                            phaseOffset: 0f
                        )
                        {
                            image = this.ProjectileImage,
                            Damage = (int)damage,
                            ExpiresOnHit = expiresOnHit,
                            Duration = this.ProjectileDuration,
                        }
                    );

                    EntityManager.Add(
                        new SineWaveProjectile(
                            spawnPosition,
                            aimAngle + randomSpread,
                            this.ProjectileMagnitude,
                            this.Amplitude,
                            this.Frequency,
                            phaseOffset: MathHelper.Pi
                        )
                        {
                            image = this.ProjectileImage,
                            Damage = (int)damage,
                            ExpiresOnHit = expiresOnHit,
                            Duration = this.ProjectileDuration,
                        }
                    );

                    Sound.Play(Sound.MagicShoot, 0.3f);
                    return;
                }

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
                    // Side shots have their own damage range and art, and
                    // additionally ignore the target's Defense — the Bow's
                    // Armor Piercing effect (Main shot doesn't get this).
                    double sideDamage = rand.Next(SideDamageMin, SideDamageMax) * damgeModifier;
                    float arcGapRad = MathHelper.ToRadians(this.ArcGapDegrees);

                    vel = Extensions.FromPolar(
                        aimAngle + randomSpread - arcGapRad,
                        this.ProjectileMagnitude
                    );

                    EntityManager.Add(
                        new Projectile(spawnPosition, vel)
                        {
                            image = this.SideProjectileImage,
                            Damage = (int)sideDamage,
                            ExpiresOnHit = expiresOnHit,
                            IgnoresDefense = true,
                        }
                    );

                    vel = Extensions.FromPolar(
                        aimAngle + randomSpread + arcGapRad,
                        this.ProjectileMagnitude
                    );

                    EntityManager.Add(
                        new Projectile(spawnPosition, vel)
                        {
                            image = this.SideProjectileImage,
                            Damage = (int)sideDamage,
                            ExpiresOnHit = expiresOnHit,
                            IgnoresDefense = true,
                        }
                    );
                }

                Sound.Play(Sound.MagicShoot, 0.3f);
            }
        }

        // The current class's Tier 0 weapon — shown greyed out in an empty
        // slot as a placeholder for what goes there, e.g. so a fresh player
        // can tell the Weapon slot expects a Wand/Bow/Sword before ever
        // having equipped one.
        private static Texture2D PlaceholderImage =>
            Game1
                .Instance.Weapons.FirstOrDefault(w =>
                    w.Type == Player.Instance.WeaponType && w.Tier == 0
                )
                ?.image;

        //This method needs to be replaced by drawing from the inventory.
        public void DrawEquipped(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Art.Border, new Vector2(x, y), Color.White);

            if (!IsEquipped)
            {
                if (PlaceholderImage != null)
                    spriteBatch.Draw(PlaceholderImage, new Vector2(x, y), Color.Gray * 0.5f);
                return;
            }

            spriteBatch.Draw(this.image, new Vector2(x, y), Color.White);
        }

        // Drawn in its own pass, after every equip slot's border/icon (see
        // Overlay.DrawEquipment()) — otherwise a later-drawn slot's icon can
        // paint over this one's tooltip wherever the two overlap, since
        // SpriteBatch preserves draw-call order and the four slots aren't
        // drawn in the same order they're laid out on screen. Same fix as
        // LootBag's equivalent bug.
        public void DrawTooltip(SpriteBatch spriteBatch)
        {
            if (!IsEquipped || !hover)
                return;

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

        public override string TooltipText()
        {
            string description = Util.WrapText(Art.HudFont, Description, 350);
            string text =
                $"T{Tier} - {Name}{Environment.NewLine}{description}{Environment.NewLine}Damage: {DamageMin} - {DamageMax}";

            if (Type == WeaponType.Bow)
                text += $"{Environment.NewLine}Side Damage: {SideDamageMin} - {SideDamageMax}";

            return text;
        }

        public override List<(string Text, bool Better)> ComparisonLines(Equipment equipped)
        {
            var lines = HeaderLines();
            var equippedWeapon = (Weapon)equipped;
            float mine = (DamageMin + DamageMax) / 2f;
            float theirs = (equippedWeapon.DamageMin + equippedWeapon.DamageMax) / 2f;
            lines.Add(($"Damage: {DamageMin} - {DamageMax}", mine > theirs));
            return lines;
        }
    }
}
